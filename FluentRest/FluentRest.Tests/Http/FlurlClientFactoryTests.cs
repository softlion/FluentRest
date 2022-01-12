using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Http.Configuration;

namespace FluentRest.Test.Http
{
	[TestClass]
    public class FluentRestClientFactoryTests
    {
	    [TestMethod]
	    public void default_factory_provides_same_client_per_host_scheme_port() {
		    var fac = new DefaultFluentRestClientFactory();
		    var cli1 = fac.Get("http://api.com/foo");
		    var cli2 = fac.Get("http://api.com/bar");
		    var cli3 = fac.Get("https://api.com/foo");
		    var cli4 = fac.Get("https://api.com/bar");
		    var cli5 = fac.Get("https://api.com:1234/foo");
		    var cli6 = fac.Get("https://api.com:1234/bar");

		    Assert.AreSame(cli1, cli2);
		    Assert.AreSame(cli3, cli4);
		    Assert.AreSame(cli5, cli6);

		    Assert.AreNotSame(cli1, cli3);
		    Assert.AreNotSame(cli3, cli5);
	    }

		[TestMethod]
	    public void per_base_url_factory_provides_same_client_per_provided_url() {
		    var fac = new PerBaseUrlFluentRestClientFactory();
		    var cli1 = fac.Get("http://api.com/foo");
		    var cli2 = fac.Get("http://api.com/bar");
		    var cli3 = fac.Get("http://api.com/foo");
		    Assert.AreNotSame(cli1, cli2);
		    Assert.AreSame(cli1, cli3);
	    }

	    [TestMethod]
	    public void can_configure_client_from_factory() {
		    var fac = new DefaultFluentRestClientFactory()
			    .ConfigureClient("http://api.com/foo", c => c.Settings.Timeout = TimeSpan.FromSeconds(123));
		    Assert.AreEqual(TimeSpan.FromSeconds(123), fac.Get("http://api.com/bar").Settings.Timeout);
		    Assert.AreNotEqual(TimeSpan.FromSeconds(123), fac.Get("http://api2.com/foo").Settings.Timeout);
	    }

		[TestMethod]
	    public async Task ConfigureClient_is_thread_safe() {
		    var fac = new DefaultFluentRestClientFactory();
		    var sequence = new List<int>();

		    var task1 = Task.Run(() => fac.ConfigureClient("http://api.com", c => {
			    sequence.Add(1);
				Thread.Sleep(5000);
			    sequence.Add(3);
		    }));

			await Task.Delay(200);

			// modifies same client as task1, should get blocked until task1 is done
			var task2 = Task.Run(() => fac.ConfigureClient("http://api.com", c => {
			    sequence.Add(4);
		    }));

			await Task.Delay(200);

			// modifies different client, should run immediately
			var task3 = Task.Run(() => fac.ConfigureClient("http://api2.com", c => {
				sequence.Add(2);
			}));

			await Task.WhenAll(task1, task2, task3);
		    Assert.AreEqual("1,2,3,4", string.Join(",", sequence));
	    }
	}
}
