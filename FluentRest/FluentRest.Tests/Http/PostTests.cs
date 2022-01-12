﻿using System.Net.Http;
using System.Threading.Tasks;
using FluentRest.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Urls;

namespace FluentRest.Test.Http
{
	[TestClass]
	public class PostTests : HttpMethodTests
	{
		public PostTests() : base(HttpMethod.Post) { }

		protected override Task<IFluentRestResponse> CallOnString(string url) => url.PostAsync(null);
		protected override Task<IFluentRestResponse> CallOnUrl(Url url) => url.PostAsync(null);
		protected override Task<IFluentRestResponse> CallOnFluentRestRequest(IFluentRestRequest req) => req.PostAsync(null);

		[TestMethod]
		public async Task can_post_string() {
			var expectedEndpoint = "http://some-api.com";
			var expectedBody = "abc123";
			await expectedEndpoint.PostStringAsync(expectedBody);
			HttpTest.ShouldHaveCalled(expectedEndpoint)
				.WithVerb(HttpMethod.Post)
				.WithRequestBody(expectedBody)
				.Times(1);
		}

		[TestMethod]
		public async Task can_post_object_as_json() {
			var body = new {a = 1, b = 2};
			await "http://some-api.com".PostJsonAsync(body);
			HttpTest.ShouldHaveCalled("http://some-api.com")
				.WithVerb(HttpMethod.Post)
				.WithContentType("application/json")
				.WithRequestBody("{\"a\":1,\"b\":2}")
				.Times(1);
		}

		[TestMethod]
		public async Task can_post_url_encoded() {
			var body = new { a = 1, b = 2, c = "hi there", d = new[] { 1, 2, 3 } };
			await "http://some-api.com".PostUrlEncodedAsync(body);
			HttpTest.ShouldHaveCalled("http://some-api.com")
				.WithVerb(HttpMethod.Post)
				.WithContentType("application/x-www-form-urlencoded")
				.WithRequestBody("a=1&b=2&c=hi+there&d=1&d=2&d=3")
				.Times(1);
		}

		[TestMethod]
		public async Task can_post_nothing() {
			await "http://some-api.com".PostAsync();
			HttpTest.ShouldHaveCalled("http://some-api.com")
				.WithVerb(HttpMethod.Post)
				.WithRequestBody("")
				.Times(1);
		}

		[TestMethod]
		public async Task can_receive_json() {
			HttpTest.RespondWithJson(new TestData { id = 1, name = "Frank" });

			var data = await "http://some-api.com".PostJsonAsync(new { a = 1, b = 2 }).ReceiveJson<TestData>();

			Assert.AreEqual(1, data.id);
			Assert.AreEqual("Frank", data.name);
		}

		// [TestMethod]
		// public async Task can_receive_json_dynamic() {
		// 	HttpTest.RespondWithJson(new { id = 1, name = "Frank" });
		//
		// 	var data = await "http://some-api.com".PostJsonAsync(new { a = 1, b = 2 }).ReceiveJson();
		//
		// 	Assert.AreEqual(1, data.id);
		// 	Assert.AreEqual("Frank", data.name);				
		// }
		//
		// [TestMethod]
		// public async Task can_receive_json_dynamic_list() {
		// 	HttpTest.RespondWithJson(new[] {
		// 		new { id = 1, name = "Frank" },
		// 		new { id = 2, name = "Claire" }
		// 	});
		//
		// 	var data = await "http://some-api.com".PostJsonAsync(new { a = 1, b = 2 }).ReceiveJsonList();
		//
		// 	Assert.AreEqual(1, data[0].id);
		// 	Assert.AreEqual("Frank", data[0].name);
		// 	Assert.AreEqual(2, data[1].id);
		// 	Assert.AreEqual("Claire", data[1].name);
		// }

		[TestMethod]
		public async Task can_receive_string() {
			HttpTest.RespondWith("good job");

			var data = await "http://some-api.com".PostJsonAsync(new { a = 1, b = 2 }).ReceiveString();

			Assert.AreEqual("good job", data);
		}

		private class TestData
		{
			public int id { get; set; }
			public string name { get; set; }
		}
	}
}
