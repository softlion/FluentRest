using FluentRest.Http.Configuration;
using FluentRest.Urls;

namespace FluentRest.Http
{
	/// <summary>
	/// Defines stateful aspects (headers, cookies, etc) common to both IFluentRestClient and IFluentRestRequest
	/// </summary>
	public interface IHttpSettingsContainer
	{
	    /// <summary>
	    /// Gets or sets the FluentRestHttpSettings object used by this client or request.
	    /// </summary>
	    FluentRestHttpSettings Settings { get; set; }

		/// <summary>
		/// Collection of headers sent on this request or all requests using this client.
		/// </summary>
		INameValueList<string> Headers { get; }
    }
}
