using FluentRest.Http.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluentRest.Test.Http
{
	public abstract class HttpTestFixtureBase
	{
		protected HttpTest HttpTest { get; private set; }

		[TestInitialize]
		public void CreateHttpTest() {
			HttpTest = new HttpTest();
		}

		[TestCleanup]
		public void DisposeHttpTest() {
			HttpTest.Dispose();
		}
	}
}
