using System.Net.Http;
using System.Threading.Tasks;
using FluentRest.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluentRest.Test.Http
{
	[TestClass]
	public class RedirectTests : HttpTestFixtureBase
	{
		[TestMethod]
		public async Task can_auto_redirect() {
			HttpTest
				.RespondWith("", 302, new { Location = "http://redir.com/foo" })
				.RespondWith("", 302, new { Location = "/redir2" })
				.RespondWith("", 302, new { Location = "redir3?x=1&y=2#foo" })
				.RespondWith("", 302, new { Location = "//otherredir.com/bar/?a=b" })
				.RespondWith("done!");

			var resp = await "http://start.com".PostStringAsync("foo!").ReceiveString();

			Assert.AreEqual("done!", resp);
			HttpTest.ShouldHaveMadeACall().Times(5);
			HttpTest.ShouldHaveCalled("http://start.com").WithVerb(HttpMethod.Post).WithRequestBody("foo!")
				.With(call => call.RedirectedFrom == null);
			HttpTest.ShouldHaveCalled("http://redir.com/foo").WithVerb(HttpMethod.Get).WithRequestBody("")
				.With(call => call.RedirectedFrom.Request.Url.ToString() == "http://start.com");
			HttpTest.ShouldHaveCalled("http://redir.com/redir2").WithVerb(HttpMethod.Get).WithRequestBody("")
				.With(call => call.RedirectedFrom.Request.Url.ToString() == "http://redir.com/foo");
			HttpTest.ShouldHaveCalled("http://redir.com/redir2/redir3?x=1&y=2#foo").WithVerb(HttpMethod.Get).WithRequestBody("")
				.With(call => call.RedirectedFrom.Request.Url.ToString() == "http://redir.com/redir2");
			HttpTest.ShouldHaveCalled("http://otherredir.com/bar/?a=b#foo").WithVerb(HttpMethod.Get).WithRequestBody("")
				.With(call => call.RedirectedFrom.Request.Url.ToString() == "http://redir.com/redir2/redir3?x=1&y=2#foo");
		}

		[TestMethod]
		public async Task redirect_location_inherits_fragment_when_none() {
			HttpTest
				.RespondWith("", 302, new { Location = "/redir1" })
				.RespondWith("", 302, new { Location = "/redir2#bar" })
				.RespondWith("", 302, new { Location = "/redir3" })
				.RespondWith("done!");
			await "http://start.com?x=y#foo".GetAsync();

			HttpTest.ShouldHaveCalled("http://start.com?x=y#foo");
			// also asserts that they do NOT inherit query params in the same way
			HttpTest.ShouldHaveCalled("http://start.com/redir1#foo");
			HttpTest.ShouldHaveCalled("http://start.com/redir2#bar");
			HttpTest.ShouldHaveCalled("http://start.com/redir3#bar");
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task can_enable_auto_redirect_per_request(bool enabled) {
			HttpTest
				.RespondWith("original", 302, new { Location = "http://redir.com/foo" })
				.RespondWith("redirected");

			// whatever we want at the request level, set it the opposite at the client level
			var fc = new FluentRestClient().WithAutoRedirect(!enabled);

			var result = await fc.Request("http://start.com").WithAutoRedirect(enabled).GetStringAsync();

			Assert.AreEqual(enabled ? "redirected" : "original", result);
			HttpTest.ShouldHaveMadeACall().Times(enabled ? 2 : 1);
		}

		[TestMethod]
		[DataRow(false,false)]
		[DataRow(false,true)]
		[DataRow(true,false)]
		[DataRow(true,true)]
		public async Task can_configure_header_forwarding( bool fwdAuth,  bool fwdOther) {
			HttpTest
				.RespondWith("", 302, new { Location = "/next" })
				.RespondWith("done!");

			await "http://start.com"
				.WithHeaders(new {
					Authorization = "xyz",
					Cookie = "x=foo;y=bar",
					Transfer_Encoding = "chunked",
					Custom1 = "foo",
					Custom2 = "bar"
				})
				.ConfigureRequest(settings => {
					settings.Redirects.ForwardAuthorizationHeader = fwdAuth;
					settings.Redirects.ForwardHeaders = fwdOther;
				})
				.PostAsync(null);

			HttpTest.ShouldHaveCalled("http://start.com")
				.WithHeader("Authorization")
				.WithHeader("Cookie")
				.WithHeader("Transfer-Encoding")
				.WithHeader("Custom1")
				.WithHeader("Custom2");

			HttpTest.ShouldHaveCalled("http://start.com/next")
				.With(call =>
					call.Request.Headers.Contains("Authorization") == fwdAuth &&
					call.Request.Headers.Contains("Custom1") == fwdOther &&
					call.Request.Headers.Contains("Custom2") == fwdOther)
				.WithoutHeader("Cookie") // special rule: never forward this when CookieJar isn't being used
				.WithoutHeader("Transfer-Encoding"); // special rule: never forward this if verb is changed to GET, which is is on a 302 POST
		}

		[TestMethod]
		[DataRow(301, true)]
		[DataRow(302, true)]
		[DataRow(303, true)]
		[DataRow(307, false)]
		[DataRow(308, false)]
		public async Task redirect_preserves_verb_sometimes(int status, bool changeToGet) {
			HttpTest
				.RespondWith("", status, new { Location = "/next" })
				.RespondWith("done!");

			await "http://start.com".PostStringAsync("foo!");

			HttpTest.ShouldHaveCalled("http://start.com/next")
				.WithVerb(changeToGet ? HttpMethod.Get : HttpMethod.Post)
				.WithRequestBody(changeToGet ? "" : "foo!");
		}

		[TestMethod]
		public void can_detect_circular_redirects() {
			HttpTest
				.RespondWith("", 301, new { Location = "/redir1" })
				.RespondWith("", 301, new { Location = "/redir2" })
				.RespondWith("", 301, new { Location = "/redir1" });

			var ex = Assert.ThrowsExceptionAsync<FluentRestHttpException>(() => "http://start.com".GetAsync());
			StringAssert.Contains(ex.Result.Message, "Circular redirect");
		}

		[TestMethod]
		[DataRow(null)] // test the default (10)
		[DataRow(5)]
		public async Task can_limit_redirects(int? max) {
			for (var i = 1; i <= 20; i++)
				HttpTest.RespondWith("", 301, new { Location = "/redir" + i });

			var fc = new FluentRestClient();
			if (max.HasValue)
				fc.Settings.Redirects.MaxAutoRedirects = max.Value;

			await fc.Request("http://start.com").GetAsync();

			var count = max ?? 10;
			HttpTest.ShouldHaveCalled("http://start.com/redir*").Times(count);
			HttpTest.ShouldHaveCalled("http://start.com/redir" + count);
			HttpTest.ShouldNotHaveCalled("http://start.com/redir" + (count + 1));
		}

		[TestMethod]
		public async Task can_change_redirect_behavior_from_event() {
			var eventFired = false;

			HttpTest
				.RespondWith("", 301, new { Location = "/next" })
				.RespondWith("done!");

			var fc = new FluentRestClient()
				.OnRedirect(call => {
					eventFired = true;

					// assert all the properties of call.Redirect
					Assert.IsTrue(call.Redirect.Follow);
					Assert.AreEqual("http://start.com/next", call.Redirect.Url.ToString());
					Assert.AreEqual(1, call.Redirect.Count);
					Assert.IsTrue(call.Redirect.ChangeVerbToGet);

					// now change the behavior
					call.Redirect.Url.SetQueryParam("x", 999);
					call.Redirect.ChangeVerbToGet = false;
				});

			await fc.Request("http://start.com").PostStringAsync("foo!");

			Assert.IsTrue(eventFired);

			HttpTest.ShouldHaveCalled("http://start.com/next?x=999")
				.WithVerb(HttpMethod.Post)
				.WithRequestBody("foo!");
		}

		[TestMethod]
		public async Task can_block_redirect_from_event() {
			HttpTest
				.RespondWith("", 301, new { Location = "/next" })
				.RespondWith("done!");

			var fc = new FluentRestClient();
			await fc.Request("http://start.com")
				.OnRedirect(call => call.Redirect.Follow = false)
				.GetAsync();

			HttpTest.ShouldNotHaveCalled("http://start.com/next");
		}

		[TestMethod]
		public async Task can_disable_redirect() {
			HttpTest
				.RespondWith("", 301, new { Location = "/next" })
				.RespondWith("done!");

			var fc = new FluentRestClient();
			fc.Settings.Redirects.Enabled = false;
			await fc.Request("http://start.com").GetAsync();

			HttpTest.ShouldNotHaveCalled("http://start.com/next");
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task can_allow_redirect_secure_to_insecure(bool allow) {
			HttpTest
				.RespondWith("", 301, new { Location = "http://insecure.com/next" })
				.RespondWith("done!");

			var fc = new FluentRestClient();
			if (allow) // test that false is default (don't set explicitly)
				fc.Settings.Redirects.AllowSecureToInsecure = true;

			await fc.Request("https://secure.com").GetAsync();

			if (allow)
				HttpTest.ShouldHaveCalled("http://insecure.com/next");
			else
				HttpTest.ShouldNotHaveCalled("http://insecure.com/next");
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task can_allow_forward_auth_header(bool allow) {
			HttpTest
				.RespondWith("", 301, new { Location = "/next" })
				.RespondWith("done!");

			var fc = new FluentRestClient();
			if (allow) // test that false is default (don't set explicitly)
				fc.Settings.Redirects.ForwardAuthorizationHeader = true;

			await fc.Request("http://start.com")
				.WithHeader("Authorization", "foo")
				.GetAsync();

			if (allow)
				HttpTest.ShouldHaveCalled("http://start.com/next").WithHeader("Authorization", "foo");
			else
				HttpTest.ShouldHaveCalled("http://start.com/next").WithoutHeader("Authorization");
		}
	}
}
