using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Urls;

namespace FluentRest.Test.UrlBuilder
{
	[TestClass]
	public class UrlParsingTests
	{
		[TestMethod]
		// relative
		[DataRow("//relative/with/authority", "", "relative", "", "relative", null, "/with/authority", "", "")]
		[DataRow("/relative/without/authority", "", "", "", "", null, "/relative/without/authority", "", "")]
		[DataRow("relative/without/path/anchor", "", "", "", "", null, "relative/without/path/anchor", "", "")]
		// absolute
		[DataRow("http://www.mysite.com/with/path?x=1", "http", "www.mysite.com", "", "www.mysite.com", null, "/with/path", "x=1", "")]
		[DataRow("https://www.mysite.com/with/path?x=1#foo", "https", "www.mysite.com", "", "www.mysite.com", null, "/with/path", "x=1", "foo")]
		[DataRow("http://user:pass@www.mysite.com:8080/with/path?x=1?y=2", "http", "user:pass@www.mysite.com:8080", "user:pass", "www.mysite.com", 8080, "/with/path", "x=1?y=2", "")]
		[DataRow("http://www.mysite.com/#with/path?x=1?y=2", "http", "www.mysite.com", "", "www.mysite.com", null, "/", "", "with/path?x=1?y=2")]
		// from https://en.wikipedia.org/wiki/Uniform_Resource_Identifier#Examples
		[DataRow("https://john.doe@www.example.com:123/forum/questions/?tag=networking&order=newest#top", "https", "john.doe@www.example.com:123", "john.doe", "www.example.com", 123, "/forum/questions/", "tag=networking&order=newest", "top")]
		[DataRow("ldap://[2001:db8::7]/c=GB?objectClass?one", "ldap", "[2001:db8::7]", "", "[2001:db8::7]", null, "/c=GB", "objectClass?one", "")]
		[DataRow("mailto:John.Doe@example.com", "mailto", "", "", "", null, "John.Doe@example.com", "", "")]
		[DataRow("news:comp.infosystems.www.servers.unix", "news", "", "", "", null, "comp.infosystems.www.servers.unix", "", "")]
		[DataRow("tel:+1-816-555-1212", "tel", "", "", "", null, "+1-816-555-1212", "", "")]
		[DataRow("telnet://192.0.2.16:80/", "telnet", "192.0.2.16:80", "", "192.0.2.16", 80, "/", "", "")]
		[DataRow("urn:oasis:names:specification:docbook:dtd:xml:4.1.2", "urn", "", "", "", null, "oasis:names:specification:docbook:dtd:xml:4.1.2", "", "")]
		// with uppercase letters
		[DataRow("http://www.mySite.com:8080/With/Path?x=1?Y=2", "http", "www.mysite.com:8080", "", "www.mysite.com", 8080, "/With/Path", "x=1?Y=2", "")]
		[DataRow("HTTP://www.mysite.com:8080", "http", "www.mysite.com:8080", "", "www.mysite.com", 8080, "", "", "")]
		public void can_parse_url_parts(string url, string scheme, string authority, string userInfo, string host, int? port, string path, string query, string fragment) {
			// there's a few ways to get Url object so let's check all of them
			foreach (var parsed in new[] { new Url(url), Url.Parse(url), new Url(new Uri(url, UriKind.RelativeOrAbsolute)) }) {
				Assert.AreEqual(scheme, parsed.Scheme);
				Assert.AreEqual(authority, parsed.Authority);
				Assert.AreEqual(userInfo, parsed.UserInfo);
				Assert.AreEqual(host, parsed.Host);
				Assert.AreEqual(port, parsed.Port);
				Assert.AreEqual(path, parsed.Path);
				Assert.AreEqual(query, parsed.Query);
				Assert.AreEqual(fragment, parsed.Fragment);
			}
		}

		[DataRow("http://www.trailing-slash.com/", "/")]
		[DataRow("http://www.trailing-slash.com/a/b/", "/a/b/")]
		[DataRow("http://www.trailing-slash.com/a/b/?x=y", "/a/b/")]
		[DataRow("http://www.no-trailing-slash.com", "")]
		[DataRow("http://www.no-trailing-slash.com/a/b", "/a/b")]
		[DataRow("http://www.no-trailing-slash.com/a/b?x=y", "/a/b")]
		public void path_retains_trailing_slash(string url, string path) {
			Assert.AreEqual(path, new Url(url).Path);
		}

		[TestMethod]
		public void can_parse_query_params() {
			var q = new Url("http://www.mysite.com/more?x=1&y=2&z=3&y=4&abc&xyz&foo=&=bar&y=6").QueryParams;

			CollectionAssert.AreEqual(new (string, object)[] {
				("x", "1"),
				("y", "2"),
				("z", "3"),
				("y", "4"),
				("abc", null),
				("xyz", null),
				("foo", ""),
				("", "bar"),
				("y", "6")
			}, q.ToArray());

			Assert.AreEqual(("y", "4"), q[3]);
			Assert.AreEqual("foo", q[6].Name);
			Assert.AreEqual("bar", q[7].Value);

			Assert.AreEqual("1", q.FirstOrDefault("x"));
			CollectionAssert.AreEqual(new[] { "2", "4", "6" }, q.GetAll("y").ToList()); // group values of same name into array
			Assert.AreEqual("3", q.FirstOrDefault("z"));
			Assert.AreEqual(null, q.FirstOrDefault("abc"));
			Assert.AreEqual(null, q.FirstOrDefault("xyz"));
			Assert.AreEqual("", q.FirstOrDefault("foo"));
			Assert.AreEqual("bar", q.FirstOrDefault(""));
		}

		[DataRow("http://www.mysite.com/more?x=1&y=2", true)]
		[DataRow("//how/about/this#hi", false)]
		[DataRow("/how/about/this#hi", false)]
		[DataRow("how/about/this#hi", false)]
		[DataRow("", false)]
		public void Url_converts_to_uri(string s, bool isAbsolute) {
			var url = new Url(s);
			var uri = url.ToUri();
			Assert.AreEqual(s, uri.OriginalString);
			Assert.AreEqual(isAbsolute, uri.IsAbsoluteUri);
		}

		[TestMethod]
		public void interprets_plus_as_space() {
			var url = new Url("http://www.mysite.com/foo+bar?x=1+2");
			Assert.AreEqual("1 2", url.QueryParams.FirstOrDefault("x"));
		}

		[TestMethod] // #437
		public void interprets_encoded_plus_as_plus() {
			var urlStr = "http://google.com/search?q=param_with_%2B";
			var url = new Url(urlStr);
			var paramValue = url.QueryParams.FirstOrDefault("q");
			Assert.AreEqual("param_with_+", paramValue);
		}

		[TestMethod] // #56
		public void does_not_alter_url_passed_to_constructor() {
			var expected = "http://www.mysite.com/hi%20there/more?x=%CD%EE%E2%FB%E9%20%E3%EE%E4";
			var url = new Url(expected);
			Assert.AreEqual(expected, url.ToString());
		}

		[TestMethod] // #656
		public void queryparams_uses_equals() {
			var url = new Url("http://www.mysite.com?param=1");
			// String gets boxed, so we need to use Equals, instead of ==
			var contains = url.QueryParams.Contains("param", "1");
			Assert.IsTrue(contains);
		}
	}
}
