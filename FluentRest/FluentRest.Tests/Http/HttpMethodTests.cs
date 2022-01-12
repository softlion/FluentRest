using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentRest.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Urls;

namespace FluentRest.Test.Http
{
	/// <summary>
	/// Each HTTP method with first-class support in FluentRest (via PostAsync, GetAsync, etc.) should
	/// have a test fixture that inherits from this base class.
	/// </summary>
	public abstract class HttpMethodTests : HttpTestFixtureBase
	{
		private readonly HttpMethod _verb;

		protected HttpMethodTests(HttpMethod verb) {
			_verb = verb;
		}

		protected abstract Task<IFluentRestResponse> CallOnString(string url);
		protected abstract Task<IFluentRestResponse> CallOnUrl(Url url);
		protected abstract Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req);

		[TestMethod]
		public async Task can_call_on_FluentRestClient() {
			var resp = await CallOnFluentRestRequest(new FluentRestRequest("http://www.api.com"));
			Assert.AreEqual(200, resp.StatusCode);
			HttpTest.ShouldHaveCalled("http://www.api.com").WithVerb(_verb).Times(1);
		}

		[TestMethod]
		public async Task can_call_on_string() {
			var resp = await CallOnString("http://www.api.com");
			Assert.AreEqual(200, resp.StatusCode);
			HttpTest.ShouldHaveCalled("http://www.api.com").WithVerb(_verb).Times(1);
		}

		[TestMethod]
		public async Task can_call_on_url() {
			var resp = await CallOnUrl(new Url("http://www.api.com"));
			Assert.AreEqual(200, resp.StatusCode);
			HttpTest.ShouldHaveCalled("http://www.api.com").WithVerb(_verb).Times(1);
		}
	}

	[TestClass]
	public class PutTests : HttpMethodTests
	{
		public PutTests() : base(HttpMethod.Put) { }
		protected override Task<IFluentRestResponse> CallOnString(string url) => url.PutAsync(null);
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.PutAsync(null);
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.PutAsync(null);
	}

	[TestClass]
	public class PatchTests : HttpMethodTests
	{
		public PatchTests() : base(new HttpMethod("PATCH")) { }
		protected override Task<IFluentRestResponse> CallOnString(string url) => url.PatchAsync(null);
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.PatchAsync(null);
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.PatchAsync(null);
	}

	[TestClass]
	public class DeleteTests : HttpMethodTests
	{
		public DeleteTests() : base(HttpMethod.Delete) { }
		protected override Task<IFluentRestResponse> CallOnString(string url) => url.DeleteAsync();
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.DeleteAsync();
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.DeleteAsync();
	}

	[TestClass]
	public class HeadTests : HttpMethodTests
	{
		public HeadTests() : base(HttpMethod.Head) { }
		protected override Task<IFluentRestResponse> CallOnString(string url) => url.HeadAsync();
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.HeadAsync();
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.HeadAsync();
	}

	[TestClass]
	public class OptionsTests : HttpMethodTests
	{
		public OptionsTests() : base(HttpMethod.Options) { }
		protected override Task<IFluentRestResponse> CallOnString(string url) => url.OptionsAsync();
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.OptionsAsync();
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.OptionsAsync();
	}
}
