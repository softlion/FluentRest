using System;
using System.Net.Http;
using FluentRest.Http.Configuration;
using FluentRest.Http.Testing;
using FluentRest.Urls;

namespace FluentRest.Http
{
    /// <summary>
    /// A reusable object for making HTTP calls.
    /// </summary>
    public class FluentRestClient : IFluentRestClient
    {
        private ClientFluentRestHttpSettings _settings;
        private Lazy<HttpClient> _httpClient;
        private Lazy<HttpMessageHandler> _httpMessageHandler;

        // if an existing HttpClient is provided on construction, skip the lazy logic and just use that.
        private readonly HttpClient _injectedClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentRestClient"/> class.
        /// </summary>
        /// <param name="baseUrl">The base URL associated with this client.</param>
        public FluentRestClient(string baseUrl = null) {
            _httpClient = new Lazy<HttpClient>(CreateHttpClient);
            _httpMessageHandler = new Lazy<HttpMessageHandler>(() => Settings.HttpClientFactory.CreateMessageHandler());
            BaseUrl = baseUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentRestClient"/> class, wrapping an existing HttpClient.
        /// Generally you should let FluentRest create and manage HttpClient instances for you, but you might, for
        /// example, have an HttpClient instance that was created by a 3rd-party library and you want to use
        /// FluentRest to build and send calls with it.
        /// </summary>
        /// <param name="httpClient">The instantiated HttpClient instance.</param>
        public FluentRestClient(HttpClient httpClient) {
            _injectedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            BaseUrl = httpClient.BaseAddress?.ToString();
        }

        /// <inheritdoc />
        public string BaseUrl { get; set; }

        /// <inheritdoc />
        public ClientFluentRestHttpSettings Settings {
            get => _settings ?? (_settings = new ClientFluentRestHttpSettings());
            set => _settings = value;
        }

        /// <inheritdoc />
        public INameValueList<string> Headers { get; } = new NameValueList<string>(false); // header names are case-insensitive https://stackoverflow.com/a/5259004/62600

        /// <inheritdoc />
        public HttpClient HttpClient => HttpTest.Current?.HttpClient ?? _injectedClient ?? GetHttpClient();

        private DateTime? _clientCreatedAt;
        private HttpClient _zombieClient;
        private readonly object _connectionLeaseLock = new object();

        private HttpClient GetHttpClient() {
            if (ConnectionLeaseExpired()) {
                lock (_connectionLeaseLock) {
                    if (ConnectionLeaseExpired()) {
                        // when the connection lease expires, force a new HttpClient to be created, but don't
                        // dispose the old one just yet - it might have pending requests. Instead, it becomes
                        // a zombie and is disposed on the _next_ lease timeout, which should be safe.
                        _zombieClient?.Dispose();
                        _zombieClient = _httpClient.Value;
                        _httpClient = new Lazy<HttpClient>(CreateHttpClient);
                        _httpMessageHandler = new Lazy<HttpMessageHandler>(() => Settings.HttpClientFactory.CreateMessageHandler());
                        _clientCreatedAt = DateTime.UtcNow;
                    }
                }
            }
            return _httpClient.Value;
        }

        private HttpClient CreateHttpClient() {
            var cli = Settings.HttpClientFactory.CreateHttpClient(HttpMessageHandler);
            _clientCreatedAt = DateTime.UtcNow;
            return cli;
        }

        private bool ConnectionLeaseExpired() {
            // for thread safety, capture these to temp variables
            var createdAt = _clientCreatedAt;
            var timeout = Settings.ConnectionLeaseTimeout;

            return
                _httpClient.IsValueCreated &&
                createdAt.HasValue &&
                timeout.HasValue &&
                DateTime.UtcNow - createdAt.Value > timeout.Value;
        }

        /// <inheritdoc />
        public HttpMessageHandler HttpMessageHandler => HttpTest.Current?.HttpMessageHandler ?? _httpMessageHandler?.Value;

        /// <inheritdoc />
        public IFluentRestRequest Request(params object[] urlSegments) =>
            new FluentRestRequest(BaseUrl, urlSegments).WithClient(this);

        FluentRestHttpSettings IHttpSettingsContainer.Settings {
            get => Settings;
            set => Settings = value as ClientFluentRestHttpSettings;
        }

        /// <inheritdoc />
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Disposes the underlying HttpClient and HttpMessageHandler.
        /// </summary>
        public virtual void Dispose() {
            if (IsDisposed)
                return;

            _injectedClient?.Dispose();
            if (_httpMessageHandler?.IsValueCreated == true)
                _httpMessageHandler.Value.Dispose();
            if (_httpClient?.IsValueCreated == true)
                _httpClient.Value.Dispose();

            IsDisposed = true;
        }
    }
}