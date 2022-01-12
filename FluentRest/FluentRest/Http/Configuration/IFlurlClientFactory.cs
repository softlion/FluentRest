using System;
using System.Runtime.CompilerServices;
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

	/// <summary>
	/// Extension methods on IFluentRestClientFactory
	/// </summary>
	public static class FluentRestClientFactoryExtensions
	{
		// https://stackoverflow.com/questions/51563732/how-do-i-lock-when-the-ideal-scope-of-the-lock-object-is-known-only-at-runtime
		private static readonly ConditionalWeakTable<IFluentRestClient, object> _clientLocks = new ConditionalWeakTable<IFluentRestClient, object>();

		/// <summary>
		/// Provides thread-safe access to a specific IFluentRestClient, typically to configure settings and default headers.
		/// The URL is used to find the client, but keep in mind that the same client will be used in all calls to the same host by default.
		/// </summary>
		/// <param name="factory">This IFluentRestClientFactory.</param>
		/// <param name="url">the URL used to find the IFluentRestClient.</param>
		/// <param name="configAction">the action to perform against the IFluentRestClient.</param>
		public static IFluentRestClientFactory ConfigureClient(this IFluentRestClientFactory factory, string url, Action<IFluentRestClient> configAction) {
			var client = factory.Get(url);
			lock (_clientLocks.GetOrCreateValue(client)) {
				configAction(client);
			}
			return factory;
		}
	}
}