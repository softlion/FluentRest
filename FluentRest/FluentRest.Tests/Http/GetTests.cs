using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Http;
using FluentRest.Urls;

namespace FluentRest.Test.Http
{
	[TestClass]
	public class GetTests : HttpMethodTests
	{
		public GetTests() : base(HttpMethod.Get) { }

		protected override Task<IFluentRestResponse> CallOnString(string url) => url.GetAsync();
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.GetAsync();
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.GetAsync();

		[TestMethod]
		public async Task can_get_json() {
			HttpTest.RespondWithJson(new TestData { id = 1, name = "Frank" });

			var data = await "http://some-api.com".GetJsonAsync<TestData>();

			Assert.AreEqual(1, data.id);
			Assert.AreEqual("Frank", data.name);
		}

		[TestMethod]
		public async Task can_get_response_then_deserialize() {
			// FluentRestResponse was introduced in 3.0. I don't think we need to go crazy with new tests, because existing
			// methods like FluentRestRequest.GetJson, ReceiveJson, etc all go through FluentRestResponse now.
			HttpTest.RespondWithJson(new TestData { id = 1, name = "Frank" }, 234, new { my_header = "hi" }, null, true);

			var resp = await "http://some-api.com".GetAsync();
			Assert.AreEqual(234, resp.StatusCode);
			Assert.IsTrue(resp.Headers.TryGetFirst("my-header", out var headerVal));
			Assert.AreEqual("hi", headerVal);

			var data = await resp.GetJsonAsync<TestData>();
			Assert.AreEqual(1, data.id);
			Assert.AreEqual("Frank", data.name);
		}

		// [TestMethod]
		// public async Task can_get_json_dynamic() {
		// 	HttpTest.RespondWithJson(new { id = 1, name = "Frank" });
		//
		// 	var data = await "http://some-api.com".GetJsonAsync();
		//
		// 	Assert.AreEqual(1, data.id);
		// 	Assert.AreEqual("Frank", data.name);
		// }
		//
		// [TestMethod]
		// public async Task can_get_json_dynamic_list() {
		// 	HttpTest.RespondWithJson(new[] {
		// 		new { id = 1, name = "Frank" },
		// 		new { id = 2, name = "Claire" }
		// 	});
		//
		// 	var data = await "http://some-api.com".GetJsonListAsync();
		//
		// 	Assert.AreEqual(1, data[0].id);
		// 	Assert.AreEqual("Frank", data[0].name);
		// 	Assert.AreEqual(2, data[1].id);
		// 	Assert.AreEqual("Claire", data[1].name);
		// }

		[TestMethod]
		public async Task can_get_string() {
			HttpTest.RespondWith("good job");

			var data = await "http://some-api.com".GetStringAsync();

			Assert.AreEqual("good job", data);
		}

		[TestMethod]
		public async Task can_get_stream() {
			HttpTest.RespondWith("good job");

			var data = await "http://some-api.com".GetStreamAsync();

			Assert.AreEqual("good job", new StreamReader(data).ReadToEnd());
		}

		[TestMethod]
		public async Task can_get_bytes() {
			HttpTest.RespondWith("good job");

			var data = await "http://some-api.com".GetBytesAsync();

			CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("good job"), data);
		}

		[TestMethod]
		public async Task failure_throws_detailed_exception() {
			HttpTest.RespondWith("bad job", status: 500);

			try {
				await "http://api.com".GetStringAsync();
				Assert.Fail("FluentRestHttpException was not thrown!");
			}
			catch (FluentRestHttpException ex) {
				Assert.AreEqual("http://api.com/", ex.Call.HttpRequestMessage.RequestUri.AbsoluteUri);
				Assert.AreEqual(HttpMethod.Get, ex.Call.HttpRequestMessage.Method);
				Assert.AreEqual(500, ex.Call.Response.StatusCode);
				// these should be equivalent:
				Assert.AreEqual("bad job", await ex.Call.Response.GetStringAsync());
				Assert.AreEqual("bad job", await ex.GetResponseStringAsync());
			}
		}

		[DataRow(false)]
		[DataRow(true)]
		public async Task can_get_error_json_typed(bool useShortcut) {
			HttpTest.RespondWithJson(new { code = 999, message = "our server crashed" }, 500);

			try {
				await "http://api.com".GetStringAsync();
			}
			catch (FluentRestHttpException ex) {
				var error = useShortcut ?
					await ex.GetResponseJsonAsync<TestError>() :
					await ex.Call.Response.GetJsonAsync<TestError>();
				Assert.IsNotNull(error);
				Assert.AreEqual(999, error.code);
				Assert.AreEqual("our server crashed", error.message);
			}
		}

		// [DataRow(false)]
		// [DataRow(true)]
		// public async Task can_get_error_json_untyped(bool useShortcut) {
		// 	HttpTest.RespondWithJson(new { code = 999, message = "our server crashed" }, 500);
		//
		// 	try {
		// 		await "http://api.com".GetStringAsync();
		// 	}
		// 	catch (FluentRestHttpException ex) {
		// 		var error = useShortcut ? // error is a dynamic this time
		// 			await ex.GetResponseJsonAsync() :
		// 			await ex.Call.Response.GetJsonAsync();
		// 		Assert.IsNotNull(error);
		// 		Assert.AreEqual(999, error.code);
		// 		Assert.AreEqual("our server crashed", error.message);
		// 	}
		// }

        [TestMethod]
        public async Task can_get_null_json_when_timeout_and_exception_handled() {
            HttpTest.SimulateTimeout();
            var data = await "http://api.com"
                .ConfigureRequest(c => c.OnError = call => call.ExceptionHandled = true)
                .GetJsonAsync<TestData>();
            Assert.IsNull(data);
        }

		// quotes around charset value is technically legal but there's an issue in .NET we want to avoid: https://github.com/dotnet/corefx/issues/5014
		[TestMethod]
		public async Task can_get_string_with_quoted_charset_header() {
			HttpTest.RespondWith(() => {
				var content = new StringContent("foo");
				content.Headers.Clear();
				content.Headers.Add("Content-Type", "text/javascript; charset=\"UTF-8\"");
				return content;
			});

			var resp = await "http://api.com".GetStringAsync(); // without StripCharsetQuotes, this fails
			Assert.AreEqual("foo", resp);
		}

		[TestMethod] // #313
		public async Task can_setting_content_header_with_no_content() {
			await "http://api.com"
				.WithHeader("Content-Type", "application/json")
				.GetAsync();

			HttpTest.ShouldHaveMadeACall().WithContentType("application/json");
		}

		[TestMethod] // #571
		public async Task can_deserialize_after_callback_reads_string() {
			HttpTest.RespondWithJson(new TestData { id = 123, name = "foo" });
			string logMe = null;
			var result = await new FluentRestRequest("http://api.com")
				.AfterCall( async call => logMe = await call.Response.GetStringAsync())
				.GetJsonAsync<TestData>();

			Assert.IsNotNull(result);
			Assert.AreEqual(123, result.id);
			Assert.AreEqual("foo", result.name);
			Assert.AreEqual("{\"id\":123,\"name\":\"foo\"}", logMe);
		}

		[TestMethod] // #571 (opposite of previous test and probably less common)
		public async Task can_read_string_after_callback_deserializes() {
			HttpTest.RespondWithJson(new TestData { id = 123, name = "foo" });
			TestData logMe = null;
			var result = await new FluentRestRequest("http://api.com")
				.AfterCall(async call => logMe = await call.Response.GetJsonAsync<TestData>())
				.GetStringAsync();

			Assert.AreEqual("{\"id\":123,\"name\":\"foo\"}", result);
			Assert.IsNotNull(logMe);
			Assert.AreEqual(123, logMe.id);
			Assert.AreEqual("foo", logMe.name);
		}

		[TestMethod] // #571
		public async Task can_deserialize_as_different_type_than_callback() {
			HttpTest.RespondWithJson(new TestData2 { id = 123, somethingElse = "bar" });
			TestData logMe = null;
			var result = await new FluentRestRequest("http://api.com")
				.AfterCall(async call => logMe = await call.Response.GetJsonAsync<TestData>())
				.GetJsonAsync<TestData2>();

			Assert.IsNotNull(result);
			Assert.AreEqual(123, result.id);
			// This doesn't work because we deserialized to TestData first, which doesn't have somethingElse, so that value is lost.
			//Assert.AreEqual("bar", result.somethingElse);
			Assert.IsNull(result.somethingElse);

			Assert.IsNotNull(logMe);
			Assert.AreEqual(123, logMe.id);
			Assert.IsNull(logMe.name);
		}

		private class TestData
		{
			public int id { get; set; }
			public string name { get; set; }
		}

		private class TestData2
		{
			public int id { get; set; }
			public string somethingElse { get; set; }
		}

		private class TestError
		{
			public int code { get; set; }
			public string message { get; set; }
		}
	}
}
