using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Http;
using FluentRest.Http.Configuration;
using FluentRest.Http.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Urls;

namespace FluentRest.Test.Http
{
	/// <summary>
	/// Most HTTP tests in this project are with FluentRest in fake mode. These are some real ones, mostly using http://httpbin.org.
	/// </summary>
	[TestClass]
	public class RealHttpTests
	{
		[DataRow("gzip", "gzipped")]
		[DataRow("deflate", "deflated"), Ignore("#474")]
		public async Task decompresses_automatically(string encoding, string jsonKey) {
			var result = await "https://httpbin.org"
				.AppendPathSegment(encoding)
				.WithHeader("Accept-encoding", encoding)
				.GetJsonAsync<Dictionary<string, object>>();

			Assert.AreEqual(true, result[jsonKey]);
		}

		[DataRow("https://httpbin.org/image/jpeg", null, "my-image.jpg", "my-image.jpg")]
		// should use last path segment url-decoded (foo?bar:ding), then replace illegal path characters with _
		[DataRow("https://httpbin.org/anything/foo%3Fbar%3Ading", null, null, "foo_bar_ding")]
		// should use filename from content-disposition excluding any leading/trailing quotes
		[DataRow("https://httpbin.org/response-headers", "attachment; filename=\"myfile.txt\"", null, "myfile.txt")]
		// should prefer filename* over filename, per https://tools.ietf.org/html/rfc6266#section-4.3
		[DataRow("https://httpbin.org/response-headers", "attachment; filename=filename.txt; filename*=utf-8''filenamestar.txt", null, "filenamestar.txt")]
		// has Content-Disposition header but no filename in it, should use last part of URL
		[DataRow("https://httpbin.org/response-headers", "attachment", null, "response-headers")]
		public async Task can_download_file(string url, string contentDisposition, string suppliedFilename, string expectedFilename) {
			var folder = Path.Combine(Path.GetTempPath(), $"FluentRest-test-{Guid.NewGuid()}"); // random so parallel tests don't trip over each other

			try {
				var path = await url.SetQueryParam("Content-Disposition", contentDisposition).DownloadFileAsync(folder, suppliedFilename);
				var expected = Path.Combine(folder, expectedFilename);
				Assert.AreEqual(expected, path);
				Assert.IsTrue(File.Exists(expected));
			}
			finally {
				Directory.Delete(folder, true);
			}
		}

		[TestMethod]
		public async Task can_post_and_receive_json() {
			var result = await "https://httpbin.org/post".PostJsonAsync(new { a = 1, b = 2 }).ReceiveJson<JsonElement>();
			var json = result.GetProperty("json");
			
			Assert.AreEqual(json.GetProperty("a").GetInt32(), 1);
			Assert.AreEqual(json.GetProperty("b").GetInt32(), 2);
		}

		[TestMethod]
		public async Task can_get_stream() {
			using (var stream = await "https://www.google.com".GetStreamAsync())
			using (var ms = new MemoryStream()) {
				stream.CopyTo(ms);
				Assert.IsTrue(ms.Length > 0);
			}
		}

		[TestMethod]
		public async Task can_get_string() {
			var s = await "https://www.google.com".GetStringAsync();
			Assert.IsTrue(s.Length > 0);
		}

		[TestMethod]
		public async Task can_get_byte_array() {
			var bytes = await "https://www.google.com".GetBytesAsync();
			Assert.IsTrue(bytes.Length > 0);
		}

		[TestMethod]
		public void fails_on_non_success_status() {
			Assert.ThrowsExceptionAsync<FluentRestHttpException>(async () => await "https://httpbin.org/status/418".GetAsync());
		}

		[TestMethod]
		public async Task can_allow_non_success_status() {
			var resp = await "https://httpbin.org/status/418".AllowHttpStatus("4xx").GetAsync();
			Assert.AreEqual(418, resp.StatusCode);
		}

		[TestMethod]
		public async Task can_post_multipart() 
		{
			var folder = "c:\\FluentRest-test-" + Guid.NewGuid(); // random so parallel tests don't trip over each other
			var path1 = Path.Combine(folder, "upload1.txt");
			var path2 = Path.Combine(folder, "upload2.txt");

			Directory.CreateDirectory(folder);
			try 
			{
				File.WriteAllText(path1, "file contents 1");
				File.WriteAllText(path2, "file contents 2");

				await using var stream = File.OpenRead(path2);
				JsonElement resp = await "https://httpbin.org/post"
					.PostMultipartAsync(content => {
						content
							.AddStringParts(new { a = 1, b = 2 })
							.AddString("DataField", "hello!")
							.AddFile("File1", path1)
							.AddFile("File2", stream, "foo.txt");

						// hack to deal with #179. appears to be fixed on httpbin now.
						// content.Headers.ContentLength = 735;
					})
					//.ReceiveString();
					.ReceiveJson<JsonElement>();

				var form = resp.GetProperty("form");
				Assert.AreEqual("1", form.GetProperty("a").GetString());
				Assert.AreEqual("2", form.GetProperty("b").GetString());
				Assert.AreEqual("hello!", form.GetProperty("DataField").GetString());
				var files = resp.GetProperty("files");
				Assert.AreEqual("file contents 1", files.GetProperty("File1").GetString());
				Assert.AreEqual("file contents 2", files.GetProperty("File2").GetString());
			}
			finally {
				Directory.Delete(folder, true);
			}
		}

		[TestMethod]
		public async Task can_handle_http_error() {
			var handlerCalled = false;

			try {
				await "https://httpbin.org/status/500".ConfigureRequest(c => {
					c.OnError = call => {
						call.ExceptionHandled = true;
						handlerCalled = true;
					};
				}).GetJsonAsync<JsonElement>();
				Assert.IsTrue(handlerCalled, "error handler should have been called.");
			}
			catch (FluentRestHttpException) {
				Assert.Fail("exception should have been suppressed.");
			}
		}

		[TestMethod]
		public async Task can_handle_parsing_error() {
			Exception ex = null;

			try {
				await "http://httpbin.org/image/jpeg".ConfigureRequest(c => {
					c.OnError = call => {
						ex = call.Exception;
						call.ExceptionHandled = true;
					};
				}).GetJsonAsync<ExpandoObject>();
				Assert.IsNotNull(ex, "error handler should have been called.");
				Assert.IsInstanceOfType(ex, typeof(FluentRestParsingException));
			}
			catch (FluentRestHttpException) {
				Assert.Fail("exception should have been suppressed.");
			}
		}

		[TestMethod]
		public async Task can_comingle_real_and_fake_tests() {
			// do a fake call while a real call is running
			var realTask = "https://www.google.com/".GetStringAsync();
			using (var test = new HttpTest()) {
				test.RespondWith("fake!");
				var fake = await "https://www.google.com/".GetStringAsync();
				Assert.AreEqual("fake!", fake);
			}
			Assert.AreNotEqual("fake!", await realTask);
		}

		[TestMethod]
		public void can_set_timeout() {
			var ex = Assert.ThrowsExceptionAsync<FluentRestHttpTimeoutException>(async () => {
				await "https://httpbin.org/delay/5"
					.WithTimeout(TimeSpan.FromMilliseconds(50))
					.HeadAsync();
			});
			Assert.IsTrue(ex.Result.InnerException is TaskCanceledException);
		}

		[TestMethod]
		public async Task can_cancel_request() {
			var cts = new CancellationTokenSource();
			var ex = await Assert.ThrowsExceptionAsync<FluentRestHttpException>(async () => {
				var task = "https://httpbin.org/delay/5".GetAsync(cts.Token);
				cts.Cancel();
				await task;
			});
			Assert.IsTrue(ex.InnerException is TaskCanceledException);
		}

		[TestMethod] // make sure the 2 tokens in play are playing nicely together
		public async Task  can_set_timeout_and_cancellation_token() {
			// cancellation with timeout value set
			var cts = new CancellationTokenSource();
			var ex = await Assert.ThrowsExceptionAsync<FluentRestHttpException>(async () => {
				var task = "https://httpbin.org/delay/5"
					.WithTimeout(TimeSpan.FromMilliseconds(50))
					.GetAsync(cts.Token);
				cts.Cancel();
				await task;
			});
			Assert.IsTrue(ex.InnerException is OperationCanceledException);
			Assert.IsTrue(cts.Token.IsCancellationRequested);

			// timeout with cancellation token set
			cts = new CancellationTokenSource();
			ex = await Assert.ThrowsExceptionAsync<FluentRestHttpTimeoutException>(async () => {
				await "https://httpbin.org/delay/5"
					.WithTimeout(TimeSpan.FromMilliseconds(50))
					.GetAsync(cts.Token);
			});
			Assert.IsTrue(ex.InnerException is OperationCanceledException);
			Assert.IsFalse(cts.Token.IsCancellationRequested);
		}

		[TestMethod]
		public async Task connection_lease_timeout_doesnt_disrupt_calls() {
			// testing this quickly is tricky. HttpClient will be replaced by a new instance after 1 timeout and disposed
			// after another, so the timeout period (typically minutes in real-world scenarios) needs to be long enough
			// that we don't dispose before the response from google is received. 1 second seems to work.
			var cli = new FluentRestClient("http://www.google.com");
			cli.Settings.ConnectionLeaseTimeout = TimeSpan.FromMilliseconds(1000);

			// ping google for about 2.5 seconds
			var tasks = new List<Task>();
			for (var i = 0; i < 100; i++) {
				tasks.Add(cli.Request().HeadAsync());
				await Task.Delay(25);
			}
			await Task.WhenAll(tasks); // failed HTTP status, etc, would throw here and fail the test.
		}

		[TestMethod]
		public async Task test_settings_override_client_settings() {
			var cli1 = new FluentRestClient();
			cli1.Settings.HttpClientFactory = new DefaultHttpClientFactory();
			var h = cli1.HttpClient; // force (lazy) instantiation

			using (var test = new HttpTest()) {
				test.Settings.Redirects.Enabled = false;

				test.RespondWith("foo!");
				var s = await cli1.Request("http://www.google.com")
					.WithAutoRedirect(true) // test says redirects are off, and test should always win
					.GetStringAsync();
				Assert.AreEqual("foo!", s);
				Assert.IsFalse(cli1.Settings.Redirects.Enabled);

				var cli2 = new FluentRestClient();
				cli2.Settings.HttpClientFactory = new DefaultHttpClientFactory();
				h = cli2.HttpClient;

				test.RespondWith("foo 2!");
				s = await cli2.Request("http://www.google.com")
					.WithAutoRedirect(true) // test says redirects are off, and test should always win
					.GetStringAsync();
				Assert.AreEqual("foo 2!", s);
				Assert.IsFalse(cli2.Settings.Redirects.Enabled);
			}
		}

		[TestMethod]
		public async Task can_allow_real_http_in_test() {
			using (var test = new HttpTest()) {
				test.RespondWith("foo");
				test.ForCallsTo("*httpbin*").AllowRealHttp();

				Assert.AreEqual("foo", await "https://www.google.com".GetStringAsync());
				Assert.AreNotEqual("foo", await "https://httpbin.org/get".GetStringAsync());
				JsonElement result = await "https://httpbin.org/get?x=bar".GetJsonAsync<JsonElement>();
				Assert.AreEqual("bar", result.GetProperty("args").GetProperty("x").GetString());
				Assert.AreEqual("foo", await "https://www.microsoft.com".GetStringAsync());

				// real calls still get logged
				Assert.AreEqual(4, test.CallLog.Count);
				test.ShouldHaveCalled("https://httpbin*").Times(2);
			}
		}

		[TestMethod]
		public async Task does_not_create_empty_content_for_forwarding_content_header() {
			// FluentRest was auto-creating an empty HttpContent object in order to forward content-level headers,
			// and on .NET Framework a GET with a non-null HttpContent throws an exceptions (#583)
			var calls = new List<FluentRestDetail>();
			var resp = await "http://httpbin.org/redirect-to?url=http%3A%2F%2Fexample.com%2F".ConfigureRequest(c => {
				c.Redirects.ForwardHeaders = true;
				c.BeforeCall = call => calls.Add(call);
			}).PostUrlEncodedAsync("test=test");

			Assert.AreEqual(2, calls.Count);
			Assert.AreEqual(HttpMethod.Post, calls[0].Request.Verb);
			Assert.IsNotNull(calls[0].HttpRequestMessage.Content);
			Assert.AreEqual(HttpMethod.Get, calls[1].Request.Verb);
			Assert.IsNull(calls[1].HttpRequestMessage.Content);
		}

		#region cookies
		[TestMethod]
		public async Task can_send_cookies() {
			var req = "https://httpbin.org/cookies".WithCookies(new { x = 1, y = 2 });
			Assert.AreEqual(2, req.Cookies.Count());
			Assert.IsTrue(req.Cookies.Contains(("x", "1")));
			Assert.IsTrue(req.Cookies.Contains(("y", "2")));

			var s = await req.GetStringAsync();

			JsonElement resp = await req.WithAutoRedirect(false).GetJsonAsync<JsonElement>();
			var cookies = resp.GetProperty("cookies");
			// httpbin returns json representation of cookies that were sent
			Assert.AreEqual("1", cookies.GetProperty("x").GetString());
			Assert.AreEqual("2", cookies.GetProperty("y").GetString());
		}

		[TestMethod]
		public async Task can_receive_cookies() {
			// endpoint does a redirect, so we need to disable auto-redirect in order to see the cookie in the response
			var resp = await "https://httpbin.org/cookies/set?z=999".WithAutoRedirect(false).GetAsync();
			Assert.AreEqual("999", resp.Cookies.FirstOrDefault(c => c.Name == "z")?.Value);


			// but using WithCookies we can capture it even with redirects enabled
			await "https://httpbin.org/cookies/set?z=999".WithCookies(out var cookies).GetAsync();
			Assert.AreEqual("999", cookies.FirstOrDefault(c => c.Name == "z")?.Value);

			// this works with redirects too
			using (var session = new CookieSession("https://httpbin.org/cookies")) {
				await session.Request("set?z=999").GetAsync();
				Assert.AreEqual("999", session.Cookies.FirstOrDefault(c => c.Name == "z")?.Value);
			}
		}

		[TestMethod]
		public async Task can_set_cookies_before_setting_url() {
			var req = new FluentRestRequest().WithCookie("z", "999");
			req.Url = "https://httpbin.org/cookies";
			JsonElement resp = await req.GetJsonAsync<JsonElement>();
			Assert.AreEqual("999", resp.GetProperty("cookies").GetProperty("z").GetString());
		}

		[TestMethod]
		public async Task can_send_different_cookies_per_request() {
			var cli = new FluentRestClient();

			var req1 = cli.Request("https://httpbin.org/cookies").WithCookie("x", "123");
			var req2 = cli.Request("https://httpbin.org/cookies").WithCookie("x", "abc");

			JsonElement resp2 = await req2.GetJsonAsync<JsonElement>();
			JsonElement resp1 = await req1.GetJsonAsync<JsonElement>();

			Assert.AreEqual("123", resp1.GetProperty("cookies").GetProperty("x").GetString());
			Assert.AreEqual("abc", resp2.GetProperty("cookies").GetProperty("x").GetString());
		}

		[TestMethod]
		public async Task can_receive_cookie_from_redirect_response_and_add_it_to_jar() {
			var resp = await "https://httpbin.org/redirect-to".SetQueryParam("url", "https://httpbin.org/cookies/set?x=foo").WithCookies(out var jar).GetJsonAsync<JsonElement>();

			Assert.AreEqual("foo", resp.GetProperty("cookies").GetProperty("x").GetString());
			Assert.AreEqual(1, jar.Count);
		}
		#endregion
	}
}