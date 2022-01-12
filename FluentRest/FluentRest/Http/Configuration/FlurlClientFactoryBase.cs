using System;
using System.Collections.Concurrent;
using FluentRest.Urls;

namespace FluentRest.Http.Configuration
{
	/// <summary>
	/// Encapsulates a creation/caching strategy for IFluentRestClient instances. Custom factories looking to extend
	/// FluentRest's behavior should inherit from this class, rather than implementing IFluentRestClientFactory directly.
	/// </summary>
	public abstract class FluentRestClientFactoryBase : IFluentRestClientFactory
	{
		private readonly ConcurrentDictionary<string, IFluentRestClient> _clients = new ConcurrentDictionary<string, IFluentRestClient>();

		/// <summary>
		/// By default, uses a caching strategy of one FluentRestClient per host. This maximizes reuse of
		/// underlying HttpClient/Handler while allowing things like cookies to be host-specific.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The FluentRestClient instance.</returns>
		public virtual IFluentRestClient Get(Url url) {
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			return _clients.AddOrUpdate(
				GetCacheKey(url),
				u => Create(u),
				(u, client) => client.IsDisposed ? Create(u) : client);
		}

		/// <summary>
		/// Defines a strategy for getting a cache key based on a Url. Default implementation
		/// returns the host part (i.e www.api.com) so that all calls to the same host use the
		/// same FluentRestClient (and HttpClient/HttpMessageHandler) instance.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The cache key</returns>
		protected abstract string GetCacheKey(Url url);

		/// <summary>
		/// Creates a new FluentRestClient
		/// </summary>
		/// <param name="url">The URL (not used)</param>
		/// <returns></returns>
		protected virtual IFluentRestClient Create(Url url) => new FluentRestClient();

		/// <summary>
		/// Disposes all cached IFluentRestClient instances and clears the cache.
		/// </summary>
		public void Dispose() {
			foreach (var kv in _clients) {
				if (!kv.Value.IsDisposed)
					kv.Value.Dispose();
			}
			_clients.Clear();
		}
	}
}
