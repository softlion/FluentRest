using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentRest.Http;
using FluentRest.Http.Configuration;
using FluentRest.Http.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Rest.Configuration;

namespace FluentRest.Test.Http
{
	/// <summary>
	/// FluentRestHttpSettings are available at the global, test, client, and request level. This abstract class
	/// allows the same tests to be run against settings at all 4 levels.
	/// </summary>
	public abstract class SettingsTestsBase
	{
		protected abstract FluentRestHttpSettings GetSettings();
		protected abstract IFluentRestRequest GetRequest();

		[TestMethod]
		public async Task can_allow_non_success_status() {
			using (var test = new HttpTest()) {
				GetSettings().AllowedHttpStatusRange = "4xx";
				test.RespondWith("I'm a teapot", 418);
				try {
					var result = await GetRequest().GetAsync();
					Assert.AreEqual(418, result.StatusCode);
				}
				catch (Exception) {
					Assert.Fail("Exception should not have been thrown.");
				}
			}
		}

		[TestMethod]
		public async Task can_set_pre_callback() {
			var callbackCalled = false;
			using (var test = new HttpTest()) {
				test.RespondWith("ok");
				GetSettings().BeforeCall = call => {
					Assert.IsNull(call.Response); // verifies that callback is running before HTTP call is made
					callbackCalled = true;
				};
				Assert.IsFalse(callbackCalled);
				await GetRequest().GetAsync();
				Assert.IsTrue(callbackCalled);
			}
		}

		[TestMethod]
		public async Task can_set_post_callback() {
			var callbackCalled = false;
			using (var test = new HttpTest()) {
				test.RespondWith("ok");
				GetSettings().AfterCall = call => {
					Assert.IsNotNull(call.Response); // verifies that callback is running after HTTP call is made
					callbackCalled = true;
				};
				Assert.IsFalse(callbackCalled);
				await GetRequest().GetAsync();
				Assert.IsTrue(callbackCalled);
			}
		}

		[DataRow(true)]
		[DataRow(false)]
		public async Task can_set_error_callback(bool markExceptionHandled) {
			var callbackCalled = false;
			using (var test = new HttpTest()) {
				test.RespondWith("server error", 500);
				GetSettings().OnError = call => {
					Assert.IsNotNull(call.Response); // verifies that callback is running after HTTP call is made
					callbackCalled = true;
					call.ExceptionHandled = markExceptionHandled;
				};
				Assert.IsFalse(callbackCalled);
				try {
					await GetRequest().GetAsync();
					Assert.IsTrue(callbackCalled, "OnError was never called");
					Assert.IsTrue(markExceptionHandled, "ExceptionHandled was marked false in callback, but exception was not propagated.");
				}
				catch (FluentRestHttpException) {
					Assert.IsTrue(callbackCalled, "OnError was never called");
					Assert.IsFalse(markExceptionHandled, "ExceptionHandled was marked true in callback, but exception was propagated.");
				}
			}
		}

		[TestMethod]
		public async Task can_disable_exception_behavior() {
			using (var test = new HttpTest()) {
				GetSettings().OnError = call => {
					call.ExceptionHandled = true;
				};
				test.RespondWith("server error", 500);
				try {
					var result = await GetRequest().GetAsync();
					Assert.AreEqual(500, result.StatusCode);
				}
				catch (FluentRestHttpException) {
					Assert.Fail("FluentRest should not have thrown exception.");
				}
			}
		}

		[TestMethod]
		public void can_reset_defaults() {
			GetSettings().JsonSerializer = null;
			GetSettings().Redirects.Enabled = false;
			GetSettings().BeforeCall = (call) => Console.WriteLine("Before!");
			GetSettings().Redirects.MaxAutoRedirects = 5;

			Assert.IsNull(GetSettings().JsonSerializer);
			Assert.IsFalse(GetSettings().Redirects.Enabled);
			Assert.IsNotNull(GetSettings().BeforeCall);
			Assert.AreEqual(5, GetSettings().Redirects.MaxAutoRedirects);

			GetSettings().ResetDefaults();

			Assert.IsTrue(GetSettings().Redirects.Enabled);
			Assert.IsNull(GetSettings().BeforeCall);
			Assert.AreEqual(10, GetSettings().Redirects.MaxAutoRedirects);
		}

		[TestMethod] // #256
		public async Task explicit_content_type_header_is_not_overridden()
		{
			using var test = new HttpTest();
			// PostJsonAsync will normally set Content-Type to application/json, but it shouldn't touch it if it was set explicitly.
			await "https://api.com"
				.WithHeader("content-type", "application/json-patch+json; utf-8")
				.WithHeader("CONTENT-LENGTH", 10) // header names are case-insensitive
				.PostJsonAsync(new { foo = "bar" });

			var h = test.CallLog[0].HttpRequestMessage.Content.Headers;
			CollectionAssert.AreEqual(new[] { "application/json-patch+json; utf-8" }, h.GetValues("Content-Type").ToList());
			CollectionAssert.AreEqual(new[] { "10" }, h.GetValues("Content-Length").ToList());
		}
	}

	[TestClass] // touches global settings, so can't run in parallel
	public class GlobalSettingsTests : SettingsTestsBase
	{
		protected override FluentRestHttpSettings GetSettings() => FluentRestHttp.GlobalSettings;
		protected override IFluentRestRequest GetRequest() => new FluentRestRequest("http://api.com");

		[TestCleanup]
		public void ResetDefaults() => FluentRestHttp.GlobalSettings.ResetDefaults();

		[TestMethod]
		public void settings_propagate_correctly() {
			FluentRestHttp.GlobalSettings.Redirects.Enabled = false;
			FluentRestHttp.GlobalSettings.AllowedHttpStatusRange = "4xx";
			FluentRestHttp.GlobalSettings.Redirects.MaxAutoRedirects = 123;

			var client1 = new FluentRestClient();
			client1.Settings.Redirects.Enabled = true;
			Assert.AreEqual("4xx", client1.Settings.AllowedHttpStatusRange);
			Assert.AreEqual(123, client1.Settings.Redirects.MaxAutoRedirects);
			client1.Settings.AllowedHttpStatusRange = "5xx";
			client1.Settings.Redirects.MaxAutoRedirects = 456;

			var req = client1.Request("http://myapi.com");
			Assert.IsTrue(req.Settings.Redirects.Enabled, "request should inherit client settings when not set at request level");
			Assert.AreEqual("5xx", req.Settings.AllowedHttpStatusRange, "request should inherit client settings when not set at request level");
			Assert.AreEqual(456, req.Settings.Redirects.MaxAutoRedirects, "request should inherit client settings when not set at request level");

			var client2 = new FluentRestClient();
			client2.Settings.Redirects.Enabled = false;

			req.WithClient(client2);
			Assert.IsFalse(req.Settings.Redirects.Enabled, "request should inherit client settings when not set at request level");
			Assert.AreEqual("4xx", req.Settings.AllowedHttpStatusRange, "request should inherit global settings when not set at request or client level");
			Assert.AreEqual(123, req.Settings.Redirects.MaxAutoRedirects, "request should inherit global settings when not set at request or client level");

			client2.Settings.Redirects.Enabled = true;
			client2.Settings.AllowedHttpStatusRange = "3xx";
			client2.Settings.Redirects.MaxAutoRedirects = 789;
			Assert.IsTrue(req.Settings.Redirects.Enabled, "request should inherit client settings when not set at request level");
			Assert.AreEqual("3xx", req.Settings.AllowedHttpStatusRange, "request should inherit client settings when not set at request level");
			Assert.AreEqual(789, req.Settings.Redirects.MaxAutoRedirects, "request should inherit client settings when not set at request level");

			req.Settings.Redirects.Enabled = false;
			req.Settings.AllowedHttpStatusRange = "6xx";
			req.Settings.Redirects.MaxAutoRedirects = 2;
			Assert.IsFalse(req.Settings.Redirects.Enabled, "request-level settings should override any defaults");
			Assert.AreEqual("6xx", req.Settings.AllowedHttpStatusRange, "request-level settings should override any defaults");
			Assert.AreEqual(2, req.Settings.Redirects.MaxAutoRedirects, "request-level settings should override any defaults");

			req.Settings.ResetDefaults();
			Assert.IsTrue(req.Settings.Redirects.Enabled, "request should inherit client settings when cleared at request level");
			Assert.AreEqual("3xx", req.Settings.AllowedHttpStatusRange, "request should inherit client settings when cleared request level");
			Assert.AreEqual(789, req.Settings.Redirects.MaxAutoRedirects, "request should inherit client settings when cleared request level");

			client2.Settings.ResetDefaults();
			Assert.IsFalse(req.Settings.Redirects.Enabled, "request should inherit global settings when cleared at request and client level");
			Assert.AreEqual("4xx", req.Settings.AllowedHttpStatusRange, "request should inherit global settings when cleared at request and client level");
			Assert.AreEqual(123, req.Settings.Redirects.MaxAutoRedirects, "request should inherit global settings when cleared at request and client level");
		}

		[TestMethod]
		public void can_provide_custom_client_factory() {
			FluentRestHttp.GlobalSettings.HttpClientFactory = new SomeCustomHttpClientFactory();
			Assert.IsInstanceOfType(GetRequest().Client.HttpClient, typeof(SomeCustomHttpClient));
			Assert.IsInstanceOfType(GetRequest().Client.HttpMessageHandler, typeof(SomeCustomMessageHandler));
		}

		[TestMethod]
		public void can_configure_global_from_FluentRestHttp_object() {
			FluentRestHttp.Configure(settings => settings.Redirects.Enabled = false);
			Assert.IsFalse(FluentRestHttp.GlobalSettings.Redirects.Enabled);
		}

		[TestMethod]
		public void can_configure_client_from_FluentRestHttp_object() {
			FluentRestHttp.ConfigureClient("http://host1.com/foo", cli => cli.Settings.Redirects.Enabled = false);
			Assert.IsFalse(new FluentRestRequest("http://host1.com/bar").Client.Settings.Redirects.Enabled); // different URL but same host, so should use same client
			Assert.IsTrue(new FluentRestRequest("http://host2.com").Client.Settings.Redirects.Enabled);
		}
	}

	[TestClass]
	public class HttpTestSettingsTests : SettingsTestsBase
	{
		private HttpTest _test;

		[TestInitialize]
		public void CreateTest() => _test = new HttpTest();

		[TestCleanup]
		public void DisposeTest() => _test.Dispose();

		protected override FluentRestHttpSettings GetSettings() => HttpTest.Current.Settings;
		protected override IFluentRestRequest GetRequest() => new FluentRestRequest("http://api.com");

		[TestMethod] // #246
		public void test_settings_dont_override_request_settings_when_not_set_explicitily() {
			var ser1 = new FakeSerializer();
			var ser2 = new FakeSerializer();

			using (var test = new HttpTest()) {
				var cli = new FluentRestClient();
				cli.Settings.JsonSerializer = ser1;
				Assert.AreSame(ser1, cli.Settings.JsonSerializer);

				var req = new FluentRestRequest { Client = cli };
				Assert.AreSame(ser1, req.Settings.JsonSerializer);

				req.Settings.JsonSerializer = ser2;
				Assert.AreSame(ser2, req.Settings.JsonSerializer);
			}
		}

		private class FakeSerializer : ISerializer
		{
			public string Serialize(object obj) => "foo";
			public T Deserialize<T>(string s) => default(T);
			public T Deserialize<T>(Stream stream) => default(T);
		}
	}

	[TestClass]
	public class ClientSettingsTests : SettingsTestsBase
	{
		private readonly Lazy<IFluentRestClient> _client = new Lazy<IFluentRestClient>(() => new FluentRestClient());

		protected override FluentRestHttpSettings GetSettings() => _client.Value.Settings;
		protected override IFluentRestRequest GetRequest() => _client.Value.Request("http://api.com");

		[TestMethod]
		public void can_provide_custom_client_factory() {
			var cli = new FluentRestClient();
			cli.Settings.HttpClientFactory = new SomeCustomHttpClientFactory();
			Assert.IsInstanceOfType(cli.HttpClient, typeof(SomeCustomHttpClient));
			Assert.IsInstanceOfType(cli.HttpMessageHandler, typeof(SomeCustomMessageHandler));
		}

		[TestMethod]
		public async Task connection_lease_timeout_creates_new_HttpClient() {
			var cli = new FluentRestClient("http://api.com");
			cli.Settings.ConnectionLeaseTimeout = TimeSpan.FromMilliseconds(50);
			var hc = cli.HttpClient;

			await Task.Delay(25);
			Assert.IsTrue(hc == cli.HttpClient);

			// exceed the timeout
			await Task.Delay(25);
			Assert.IsTrue(hc != cli.HttpClient);
		}
	}

	[TestClass]
	public class RequestSettingsTests : SettingsTestsBase
	{
		private readonly Lazy<IFluentRestRequest> _req = new Lazy<IFluentRestRequest>(() => new FluentRestRequest("http://api.com"));

		protected override FluentRestHttpSettings GetSettings() => _req.Value.Settings;
		protected override IFluentRestRequest GetRequest() => _req.Value;

		[TestMethod] // #239
		public void request_default_settings_change_when_client_changes() {
			FluentRestHttp.ConfigureClient("http://test.com", cli => cli.Settings.Redirects.Enabled = false);
			var req = new FluentRestRequest("http://test.com");
			var cli1 = req.Client;
			Assert.IsFalse(req.Settings.Redirects.Enabled, "pre-configured client should provide defaults to new request");

			req.Url = "http://test.com/foo";
			Assert.AreSame(cli1, req.Client, "new URL with same host should hold onto same client");
			Assert.IsFalse(req.Settings.Redirects.Enabled);

			req.Url = "http://test2.com";
			Assert.AreNotSame(cli1, req.Client, "new host should trigger new client");
			Assert.IsTrue(req.Settings.Redirects.Enabled);

			FluentRestHttp.ConfigureClient("http://test2.com", cli => cli.Settings.Redirects.Enabled = false);
			Assert.IsFalse(req.Settings.Redirects.Enabled, "changing client settings should be reflected in request");

			req.Settings = new FluentRestHttpSettings();
			Assert.IsFalse(req.Settings.Redirects.Enabled, "entirely new settings object should still inherit current client settings");

			req.Client = new FluentRestClient();
			Assert.IsTrue(req.Settings.Redirects.Enabled, "entirely new client should provide new defaults");

			req.Url = "http://test.com";
			Assert.AreNotSame(cli1, req.Client, "client was explicitly set on request, so it shouldn't change even if the URL changes");
			Assert.IsTrue(req.Settings.Redirects.Enabled);
		}

		[TestMethod]
		public void request_gets_global_settings_when_no_client() {
			var req = new FluentRestRequest();
			Assert.IsNull(req.Client);
			Assert.IsNull(req.Url);
			Assert.AreEqual(FluentRestHttp.GlobalSettings.JsonSerializer, req.Settings.JsonSerializer);
		}
	}

	public class SomeCustomHttpClientFactory : IHttpClientFactory
	{
		public HttpClient CreateHttpClient(HttpMessageHandler handler) => new SomeCustomHttpClient();
		public HttpMessageHandler CreateMessageHandler() => new SomeCustomMessageHandler();
	}

	public class SomeCustomHttpClient : HttpClient { }
	public class SomeCustomMessageHandler : HttpClientHandler { }
}
