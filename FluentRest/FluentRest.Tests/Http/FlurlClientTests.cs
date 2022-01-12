using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Http;
using FluentRest.Urls;

namespace FluentRest.Test.Http
{
	[TestClass]
	public class FluentRestClientTests
	{
		[TestMethod]
		// check that for every FluentRestClient extension method, we have an equivalent Url and string extension
		public void extension_methods_consistently_supported() {
			var reqExts = ReflectionHelper.GetAllExtensionMethods<IFluentRestRequest>(typeof(FluentRestClient).GetTypeInfo().Assembly)
				// URL builder methods on IFluentRestClient get a free pass. We're looking for things like HTTP calling methods.
				.Where(mi => mi.DeclaringType != typeof(UrlBuilderExtensions))
				.ToList();
			var urlExts = ReflectionHelper.GetAllExtensionMethods<Url>(typeof(FluentRestClient).GetTypeInfo().Assembly).ToList();
			var stringExts = ReflectionHelper.GetAllExtensionMethods<string>(typeof(FluentRestClient).GetTypeInfo().Assembly).ToList();
			var uriExts = ReflectionHelper.GetAllExtensionMethods<Uri>(typeof(FluentRestClient).GetTypeInfo().Assembly).ToList();

			Assert.IsTrue(reqExts.Count > 20, $"IFluentRestRequest only has {reqExts.Count} extension methods? Something's wrong here.");

			// Url and string should contain all extension methods that IFluentRestRequest has
			foreach (var method in reqExts) {
				if (!urlExts.Any(m => ReflectionHelper.AreSameMethodSignatures(method, m))) {
					var args = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
					Assert.Fail($"No equivalent Url extension method found for IFluentRestRequest.{method.Name}({args})");
				}
				if (!stringExts.Any(m => ReflectionHelper.AreSameMethodSignatures(method, m))) {
					var args = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
					Assert.Fail($"No equivalent string extension method found for IFluentRestRequest.{method.Name}({args})");
				}
				if (!uriExts.Any(m => ReflectionHelper.AreSameMethodSignatures(method, m))) {
					var args = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
					Assert.Fail($"No equivalent System.Uri extension method found for IFluentRestRequest.{method.Name}({args})");
				}
			}
		}

		[TestMethod]
		public void can_create_request_without_base_url() {
			var cli = new FluentRestClient();
			var req = cli.Request("http://myapi.com/foo?x=1&y=2#foo");
			Assert.AreEqual("http://myapi.com/foo?x=1&y=2#foo", req.Url.ToString());
		}

		[TestMethod]
		public void can_create_request_with_base_url() {
			var cli = new FluentRestClient("http://myapi.com");
			var req = cli.Request("foo", "bar");
			Assert.AreEqual("http://myapi.com/foo/bar", req.Url.ToString());
		}

		[TestMethod]
		public void request_with_full_url_overrides_base_url() {
			var cli = new FluentRestClient("http://myapi.com");
			var req = cli.Request("http://otherapi.com", "foo");
			Assert.AreEqual("http://otherapi.com/foo", req.Url.ToString());
		}

		[TestMethod]
		public void can_create_request_with_base_url_and_no_segments() {
			var cli = new FluentRestClient("http://myapi.com");
			var req = cli.Request();
			Assert.AreEqual("http://myapi.com", req.Url.ToString());
		}

		[TestMethod]
		public void cannot_create_request_without_base_url_or_segments() {
			var cli = new FluentRestClient();
			Assert.ThrowsException<ArgumentException>(() => {
				var req = cli.Request();
			});
		}

		[TestMethod]
		public void cannot_create_request_without_base_url_or_segments_comprising_full_url() {
			var cli = new FluentRestClient();
			Assert.ThrowsException<ArgumentException>(() => {
				var req = cli.Request("foo", "bar");
			});
		}

		[TestMethod]
		public void default_factory_doesnt_reuse_disposed_clients() {
			var cli1 = "http://api.com".WithHeader("foo", "1").Client;
			var cli2 = "http://api.com".WithHeader("foo", "2").Client;
			cli1.Dispose();
			var cli3 = "http://api.com".WithHeader("foo", "3").Client;

			Assert.AreEqual(cli1, cli2);
			Assert.IsTrue(cli1.IsDisposed);
			Assert.IsTrue(cli2.IsDisposed);
			Assert.AreNotEqual(cli1, cli3);
			Assert.IsFalse(cli3.IsDisposed);
		}

		[TestMethod]
		public void can_create_FluentRestClient_with_existing_HttpClient() {
			var hc = new HttpClient {
				BaseAddress = new Uri("http://api.com/"),
				Timeout = TimeSpan.FromSeconds(123)
			};
			var cli = new FluentRestClient(hc);

			Assert.AreEqual("http://api.com/", cli.HttpClient.BaseAddress.ToString());
			Assert.AreEqual(123, cli.HttpClient.Timeout.TotalSeconds);
			Assert.AreEqual("http://api.com/", cli.BaseUrl);
		}

		[TestMethod] // #334
		public void can_dispose_FluentRestClient_created_with_HttpClient() {
			var hc = new HttpClient();
			var fc = new FluentRestClient(hc);
			fc.Dispose();

			// ensure the HttpClient got disposed
			Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => hc.GetAsync("http://mysite.com"));
		}
	}
}