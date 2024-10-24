using System;
using System.Net.Http;
using FluentRest.Http.Configuration;

namespace FluentRest.Http
{
	/// <summary>
	/// Interface defining FluentRestClient's contract (useful for mocking and DI)
	/// </summary>
	public interface IFluentRestClient : IHttpSettingsContainer, IDisposable {
		/// <summary>
		/// Gets or sets the FluentRestHttpSettings object used by this client.
		/// </summary>
		new ClientFluentRestHttpSettings Settings { get; set; }

		/// <summary>
		/// Gets the HttpClient to be used in subsequent HTTP calls. Creation (when necessary) is delegated
		/// to FluentRestHttp.FluentRestClientFactory. Reused for the life of the FluentRestClient.
		/// </summary>
		HttpClient HttpClient { get; }

		/// <summary>
		/// Gets the HttpMessageHandler to be used in subsequent HTTP calls. Creation (when necessary) is delegated
		/// to FluentRestHttp.FluentRestClientFactory.
		/// </summary>
		HttpMessageHandler HttpMessageHandler { get; }

		/// <summary>
		/// Gets or sets base URL associated with this client.
		/// </summary>
		string? BaseUrl { get; set; }

		/// <summary>
		/// Creates a new IFluentRestRequest that can be further built and sent fluently.
		/// </summary>
		/// <param name="urlSegments">The URL or URL segments for the request. If BaseUrl is defined, it is assumed that these are path segments off that base.</param>
		/// <returns>A new IFluentRestRequest</returns>
		IFluentRestRequest Request(params object[] urlSegments);

		/// <summary>
		/// Gets a value indicating whether this instance (and its underlying HttpClient) has been disposed.
		/// </summary>
		bool IsDisposed { get; }
	}
}