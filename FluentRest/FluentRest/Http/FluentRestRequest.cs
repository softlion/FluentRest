using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Http.Configuration;
using FluentRest.Urls;

namespace FluentRest.Http
{
	/// <inheritdoc />
	public class FluentRestRequest : IFluentRestRequest
	{
		private FluentRestHttpSettings? settings;
		private IFluentRestClient? client;
		private Url? url;
		private FluentRestDetail redirectedFrom;
		private CookieJar? cookies;

		/// <summary>
		/// Initializes a new instance of the <see cref="FluentRestRequest"/> class.
		/// </summary>
		/// <param name="url">The URL to call with this FluentRestRequest instance.</param>
		public FluentRestRequest(Url? url = null) 
		{
			this.url = url;
		}

		/// <summary>
		/// Used internally by FluentRestClient.Request and CookieSession.Request
		/// </summary>
		internal FluentRestRequest(string baseUrl, object[] urlSegments)
		{
			var parts = new List<string>(urlSegments.Select(s => s.ToInvariantString()));
			if (!Url.IsValid(parts.FirstOrDefault() ?? string.Empty) && !string.IsNullOrEmpty(baseUrl))
				parts.Insert(0, baseUrl);

			if (!parts.Any())
				throw new ArgumentException("Cannot create a Request. BaseUrl is not defined and no segments were passed.");
			if (!Url.IsValid(parts[0]))
				throw new ArgumentException("Cannot create a Request. Neither BaseUrl nor the first segment passed is a valid URL.");

			url = Url.Combine(parts.ToArray());
		}

		/// <summary>
		/// Gets or sets the FluentRestHttpSettings used by this request.
		/// </summary>
		public FluentRestHttpSettings Settings {
			get {
				if (settings == null) {
					settings = new FluentRestHttpSettings();
					ResetDefaultSettings();
				}
				return settings;
			}
			set {
				settings = value;
				ResetDefaultSettings();
			}
		}

		/// <inheritdoc />
		public IFluentRestClient? Client 
		{
			get =>
				client ?? 
				(Url != null ? FluentRestHttp.GlobalSettings.FluentRestClientFactory.Get(Url) : null);
			set {
				client = value;
				ResetDefaultSettings();
			}
		}

		/// <inheritdoc />
		public HttpMethod Verb { get; set; }

		/// <inheritdoc />
		public Url? Url 
		{
			get => url;
			set 
			{
				url = value;
				ResetDefaultSettings();
			}
		}

		private void ResetDefaultSettings() 
		{
#pragma warning disable CS8601
			if (settings != null)
				settings.Defaults = Client?.Settings;
#pragma warning restore CS8601
		}

		/// <inheritdoc />
		public INameValueList<string> Headers { get; } = new NameValueList<string>(false); // header names are case-insensitive https://stackoverflow.com/a/5259004/62600

		/// <inheritdoc />
		public IEnumerable<(string Name, string Value)> Cookies =>
			CookieCutter.ParseRequestHeader(Headers.FirstOrDefault("Cookie"));

		/// <inheritdoc />
		public CookieJar? CookieJar 
		{
			get => cookies;
			set 
			{
				cookies = value;
				if (value != null) 
				{
					this.WithCookies(
						from c in value
						where c.ShouldSendTo(this.Url, out _)
						// sort by longest path, then earliest creation time, per #2: https://tools.ietf.org/html/rfc6265#section-5.4
						orderby (c.Path ?? c.OriginUrl.Path).Length descending, c.DateReceived
						select (c.Name, c.Value));
				}
			}
		}

		public async Task<IFluentRestResponse> SendAgainAsync(FluentRestDetail call)
		{
			if (call.CancellationToken?.IsCancellationRequested == true)
				return call.Response!;

			//A new http request is required, as HttpClient refuses to send the same twice
			var request = new HttpRequestMessage(Verb, Url) { Content = call.HttpRequestMessage.Content };
			SyncHeaders(request);
			call.HttpRequestMessage = request;
			request.SetFluentRestDetail(call);
			
			return await DoCall(call);
		}

		public Task<IFluentRestResponse> SendAsync(HttpMethod verb, HttpContent? content = null, CancellationToken cancellationToken = default, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead) 
		{
			// "freeze" the client at this point to avoid excessive calls to FluentRestClientFactory.Get (#374)
			client = Client; 
			Verb = verb;

			var request = new HttpRequestMessage(verb, Url) { Content = content };
			SyncHeaders(request);
			var call = new FluentRestDetail 
			{
				Request = this,
				RedirectedFrom = redirectedFrom,
				HttpRequestMessage = request,
				CancellationToken = cancellationToken,
				HttpCompletionOption = completionOption,
			};
			request.SetFluentRestDetail(call);
			
			return DoCall(call);
		}

		private async Task<IFluentRestResponse> DoCall(FluentRestDetail call)
		{
			await RaiseEventAsync(Settings.BeforeCall, Settings.BeforeCallAsync, call);

			// in case URL or headers were modified in the handler above
			call.HttpRequestMessage.RequestUri = Url.ToUri();
			SyncHeaders(call.HttpRequestMessage);

			call.StartedUtc = DateTime.UtcNow;
			var cancellationToken = call.CancellationToken ?? default;
			var ct = GetCancellationTokenWithTimeout(cancellationToken, out var cts);

			try
			{
				var completionOption = call.HttpCompletionOption!.Value;
				var response = await Client!.HttpClient.SendAsync(call.HttpRequestMessage, completionOption, ct);
				call.HttpResponseMessage = response;
				call.HttpResponseMessage.RequestMessage = call.HttpRequestMessage;
				call.Response = new FluentRestResponse(call.HttpResponseMessage, CookieJar);

				if (call.Succeeded) 
				{
					var redirectResponse = await ProcessRedirectAsync(call, cancellationToken, completionOption);
					return redirectResponse ?? call.Response;
				}
				else
					throw new FluentRestHttpException(call, null);
			}
			catch (Exception ex) 
			{
				return await HandleExceptionAsync(call, ex, cancellationToken);
			}
			finally 
			{
				cts?.Dispose();
				call.EndedUtc = DateTime.UtcNow;
				await RaiseEventAsync(Settings.AfterCall, Settings.AfterCallAsync, call);
				call.HttpRequestMessage.Dispose();
			}
		}

		private void SyncHeaders(HttpRequestMessage request) 
		{
			// copy any client-level (default) headers to this request
			foreach (var header in Client.Headers.Where(h => !this.Headers.Contains(h.Name)).ToList())
				this.Headers.Add(header.Name, header.Value);

			// copy headers from FluentRestRequest to HttpRequestMessage
			foreach (var header in Headers)
				request.SetHeader(header.Name, header.Value, false);

			// copy headers from HttpContent to FluentRestRequest
			if (request.Content != null) {
				foreach (var header in request.Content.Headers)
					Headers.AddOrReplace(header.Key, string.Join(",", header.Value));
			}
		}

		private CancellationToken GetCancellationTokenWithTimeout(CancellationToken original, out CancellationTokenSource? timeoutTokenSource) 
		{
			timeoutTokenSource = null;
			if (!Settings.Timeout.HasValue) 
				return original;
			
			timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(original);
			timeoutTokenSource.CancelAfter(Settings.Timeout.Value);
			return timeoutTokenSource.Token;
		}

		private async Task<IFluentRestResponse?> ProcessRedirectAsync(FluentRestDetail call, CancellationToken cancellationToken, HttpCompletionOption completionOption) 
		{
			if (Settings.Redirects.Enabled)
				call.Redirect = GetRedirect(call);

			if (call.Redirect == null)
				return null;

			await RaiseEventAsync(Settings.OnRedirect, Settings.OnRedirectAsync, call);

			if (call.Redirect.Follow != true)
				return null;

			CheckForCircularRedirects(call);

			var redirect = new FluentRestRequest(call.Redirect.Url) {
				Client = Client,
				redirectedFrom = call,
				Settings = { Defaults = Settings }
			};

			if (CookieJar != null)
				redirect.CookieJar = CookieJar;

			var changeToGet = call.Redirect.ChangeVerbToGet;

			redirect.WithHeaders(Headers.Where(h =>
				h.Name.OrdinalEquals("Cookie", true) ? false : // never blindly forward Cookie header; CookieJar should be used to ensure rules are enforced
				h.Name.OrdinalEquals("Authorization", true) ? Settings.Redirects.ForwardAuthorizationHeader :
				h.Name.OrdinalEquals("Transfer-Encoding", true) ? Settings.Redirects.ForwardHeaders && !changeToGet :
				Settings.Redirects.ForwardHeaders));

			var ct = GetCancellationTokenWithTimeout(cancellationToken, out var cts);
			try 
			{
				return await redirect.SendAsync(
					changeToGet ? HttpMethod.Get : call.HttpRequestMessage.Method,
					changeToGet ? null : call.HttpRequestMessage.Content,
					ct,
					completionOption);
			}
			finally 
			{
				cts?.Dispose();
			}
		}

		// partially lifted from https://github.com/dotnet/runtime/blob/master/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/RedirectHandler.cs
		private FluentRestRedirect? GetRedirect(FluentRestDetail call) 
		{
			if (call.Response.StatusCode is < 300 or > 399)
				return null;

			if (!call.Response.Headers.TryGetFirst("Location", out var location))
				return null;

			var redirect = new FluentRestRedirect();

			if (Url.IsValid(location))
				redirect.Url = new Url(location);
			else if (location.OrdinalStartsWith("//"))
				redirect.Url = new Url(Url.Scheme + ":" + location);
			else if (location.OrdinalStartsWith("/"))
				redirect.Url = Url.Combine(Url.Root, location);
			else
				redirect.Url = Url.Combine(Url.Root, Url.Path, location);

			// Per https://tools.ietf.org/html/rfc7231#section-7.1.2, a redirect location without a
			// fragment should inherit the fragment from the original URI.
			if (string.IsNullOrEmpty(redirect.Url.Fragment))
				redirect.Url.Fragment = Url.Fragment;

			redirect.Count = 1 + (call.RedirectedFrom?.Redirect?.Count ?? 0);

			var isSecureToInsecure = Url.IsSecureScheme && !redirect.Url.IsSecureScheme;
			redirect.Follow = new[] { 301, 302, 303, 307, 308 }.Contains(call.Response.StatusCode) 
			                  && redirect.Count <= Settings.Redirects.MaxAutoRedirects 
			                  && (Settings.Redirects.AllowSecureToInsecure || !isSecureToInsecure);

			bool ChangeVerbToGetOn(int statusCode, HttpMethod verb) =>
				statusCode switch
				{
					// 301 and 302 are a bit ambiguous. The spec says to preserve the verb but most browsers rewrite it to GET.
					// HttpClient stack changes it if only it's a POST, presumably since that's a browser-friendly verb.
					301 or 302 => verb == HttpMethod.Post,
					303 => true,
					_ => false // 307 & 308 mainly
				};

			redirect.ChangeVerbToGet = redirect.Follow && ChangeVerbToGetOn(call.Response.StatusCode, call.Request.Verb);
			return redirect;
		}

		private void CheckForCircularRedirects(FluentRestDetail? call, HashSet<string>? visited = null) 
		{
			if (call == null) 
				return;
			visited ??= new HashSet<string>();
			if (visited.Contains(call.Request.Url))
				throw new FluentRestHttpException(call, "Circular redirects detected.", null);
			visited.Add(call.Request.Url);
			CheckForCircularRedirects(call.RedirectedFrom, visited);
		}

		internal static async Task<IFluentRestResponse> HandleExceptionAsync(FluentRestDetail call, Exception ex, CancellationToken token) 
		{
			call.Exception = ex;
			call.ExceptionHandled = false;
			await RaiseEventAsync(call.Request.Settings.OnError, call.Request.Settings.OnErrorAsync, call);
			if (call.ExceptionHandled)
				return call.Response!;

			throw ex switch
			{
				OperationCanceledException when !token.IsCancellationRequested => new FluentRestHttpTimeoutException(call, ex),
				FluentRestHttpException => ex,
				_ => new FluentRestHttpException(call, ex)
			};
		}

		private static async Task RaiseEventAsync(Action<FluentRestDetail>? syncHandler, Func<FluentRestDetail, Task>? asyncHandler, FluentRestDetail call) 
		{
			syncHandler?.Invoke(call);
			if (asyncHandler != null)
				await asyncHandler(call);
		}
	}
}