using System;

namespace FluentRest.Http.Configuration
{
    /// <summary>
    /// Global default settings for FluentRest.Http
    /// </summary>
    public class GlobalFluentRestHttpSettings : ClientFluentRestHttpSettings
    {
        internal GlobalFluentRestHttpSettings() {
            ResetDefaults();
        }

        /// <summary>
        /// Defaults at the global level do not make sense and will always be null.
        /// </summary>
        public override FluentRestHttpSettings Defaults {
            get => null;
            set => throw new Exception("Global settings cannot be backed by any higher-level defauts.");
        }

        /// <summary>
        /// Gets or sets the factory that defines creating, caching, and reusing FluentRestClient instances and,
        /// by proxy, HttpClient instances.
        /// </summary>
        public IFluentRestClientFactory FluentRestClientFactory {
            get => Get<IFluentRestClientFactory>();
            set => Set(value);
        }

        /// <summary>
        /// Resets all global settings to their default values.
        /// </summary>
        public override void ResetDefaults() {
            base.ResetDefaults();
            Timeout = TimeSpan.FromSeconds(100); // same as HttpClient
            JsonSerializer = new SystemTextJsonSerializer(null);
            UrlEncodedSerializer = new DefaultUrlEncodedSerializer();
            FluentRestClientFactory = new DefaultFluentRestClientFactory();
            HttpClientFactory = new DefaultHttpClientFactory();
            Redirects.Enabled = true;
            Redirects.AllowSecureToInsecure = false;
            Redirects.ForwardHeaders = false;
            Redirects.ForwardAuthorizationHeader = false;
            Redirects.MaxAutoRedirects = 10;
        }
    }
}