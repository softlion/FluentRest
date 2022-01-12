using System;
using FluentRest.Http.Configuration;

namespace FluentRest.Http
{
	/// <summary>
	/// A static container for global configuration settings affecting FluentRest.Http behavior.
	/// </summary>
	public static class FluentRestHttp
	{
		private static readonly object _configLock = new object();

		private static Lazy<GlobalFluentRestHttpSettings> _settings =
			new Lazy<GlobalFluentRestHttpSettings>(() => new GlobalFluentRestHttpSettings());

		/// <summary>
		/// Globally configured FluentRest.Http settings. Should normally be written to by calling FluentRestHttp.Configure once application at startup.
		/// </summary>
		public static GlobalFluentRestHttpSettings GlobalSettings => _settings.Value;

		/// <summary>
		/// Provides thread-safe access to FluentRest.Http's global configuration settings. Should only be called once at application startup.
		/// </summary>
		/// <param name="configAction">the action to perform against the GlobalSettings.</param>
		public static void Configure(Action<GlobalFluentRestHttpSettings> configAction) {
			lock (_configLock) {
				configAction(GlobalSettings);
			}
		}

		/// <summary>
		/// Provides thread-safe access to a specific IFluentRestClient, typically to configure settings and default headers.
		/// The URL is used to find the client, but keep in mind that the same client will be used in all calls to the same host by default.
		/// </summary>
		/// <param name="url">the URL used to find the IFluentRestClient.</param>
		/// <param name="configAction">the action to perform against the IFluentRestClient.</param>
		public static void ConfigureClient(string url, Action<IFluentRestClient> configAction) => 
			GlobalSettings.FluentRestClientFactory.ConfigureClient(url, configAction);
	}
}