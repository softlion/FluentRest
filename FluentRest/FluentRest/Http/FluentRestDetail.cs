using System;
using System.Net.Http;
using System.Threading;
using FluentRest.Http.Content;

namespace FluentRest.Http
{
	/// <summary>
	/// Encapsulates request, response, and other details associated with an HTTP call. Useful for diagnostics and available in
	/// global event handlers and FluentRestHttpException.Call.
	/// </summary>
	public class FluentRestDetail
	{
		/// <summary>
		/// The IFluentRestRequest associated with this call.
		/// </summary>
		public IFluentRestRequest Request { get; set; }

		/// <summary>
		/// The raw HttpRequestMessage associated with this call.
		/// </summary>
		public HttpRequestMessage HttpRequestMessage { get; set; }

		/// <summary>
		/// Captured request body. Available ONLY if HttpRequestMessage.Content is a FluentRest.Http.Content.CapturedStringContent.
		/// </summary>
		public string? RequestBody => (HttpRequestMessage.Content as CapturedStringContent)?.Content;

		/// <summary>
		/// The IFluentRestResponse associated with this call if the call completed, otherwise null.
		/// </summary>
		public IFluentRestResponse? Response { get; set; }

		/// <summary>
		/// The FluentRestDetail that received a 3xx response and automatically triggered this call.
		/// </summary>
		public FluentRestDetail? RedirectedFrom { get; set; }

		/// <summary>
		/// If this call has a 3xx response and Location header, contains information about how to handle the redirect.
		/// Otherwise null.
		/// </summary>
		public FluentRestRedirect? Redirect { get; set; }

		/// <summary>
		/// The cancellation token used in the call
		/// </summary>
		public CancellationToken? CancellationToken { get; set; }
		
		/// <summary>
		/// The completion option used in the call
		/// </summary>
		public HttpCompletionOption? HttpCompletionOption { get; set; }

		/// <summary>
		/// The raw HttpResponseMessage associated with the call if the call completed, otherwise null.
		/// </summary>
		public HttpResponseMessage? HttpResponseMessage { get; set; }

		/// <summary>
		/// Exception that occurred while sending the HttpRequestMessage.
		/// </summary>
		public Exception Exception { get; set; }
	
		/// <summary>
		/// User code should set this to true inside global event handlers (OnError, etc) to indicate
		/// that the exception was handled and should not be propagated further.
		/// </summary>
		public bool ExceptionHandled { get; set; }

		/// <summary>
		/// DateTime the moment the request was sent.
		/// </summary>
		public DateTime StartedUtc { get; set; }

		/// <summary>
		/// DateTime the moment a response was received.
		/// </summary>
		public DateTime? EndedUtc { get; set; }

		/// <summary>
		/// Total duration of the call if it completed, otherwise null.
		/// </summary>
		public TimeSpan? Duration => EndedUtc - StartedUtc;

		/// <summary>
		/// True if a response was received, regardless of whether it is an error status.
		/// </summary>
		public bool Completed => HttpResponseMessage != null;

		/// <summary>
		/// True if response was received with any success status or a match with AllowedHttpStatusRange setting.
		/// </summary>
		public bool Succeeded =>
			HttpResponseMessage == null ? false :
			(int)HttpResponseMessage.StatusCode < 400 ? true :
			string.IsNullOrEmpty(Request?.Settings?.AllowedHttpStatusRange) ? false :
			HttpStatusRangeParser.IsMatch(Request.Settings.AllowedHttpStatusRange, HttpResponseMessage.StatusCode);

		public int RetryCount { get; set; }

		/// <summary>
		/// Returns the verb and absolute URI associated with this call.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{HttpRequestMessage.Method:U} {Request?.Url}";
	}
}
