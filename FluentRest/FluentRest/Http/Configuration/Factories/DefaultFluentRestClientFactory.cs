using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentRest.Urls;

namespace FluentRest.Http.Configuration
{
	/// <summary>
	/// An IFluentRestClientFactory implementation that caches and reuses the same one instance of
	/// FluentRestClient per combination of scheme, host, and port. This is the default
	/// implementation used when calls are made fluently off Urls/strings.
	/// </summary>
	public class DefaultFluentRestClientFactory : FluentRestClientFactoryBase
	{
		/// <summary>
		/// Returns a unique cache key based on scheme, host, and port of the given URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The cache key</returns>
		protected override string GetCacheKey(Url url) => $"{url.Scheme}|{url.Host}|{url.Port}";
	}
}