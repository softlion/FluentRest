using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluentRest.Urls;

namespace FluentRest.Http
{
    /// <summary>
    /// Represents an HTTP response.
    /// </summary>
    public interface IFluentRestResponse : IDisposable
    {
        /// <summary>
        /// Gets the collection of response headers received.
        /// </summary>
        IReadOnlyNameValueList<string> Headers { get; }

        /// <summary>
        /// Gets the collection of HTTP cookies received in this response via Set-Cookie headers.
        /// </summary>
        IReadOnlyList<FluentRestCookie> Cookies { get; }

        /// <summary>
        /// Gets the raw HttpResponseMessage that this IFluentRestResponse wraps.
        /// </summary>
        HttpResponseMessage ResponseMessage { get; }

        /// <summary>
        /// Gets the HTTP status code of the response.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Deserializes JSON-formatted HTTP response body to object of type T.
        /// </summary>
        /// <typeparam name="T">A type whose structure matches the expected JSON response.</typeparam>
        /// <returns>A Task whose result is an object containing data in the response body.</returns>
        /// <example>x = await url.PostAsync(data).GetJson&lt;T&gt;()</example>
        /// <exception cref="FluentRestHttpException">Condition.</exception>
        Task<T?> GetJsonAsync<T>();

        /// <summary>
        /// Returns HTTP response body as a string.
        /// </summary>
        /// <returns>A Task whose result is the response body as a string.</returns>
        /// <example>s = await url.PostAsync(data).GetString()</example>
        Task<string?> GetStringAsync();

        /// <summary>
        /// Returns HTTP response body as a stream.
        /// </summary>
        /// <returns>A Task whose result is the response body as a stream.</returns>
        /// <example>stream = await url.PostAsync(data).GetStream()</example>
        Task<Stream> GetStreamAsync();

        /// <summary>
        /// Returns HTTP response body as a byte array.
        /// </summary>
        /// <returns>A Task whose result is the response body as a byte array.</returns>
        /// <example>bytes = await url.PostAsync(data).GetBytes()</example>
        Task<byte[]?> GetBytesAsync();
    }
}