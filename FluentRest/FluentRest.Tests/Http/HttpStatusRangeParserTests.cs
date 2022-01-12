using System;
using FluentRest.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluentRest.Test.Http
{
	[TestClass]
    public class HttpStatusRangeParserTests
    {
	    [TestMethod]
		[DataRow("4**", 399, false)]
		[DataRow("4**", 400, true)]
		[DataRow("4**", 499, true)]
		[DataRow("4**", 500, false)]

		[DataRow("4xx", 399, false)]
		[DataRow("4xx", 400, true)]
		[DataRow("4xx", 499, true)]
		[DataRow("4xx", 500, false)]

		[DataRow("4XX", 399, false)]
		[DataRow("4XX", 400, true)]
		[DataRow("4XX", 499, true)]
		[DataRow("4XX", 500, false)]

		[DataRow("400-499", 399, false)]
		[DataRow("400-499", 400, true)]
		[DataRow("400-499", 499, true)]
		[DataRow("400-499", 500, false)]

		[DataRow("100,3xx,600", 100, true)]
		[DataRow("100,3xx,600", 101, false)]
		[DataRow("100,3xx,600", 300, true)]
		[DataRow("100,3xx,600", 399, true)]
		[DataRow("100,3xx,600", 400, false)]
		[DataRow("100,3xx,600", 600, true)]

		[DataRow("400-409,490-499", 399, false)]
		[DataRow("400-409,490-499", 405, true)]
		[DataRow("400-409,490-499", 450, false)]
		[DataRow("400-409,490-499", 495, true)]
		[DataRow("400-409,490-499", 500, false)]

		[DataRow("*", 0, true)]
		[DataRow(",,,*", 9999, true)]

		[DataRow("", 0, false)]
		[DataRow(",,,", 9999, false)]
		public void parser_works(string pattern, int value, bool expectedResult) 
		{
			var ok = HttpStatusRangeParser.IsMatch(pattern, value);
			Assert.AreEqual(expectedResult, ok);
		}

		[TestMethod]
		[DataRow("-100")]
		[DataRow("100-")]
		[DataRow("1yy")]
		public void parser_throws_on_invalid_pattern(string pattern) {
            Assert.ThrowsException<ArgumentException>(() => HttpStatusRangeParser.IsMatch(pattern, 100));
		}
    }
}
