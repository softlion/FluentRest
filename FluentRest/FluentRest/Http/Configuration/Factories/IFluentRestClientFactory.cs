using System;
using FluentRest.Urls;

namespace FluentRest.Http.Configuration
{
	/// <summary>
	/// Interface for defining a strategy for creating, caching, and reusing IFluentRestClient instances and,
	/// by proxy, their underlying HttpClient instances.
	/// </summary>
	public interface IFluentRestClientFactory : IDisposable
	{
		/// <summary>
		/// Strategy to create a FluentRestClient or reuse an exisitng one, based on URL being called.
		/// </summary>
		/// <param name="url">The URL being called.</param>
		/// <returns></returns>
		IFluentRestClient Get(Url url);
	}
}