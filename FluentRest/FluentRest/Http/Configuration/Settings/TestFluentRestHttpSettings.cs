using FluentRest.Http.Testing;

namespace FluentRest.Http.Configuration
{
	/// <summary>
	/// Settings overrides within the context of an HttpTest
	/// </summary>
	public class TestFluentRestHttpSettings : ClientFluentRestHttpSettings
	{
		/// <summary>
		/// Resets all test settings to their FluentRest.Http-defined default values.
		/// </summary>
		public override void ResetDefaults() {
			base.ResetDefaults();
			HttpClientFactory = new TestHttpClientFactory();
		}
	}
}
