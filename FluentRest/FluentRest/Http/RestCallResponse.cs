using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Http.Configuration;
using FluentRest.Rest.Configuration;
using FluentRest.Urls;

namespace FluentRest.Http
{
	/// <inheritdoc />
	public class FluentRestResponse : IFluentRestResponse
	{
		private readonly Lazy<IReadOnlyNameValueList<string>> headers;
		private readonly Lazy<IReadOnlyList<FluentRestCookie>> cookies;
		private object? capturedBody;
		private bool streamRead;
		private ISerializer? serializer;

		public IReadOnlyNameValueList<string> Headers => headers.Value;
		public IReadOnlyList<FluentRestCookie> Cookies => cookies.Value;
		public HttpResponseMessage ResponseMessage { get; }
		public int StatusCode => (int)ResponseMessage.StatusCode;

		/// <summary>
		/// Creates a new object that wraps HttpResponseMessage
		/// </summary>
		public FluentRestResponse(HttpResponseMessage resp, CookieJar? cookieJar = null) 
        {
			ResponseMessage = resp;
			headers = new Lazy<IReadOnlyNameValueList<string>>(LoadHeaders);
			cookies = new Lazy<IReadOnlyList<FluentRestCookie>>(LoadCookies);
			LoadCookieJar(cookieJar);
		}

		private IReadOnlyNameValueList<string> LoadHeaders() {
			var result = new NameValueList<string>(false);

			foreach (var h in ResponseMessage.Headers)
			foreach (var v in h.Value)
				result.Add(h.Key, v);

			if (ResponseMessage.Content?.Headers == null)
				return result;

			foreach (var h in ResponseMessage.Content.Headers)
			foreach (var v in h.Value)
				result.Add(h.Key, v);

			return result;
		}

		private IReadOnlyList<FluentRestCookie> LoadCookies() {
			var url = ResponseMessage.RequestMessage.RequestUri.AbsoluteUri;
			return ResponseMessage.Headers.TryGetValues("Set-Cookie", out var headerValues) ?
				headerValues.Select(hv => CookieCutter.ParseResponseHeader(url, hv)).ToList() :
				new List<FluentRestCookie>();
		}

		private void LoadCookieJar(CookieJar? jar) 
        {
			if (jar == null) 
                return;
			foreach (var cookie in Cookies)
				jar.TryAddOrReplace(cookie, out _); // not added if cookie fails validation
		}

        /// <inheritdoc />
        public async Task<T?> GetJsonAsync<T>() 
        {
			if (streamRead) 
            {
				if (capturedBody == null) 
                    return default;
				if (capturedBody is T body) 
                    return body;
			}

			var call = ResponseMessage.RequestMessage.GetFluentRestDetail();
			serializer ??= call.Request?.Settings?.JsonSerializer ?? SystemTextJsonSerializer.Default;

			try 
            {
				if (streamRead) 
                {
					// Stream was read but captured as a different type than T. If it was captured as a string,
					// we should be in good shape. If it was deserialized to a different type, the best we can
					// do is serialize it and then deserialize to T, and we could lose data. But that's a very
					// uncommon scenario, hopefully. 
					var s = capturedBody as string ?? serializer.Serialize(capturedBody!);
					capturedBody = serializer.Deserialize<T>(s);
				}
				else
                {
                    await using var stream = await ResponseMessage.Content.ReadAsStreamAsync();
                    if (stream == null || stream.Length == 0)
	                    return default;
                    capturedBody = serializer.Deserialize<T>(stream);
                }
				return (T?)capturedBody;
			}
			catch (Exception ex) 
            {
				serializer = null;
				capturedBody = await ResponseMessage.Content.ReadAsStringAsync();
				streamRead = true;
				call.Exception = new FluentRestParsingException(call, "JSON", ex);
				await FluentRestRequest.HandleExceptionAsync(call, call.Exception, CancellationToken.None);
				return default;
			}
			finally 
            {
				streamRead = true;
			}
		}

		//public async Task<dynamic> GetJsonAsync() 
  //      {
		//	dynamic d = await GetJsonAsync<ExpandoObject>();
		//	return d;
		//}
		//public async Task<IList<dynamic>> GetJsonListAsync() 
  //      {
		//	dynamic[] d = await GetJsonAsync<ExpandoObject[]>();
		//	return d;
		//}
		
        public async Task<string?> GetStringAsync() 
        {
			if (streamRead) 
            {
				return
					(capturedBody == null) ? null :
					// if GetJsonAsync<T> was called, we streamed the response directly to a T (for memory efficiency)
					// without first capturing a string. it's too late to get it, so the best we can do is serialize the T
					(serializer != null) ? serializer.Serialize(capturedBody) :
					capturedBody?.ToString();
			}

			// strip quotes from charset so .NET doesn't choke on them
			// https://github.com/dotnet/corefx/issues/5014
			var ct = ResponseMessage.Content.Headers?.ContentType;
			if (ct?.CharSet != null)
				ct.CharSet = ct.CharSet.StripQuotes();

			capturedBody = await ResponseMessage.Content.ReadAsStringAsync();
			streamRead = true;
			return (string)capturedBody;
		}

		/// <inheritdoc />
		public Task<Stream> GetStreamAsync() 
        {
			streamRead = true;
			return ResponseMessage.Content.ReadAsStreamAsync();
		}

		/// <inheritdoc />
		public async Task<byte[]?> GetBytesAsync() 
        {
			if (streamRead)
				return capturedBody as byte[];

			capturedBody = await ResponseMessage.Content.ReadAsByteArrayAsync();
			streamRead = true;
			return (byte[])capturedBody;
		}

		/// <summary>
		/// Disposes the underlying HttpResponseMessage.
		/// </summary>
		public void Dispose() => ResponseMessage.Dispose();
	}
}
