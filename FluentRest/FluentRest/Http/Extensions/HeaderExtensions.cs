using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Urls;

namespace FluentRest.Http
{
	public delegate Task<string?> RefreshTokenActionDelegate(FluentRestDetail call, string? currentToken);

	/// <summary>
	/// Fluent extension methods for working with HTTP request headers.
	/// </summary>
    public static class HeaderExtensions
    {
	    /// <summary>
	    /// Sets an HTTP header to be sent with this IFluentRestRequest or all requests made with this IFluentRestClient.
	    /// </summary>
	    /// <param name="clientOrRequest">The IFluentRestClient or IFluentRestRequest.</param>
	    /// <param name="name">HTTP header name.</param>
	    /// <param name="value">HTTP header value.</param>
	    /// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
	    public static T WithHeader<T>(this T clientOrRequest, string name, object? value) where T : IHttpSettingsContainer {
		    if (value == null)
			    clientOrRequest.Headers.Remove(name);
			else
			    clientOrRequest.Headers.AddOrReplace(name, value.ToInvariantString()!);
		    return clientOrRequest;
	    }

	    /// <summary>
	    /// Sets HTTP headers based on property names/values of the provided object, or keys/values if object is a dictionary, to be sent with this IFluentRestRequest or all requests made with this IFluentRestClient.
	    /// </summary>
	    /// <param name="clientOrRequest">The IFluentRestClient or IFluentRestRequest.</param>
	    /// <param name="headers">Names/values of HTTP headers to set. Typically an anonymous object or IDictionary.</param>
	    /// <param name="replaceUnderscoreWithHyphen">If true, underscores in property names will be replaced by hyphens. Default is true.</param>
	    /// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
	    public static T WithHeaders<T>(this T clientOrRequest, object headers, bool replaceUnderscoreWithHyphen = true) where T : IHttpSettingsContainer {
		    if (headers == null)
			    return clientOrRequest;

			// underscore replacement only applies when object properties are parsed to kv pairs
		    replaceUnderscoreWithHyphen = replaceUnderscoreWithHyphen && !(headers is string) && !(headers is IEnumerable);

		    foreach (var kv in headers.ToKeyValuePairs()) {
			    var key = replaceUnderscoreWithHyphen ? kv.Key.Replace("_", "-") : kv.Key;
			    clientOrRequest.WithHeader(key, kv.Value);
		    }

		    return clientOrRequest;
	    }

	    /// <summary>
	    /// Sets HTTP authorization header according to Basic Authentication protocol to be sent with this IFluentRestRequest or all requests made with this IFluentRestClient.
	    /// </summary>
	    /// <param name="clientOrRequest">The IFluentRestClient or IFluentRestRequest.</param>
	    /// <param name="username">Username of authenticating user.</param>
	    /// <param name="password">Password of authenticating user.</param>
	    /// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
	    public static T WithBasicAuth<T>(this T clientOrRequest, string username, string password) where T : IHttpSettingsContainer {
		    // http://stackoverflow.com/questions/14627399/setting-authorization-header-of-httpclient
		    var encodedCreds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
		    return clientOrRequest.WithHeader("Authorization", $"Basic {encodedCreds}");
	    }

	    public static T WithOAuthBearerToken<T>(this T clientOrRequest, string? token) where T : IHttpSettingsContainer 
		    => token != null ? clientOrRequest.WithHeader("Authorization", $"Bearer {token}") : clientOrRequest;

	    /// <summary>
	    /// Sets HTTP authorization header with acquired bearer token according to OAuth 2.0 specification to be sent with this IFluentRestRequest or all requests made with this IFluentRestClient.
	    /// Optionally sets an action that refreshes the token when a 401 Unauthorized error is returned.
	    /// </summary>
	    /// <param name="clientOrRequest">The IFluentRestClient or IFluentRestRequest.</param>
	    /// <param name="token">The acquired bearer token to pass.</param>
	    /// <param name="refreshTokenAction">An optional action to refresh the token if 401 Unauthorized was returned from the call</param>
	    /// <param name="maxRetry">Default to 1. Max retry calls to refreshTokenAction if the call still fails after update</param>
	    /// <returns>This IFluentRestClient or IFluentRestRequest.</returns>
	    public static T WithOAuthBearerToken<T>(this T clientOrRequest, string? token, RefreshTokenActionDelegate? refreshTokenAction, int maxRetry = 1) where T : IHttpSettingsContainer
	    {
		    var currentToken = token;
		    var sync = new SemaphoreSlim(1, 1);
		    
		    if(currentToken != null)
				clientOrRequest.WithHeader("Authorization", $"Bearer {currentToken}");
		    
		    if(refreshTokenAction != null)
		    {
			    clientOrRequest.OnError(async call =>
			    {
				    if (call.Response?.StatusCode == (int)HttpStatusCode.Unauthorized)
				    {
					    if (call.RetryCount < maxRetry)
					    {
						    var existingToken = currentToken;
						    await sync.WaitAsync();
						    var alreadyRefreshed = existingToken != currentToken;

						    try
						    {
							    var newToken = alreadyRefreshed ? currentToken : await refreshTokenAction(call, currentToken);
							    if (newToken != null && (newToken != currentToken || alreadyRefreshed))
							    {
								    currentToken = newToken;
								    clientOrRequest.WithHeader("Authorization", $"Bearer {newToken}");
								    call.ExceptionHandled = true;
								    call.RetryCount++;
								    call.Response = await call.Request.SendAgainAsync(call);
							    }
							    else
								    currentToken = newToken;
						    }
						    finally
						    {
							    sync.Release();
						    }
					    }
				    }
			    });
		    }

		    return clientOrRequest;
	    }
    }
}
