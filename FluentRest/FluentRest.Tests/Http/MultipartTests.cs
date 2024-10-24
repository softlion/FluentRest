﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentRest.Http;
using FluentRest.Http.Content;
using FluentRest.Http.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluentRest.Test.Http
{
	[TestClass]
    public class MultipartTests
    {
	    [TestMethod]
		public async Task can_build_and_send_multipart_content() {
			var content = new CapturedMultipartContent()
				.AddString("string", "foo")
				.AddString("string2", "bar", "text/blah")
				.AddStringParts(new { part1 = 1, part2 = 2, part3 = (string)null }) // part3 should be excluded
				.AddFile("file1", Path.Combine("path", "to", "image1.jpg"), "image/jpeg")
				.AddFile("file2", Path.Combine("path", "to", "image2.jpg"), "image/jpeg", fileName: "new-name.jpg")
				.AddJson("json", new { foo = "bar" })
				.AddUrlEncoded("urlEnc", new { fizz = "buzz" });

			void AssertAll() {
				Assert.AreEqual(8, content.Parts.Count);
				AssertStringPart<CapturedStringContent>(content.Parts[0], "string", "foo", null);
				AssertStringPart<CapturedStringContent>(content.Parts[1], "string2", "bar", "text/blah");
				AssertStringPart<CapturedStringContent>(content.Parts[2], "part1", "1", null);
				AssertStringPart<CapturedStringContent>(content.Parts[3], "part2", "2", null);
				AssertFilePart(content.Parts[4], "file1", "image1.jpg", "image/jpeg");
				AssertFilePart(content.Parts[5], "file2", "new-name.jpg", "image/jpeg");
				AssertStringPart<CapturedJsonContent>(content.Parts[6], "json", "{\"foo\":\"bar\"}", "application/json; charset=UTF-8");
				AssertStringPart<CapturedUrlEncodedContent>(content.Parts[7], "urlEnc", "fizz=buzz", "application/x-www-form-urlencoded");
			}

			// Assert before and after sending a request. MultipartContent clears the parts collection after request is sent;
			// CapturedMultipartContent (as the name implies) should preserve it (#580)

			AssertAll();
			using (var test = new HttpTest()) {
				await "https://upload.com".PostAsync(content);
			}
			AssertAll();
		}

		private void AssertStringPart<TContent>(HttpContent part, string name, string content, string contentType) {
			Assert.IsInstanceOfType(part, typeof(TContent));
		    Assert.AreEqual(name, part.Headers.ContentDisposition.Name);
		    Assert.AreEqual(content, (part as CapturedStringContent)?.Content);
		    if (contentType == null)
			    Assert.IsFalse(part.Headers.Contains("Content-Type")); // #392
		    else
			    Assert.AreEqual(contentType, part.Headers.GetValues("Content-Type").SingleOrDefault());
		}

	    private void AssertFilePart(HttpContent part, string name, string fileName, string contentType) {
		    Assert.IsInstanceOfType(part, typeof(FileContent));
			Assert.AreEqual(name, part.Headers.ContentDisposition.Name);
			Assert.AreEqual(fileName, part.Headers.ContentDisposition.FileName);
			Assert.AreEqual(contentType, part.Headers.ContentType?.MediaType);
	    }

		[TestMethod]
		public void must_provide_required_args_to_builder() {
			var content = new CapturedMultipartContent();
			Assert.ThrowsException<ArgumentNullException>(() => content.AddStringParts(null));
			Assert.ThrowsException<ArgumentNullException>(() => content.AddString("other", null));
			Assert.ThrowsException<ArgumentException>(() => content.AddString(null, "hello!"));
			Assert.ThrowsException<ArgumentException>(() => content.AddFile("  ", "path"));
		}
	}
}
