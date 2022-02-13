using System;

namespace FluentRest.Http.Configuration
{
    /// <summary>
    /// Client-level settings for FluentRest.Http
    /// </summary>
    public class ClientFluentRestHttpSettings : FluentRestHttpSettings
    {
        /// <summary>
        /// Specifies the time to keep the underlying HTTP/TCP connection open. When expired, a Connection: close header
        /// is sent with the next request, which should force a new connection and DSN lookup to occur on the next call.
        /// Default is null, effectively disabling the behavior.
        /// </summary>
        public TimeSpan? ConnectionLeaseTimeout {
            get => Get<TimeSpan?>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a factory used to create the HttpClient and HttpMessageHandler used for HTTP calls.
        /// Whenever possible, custom factory implementations should inherit from DefaultHttpClientFactory,
        /// only override the method(s) needed, call the base method, and modify the result.
        /// </summary>
        public IHttpClientFactory HttpClientFactory {
            get => Get<IHttpClientFactory>();
            set => Set(value);
        }
    }
}