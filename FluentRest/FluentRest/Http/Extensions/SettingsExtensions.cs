using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentRest.Http.Configuration;

namespace FluentRest.Http
{
	/// <summary>
	/// Fluent extension methods for tweaking FluentRestHttpSettings
	/// </summary>
	public static class SettingsExtensions
	{
		/// <summary>
		/// Change FluentRestHttpSettings for this IFluentRestClient.
		/// </summary>
		/// <param name="client">The IFluentRestClient.</param>
		/// <param name="action">Action defining the settings changes.</param>
		/// <returns>The IFluentRestClient with the modified Settings</returns>
		public static IFluentRestClient Configure(this IFluentRestClient client, Action<ClientFluentRestHttpSettings> action) {
			action(client.Settings);
			return client;
		}

		/// <summary>
		/// Change FluentRestHttpSettings for this IFluentRestRequest.
		/// </summary>
		/// <param name="request">The IFluentRestRequest.</param>
		/// <param name="action">Action defining the settings changes.</param>
		/// <returns>The IFluentRestRequest with the modified Settings</returns>
		public static IFluentRestRequest ConfigureRequest(this IFluentRestRequest request, Action<FluentRestHttpSettings> action) {
			action(request.Settings);
			return request;
		}

		/// <summary>
		/// Fluently specify the IFluentRestClient to use with this IFluentRestRequest.
		/// </summary>
		/// <param name="request">The IFluentRestRequest.</param>
		/// <param name="client">The IFluentRestClient to use when sending the request.</param>
		/// <returns>A new IFluentRestRequest to use in calling the Url</returns>
		public static IFluentRestRequest WithClient(this IFluentRestRequest request, IFluentRestClient client) {
			request.Client = client;
			return request;
		}

		/// <summary>
		/// Sets the timeout for this IFluentRestRequest or all requests made with this IFluentRestClient.
		/// </summary>
		/// <param name="obj">The IFluentRestClient or IFluentRestRequest.</param>
		/// <param name="timespan">Time to wait before the request times out.</param>
		/// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
		public static T WithTimeout<T>(this T obj, TimeSpan timespan) where T : IHttpSettingsContainer {
			obj.Settings.Timeout = timespan;
			return obj;
		}

		/// <summary>
		/// Sets the timeout for this IFluentRestRequest or all requests made with this IFluentRestClient.
		/// </summary>
		/// <param name="obj">The IFluentRestClient or IFluentRestRequest.</param>
		/// <param name="seconds">Seconds to wait before the request times out.</param>
		/// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
		public static T WithTimeout<T>(this T obj, int seconds) where T : IHttpSettingsContainer {
			obj.Settings.Timeout = TimeSpan.FromSeconds(seconds);
			return obj;
		}

		/// <summary>
		/// Adds a pattern representing an HTTP status code or range of codes which (in addition to 2xx) will NOT result in a FluentRestHttpException being thrown.
		/// </summary>
		/// <param name="obj">The IFluentRestClient or IFluentRestRequest.</param>
		/// <param name="pattern">Examples: "3xx", "100,300,600", "100-299,6xx"</param>
		/// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
		public static T AllowHttpStatus<T>(this T obj, string pattern) where T : IHttpSettingsContainer {
			if (!string.IsNullOrWhiteSpace(pattern)) {
				var current = obj.Settings.AllowedHttpStatusRange;
				if (string.IsNullOrWhiteSpace(current))
					obj.Settings.AllowedHttpStatusRange = pattern;
				else
					obj.Settings.AllowedHttpStatusRange += "," + pattern;
			}
			return obj;
		}

		/// <summary>
		/// Adds an <see cref="HttpStatusCode" /> which (in addition to 2xx) will NOT result in a FluentRestHttpException being thrown.
		/// </summary>
		/// <param name="obj">The IFluentRestClient or IFluentRestRequest.</param>
		/// <param name="statusCodes">Examples: HttpStatusCode.NotFound</param>
		/// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
		public static T AllowHttpStatus<T>(this T obj, params HttpStatusCode[] statusCodes) where T : IHttpSettingsContainer {
			var pattern = string.Join(",", statusCodes.Select(c => (int)c));
			return AllowHttpStatus(obj, pattern);
		}

		/// <summary>
		/// Prevents a FluentRestHttpException from being thrown on any completed response, regardless of the HTTP status code.
		/// </summary>
		/// <param name="obj">The IFluentRestClient or IFluentRestRequest.</param>
		/// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
		public static T AllowAnyHttpStatus<T>(this T obj) where T : IHttpSettingsContainer {
			obj.Settings.AllowedHttpStatusRange = "*";
			return obj;
		}

		/// <summary>
		/// Configures whether redirects are automatically followed.
		/// </summary>
		/// <param name="obj">The IFluentRestClient or IFluentRestRequest.</param>
		/// <param name="enabled">true if FluentRest should automatically send a new request to the redirect URL, false if it should not.</param>
		/// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
		public static T WithAutoRedirect<T>(this T obj, bool enabled) where T : IHttpSettingsContainer {
			obj.Settings.Redirects.Enabled = enabled;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked immediately before every HTTP request is sent.
		/// </summary>
		public static T BeforeCall<T>(this T obj, Action<FluentRestDetail> act) where T : IHttpSettingsContainer {
			obj.Settings.BeforeCall = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked asynchronously immediately before every HTTP request is sent.
		/// </summary>
		public static T BeforeCall<T>(this T obj, Func<FluentRestDetail, Task> act) where T : IHttpSettingsContainer {
			obj.Settings.BeforeCallAsync = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked immediately after every HTTP response is received.
		/// </summary>
		public static T AfterCall<T>(this T obj, Action<FluentRestDetail> act) where T : IHttpSettingsContainer {
			obj.Settings.AfterCall = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked asynchronously immediately after every HTTP response is received.
		/// </summary>
		public static T AfterCall<T>(this T obj, Func<FluentRestDetail, Task> act) where T : IHttpSettingsContainer {
			obj.Settings.AfterCallAsync = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked when an error occurs during any HTTP call, including when any non-success
		/// HTTP status code is returned in the response. Response should be null-checked if used in the event handler.
		/// </summary>
		public static T OnError<T>(this T obj, Action<FluentRestDetail> act) where T : IHttpSettingsContainer {
			obj.Settings.OnError = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked asynchronously when an error occurs during any HTTP call, including when any non-success
		/// HTTP status code is returned in the response. Response should be null-checked if used in the event handler.
		/// </summary>
		public static T OnError<T>(this T obj, Func<FluentRestDetail, Task> act) where T : IHttpSettingsContainer {
			obj.Settings.OnErrorAsync = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked when any 3xx response with a Location header is received.
		/// You can inspect/manipulate the call.Redirect object to determine what will happen next.
		/// An auto-redirect will only happen if call.Redirect.Follow is true upon exiting the callback.
		/// </summary>
		public static T OnRedirect<T>(this T obj, Action<FluentRestDetail> act) where T : IHttpSettingsContainer {
			obj.Settings.OnRedirect = act;
			return obj;
		}

		/// <summary>
		/// Sets a callback that is invoked asynchronously when any 3xx response with a Location header is received.
		/// You can inspect/manipulate the call.Redirect object to determine what will happen next.
		/// An auto-redirect will only happen if call.Redirect.Follow is true upon exiting the callback.
		/// </summary>
		public static T OnRedirect<T>(this T obj, Func<FluentRestDetail, Task> act) where T : IHttpSettingsContainer {
			obj.Settings.OnRedirectAsync = act;
			return obj;
		}
	}
}