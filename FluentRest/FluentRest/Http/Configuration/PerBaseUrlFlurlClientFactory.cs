using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentRest.Urls;

namespace FluentRest.Http.Configuration
{
	/// <summary>
	/// An IFluentRestClientFactory implementation that caches and reuses the same IFluentRestClient instance
	/// per URL requested, which it assumes is a "base" URL, and sets the IFluentRestClient.BaseUrl property
	/// to that value. Ideal for use with IoC containers - register as a singleton, inject into a service
	/// that wraps some web service, and use to set a private IFluentRestClient field in the constructor.
	/// </summary>
	public class PerBaseUrlFluentRestClientFactory : FluentRestClientFactoryBase
	{
		/// <summary>
		/// Returns the entire URL, which is assumed to be some "base" URL for a service.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The cache key</returns>
		protected override string GetCacheKey(Url url) => url.ToString();

		/// <summary>
		/// Returns a new new FluentRestClient with BaseUrl set to the URL passed.
		/// </summary>
		/// <param name="url">The URL</param>
		/// <returns></returns>
		protected override IFluentRestClient Create(Url url) => new FluentRestClient(url);
	}
}
