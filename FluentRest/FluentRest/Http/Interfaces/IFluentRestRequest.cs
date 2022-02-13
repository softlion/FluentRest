using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Urls;

namespace FluentRest.Http
{
    /// <summary>
    /// Represents an HTTP request. Can be created explicitly via new FluentRestRequest(), fluently via Url.Request(),
    /// or implicitly when a call is made via methods like Url.GetAsync().
    /// </summary>
    public interface IFluentRestRequest : IHttpSettingsContainer
    {
        /// <summary>
        /// Gets or sets the IFluentRestClient to use when sending the request.
        /// </summary>
        IFluentRestClient Client { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method of the request. Normally you don't need to set this explicitly; it will be set
        /// when you call the sending method, such as GetAsync, PostAsync, etc.
        /// </summary>
        HttpMethod Verb { get; set; }

        /// <summary>
        /// Gets or sets the URL to be called.
        /// </summary>
        Url Url { get; set; }

        /// <summary>
        /// Gets Name/Value pairs parsed from the Cookie request header.
        /// </summary>
        IEnumerable<(string Name, string Value)> Cookies { get; }

        /// <summary>
        /// Gets or sets the collection of HTTP cookies that can be shared between multiple requests. When set, values that
        /// should be sent with this request (based on Domain, Path, and other rules) are immediately copied to the Cookie
        /// request header, and any Set-Cookie headers received in the response will be written to the CookieJar.
        /// </summary>
        CookieJar CookieJar { get; set; }

        /// <summary>
        /// Asynchronously sends the HTTP request. Mainly used to implement higher-level extension methods (GetJsonAsync, etc).
        /// </summary>
        /// <param name="verb">The HTTP method used to make the request.</param>
        /// <param name="content">Contents of the request body.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="completionOption">The HttpCompletionOption used in the request. Optional.</param>
        /// <returns>A Task whose result is the received IFluentRestResponse.</returns>
        Task<IFluentRestResponse> SendAsync(HttpMethod verb, HttpContent? content = null, CancellationToken cancellationToken = default, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead);

        Task<IFluentRestResponse> SendAgainAsync(FluentRestDetail call);
    }
}