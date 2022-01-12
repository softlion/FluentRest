﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Urls;

namespace FluentRest.Test.UrlBuilder
{
	[TestClass]
	public class UtilityMethodTests
	{
		[TestMethod]
		public void Combine_works() {
			var url = Url.Combine("http://www.foo.com/", "/too/", "/many/", "/slashes/", "too", "few", "one/two/");
			Assert.AreEqual("http://www.foo.com/too/many/slashes/too/few/one/two/", url);
		}

		[DataRow("segment?", "foo=bar", "x=1&y=2&")]
		[DataRow("segment", "?foo=bar&x=1", "y=2&")]
		[DataRow("segment", "?", "foo=bar&x=1&y=2&")]
		[DataRow("/segment?foo=bar&", "&x=1&", "&y=2&")]
		[DataRow(null, "segment?foo=bar&x=1&y=2&", "")]
		public void Combine_supports_query(string a, string b, string c) {
			var url = Url.Combine("http://root.com", a, b, c);
			Assert.AreEqual("http://root.com/segment?foo=bar&x=1&y=2&", url);
		}

		[TestMethod]
		public void Combine_encodes_illegal_chars() {
			var url = Url.Combine("http://www.foo.com", "hi there", "?", "x=hi there", "#", "hi there");
			Assert.AreEqual("http://www.foo.com/hi%20there?x=hi%20there#hi%20there", url);
		}

		[TestMethod] // #185
		public void can_encode_and_decode_very_long_value() {
			// 65,520 chars is the tipping point for Uri.EscapeDataString https://github.com/dotnet/corefx/issues/1936
			var len = 500000;

			// every 10th char needs to be encoded
			var s = string.Concat(Enumerable.Repeat("xxxxxxxxx ", len / 10));
			Assert.AreEqual(len, s.Length); // just a sanity check

			// encode space as %20
			var encoded = Url.Encode(s, false);
			// hex encoding will add 2 addtional chars for every char that needs to be encoded
			Assert.AreEqual(len + (2 * len / 10), encoded.Length);
			var expected = string.Concat(Enumerable.Repeat("xxxxxxxxx%20", len / 10));
			Assert.AreEqual(expected, encoded);

			var decoded = Url.Decode(encoded, false);
			Assert.AreEqual(s, decoded);

			// encode space as +
			encoded = Url.Encode(s, true);
			Assert.AreEqual(len, encoded.Length);
			expected = string.Concat(Enumerable.Repeat("xxxxxxxxx+", len / 10));
			Assert.AreEqual(expected, encoded);

			// interpret + as space
			decoded = Url.Decode(encoded, true);
			Assert.AreEqual(s, decoded);

			// don't interpret + as space, encoded and decoded should be the same
			decoded = Url.Decode(encoded, false);
			Assert.AreEqual(encoded, decoded);
		}

		[DataRow("http://www.mysite.com/more", true)]
		[DataRow("http://www.mysite.com/more?x=1&y=2", true)]
		[DataRow("http://www.mysite.com/more?x=1&y=2#frag", true)]
		[DataRow("http://www.mysite.com#frag", true)]
		[DataRow("", false)]
		[DataRow("blah", false)]
		[DataRow("http:/www.mysite.com", false)]
		[DataRow("www.mysite.com", false)]
		public void IsValid_works(string s, bool isValid) {
			Assert.AreEqual(isValid, Url.IsValid(s));
		}
	}
}
