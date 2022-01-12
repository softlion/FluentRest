using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Http.Configuration;

namespace FluentRest.Test.Http
{
    [TestClass]
    public class DefaultUrlEncodedSerializerTests
    {
	    [TestMethod]
	    public void can_serialize_object() {
		    var vals = new {
			    a = "foo",
			    b = 333,
			    c = (string)null, // exclude
			    d = ""
		    };

		    var serialized = new DefaultUrlEncodedSerializer().Serialize(vals);
		    Assert.AreEqual("a=foo&b=333&d=", serialized);
	    }
	}
}
