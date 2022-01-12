using System.Net.Http;

namespace FluentRest.Http.Configuration
{
	/// <summary>
	/// Interface defining creation of HttpClient and HttpMessageHandler used in all FluentRest HTTP calls.
	/// Implementation can be added via FluentRestHttp.Configure. However, in order not to lose much of
	/// FluentRest.Http's functionality, it's almost always best to inherit DefaultHttpClientFactory and
	/// extend the base implementations, rather than implementing this interface directly.
	/// </summary>
	public interface IHttpClientFactory
	{
		/// <summary>
		/// Defines how HttpClient should be instantiated and configured by default. Do NOT attempt
		/// to cache/reuse HttpClient instances here - that should be done at the FluentRestClient level
		/// via a custom FluentRestClientFactory that gets registered globally.
		/// </summary>
		/// <param name="handler">The HttpMessageHandler used to construct the HttpClient.</param>
		/// <returns></returns>
		HttpClient CreateHttpClient(HttpMessageHandler handler);

		/// <summary>
		/// Defines how the 
		/// </summary>
		/// <returns></returns>
		HttpMessageHandler CreateMessageHandler();
	}
}