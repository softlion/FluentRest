using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Http;

namespace FluentRest.Test.Http
{
	[TestClass]
    class FluentRestHttpExceptionTests : HttpTestFixtureBase
    {
	    [TestMethod]
	    public async Task exception_message_is_nice() {
		    HttpTest.RespondWithJson(new { message = "bad data!" }, 400);

		    try {
			    await "http://myapi.com".PostJsonAsync(new { data = "bad" });
			    Assert.Fail("should have thrown 400.");
		    }
		    catch (FluentRestHttpException ex) {
			    Assert.AreEqual("Call failed with status code 400 (Bad Request): POST http://myapi.com", ex.Message);
		    }
	    }

	    [TestMethod]
	    public async Task exception_message_excludes_request_response_labels_when_body_empty() {
		    HttpTest.RespondWith("", 400);

		    try {
			    await "http://myapi.com".GetAsync();
			    Assert.Fail("should have thrown 400.");
		    }
		    catch (FluentRestHttpException ex) {
				// no "Request body:", "Response body:", or line breaks
			    Assert.AreEqual("Call failed with status code 400 (Bad Request): GET http://myapi.com", ex.Message);
		    }
	    }

	    [TestMethod]
	    public async Task can_catch_parsing_error() {
		    HttpTest.RespondWith("{ \"invalid JSON!");

		    try {
			    await "http://myapi.com".GetJsonAsync<ExpandoObject>();
			    Assert.Fail("should have failed to parse response.");
		    }
		    catch (FluentRestParsingException ex) {
			    Assert.AreEqual("Response could not be deserialized to JSON: GET http://myapi.com", ex.Message);
				// these are equivalent:
			    Assert.AreEqual("{ \"invalid JSON!", await ex.GetResponseStringAsync());
			    Assert.AreEqual("{ \"invalid JSON!", await ex.Call.Response.GetStringAsync());
		    }
		}

	    [TestMethod] // #579
		public void can_create_empty() {
		    var ex = new FluentRestHttpException(null);
		    Assert.AreEqual("Call failed.", ex.Message);
	    }
	}
}
