﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using FluentRest.Http.Configuration;

namespace FluentRest.Http.Testing
{
	/// <summary>
	/// An object whose existence puts FluentRest.Http into test mode where actual HTTP calls are faked. Provides a response
	/// queue, call log, and assertion helpers for use in Arrange/Act/Assert style tests.
	/// </summary>
	[Serializable]
	public class HttpTest : HttpTestSetup, IDisposable
	{
		private readonly ConcurrentQueue<FluentRestDetail> _calls = new ConcurrentQueue<FluentRestDetail>();
		private readonly List<FilteredHttpTestSetup> _filteredSetups = new List<FilteredHttpTestSetup>();
		private readonly Lazy<HttpClient> _httpClient;
		private readonly Lazy<HttpMessageHandler> _httpMessageHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpTest"/> class.
		/// </summary>
		/// <exception cref="Exception">A delegate callback throws an exception.</exception>
		public HttpTest() : base(new TestFluentRestHttpSettings()) {
			_httpClient = new Lazy<HttpClient>(() => Settings.HttpClientFactory.CreateHttpClient(HttpMessageHandler));
			_httpMessageHandler = new Lazy<HttpMessageHandler>(() => Settings.HttpClientFactory.CreateMessageHandler());
		    SetCurrentTest(this);
	    }

		internal HttpClient HttpClient => _httpClient.Value;
		internal HttpMessageHandler HttpMessageHandler => _httpMessageHandler.Value;
		internal void LogCall(FluentRestDetail call) => _calls.Enqueue(call);

		/// <summary>
		/// Gets the current HttpTest from the logical (async) call context
		/// </summary>
		public static HttpTest Current => GetCurrentTest();

		/// <summary>
		/// List of all (fake) HTTP calls made since this HttpTest was created.
		/// </summary>
		public IReadOnlyList<FluentRestDetail> CallLog => new ReadOnlyCollection<FluentRestDetail>(_calls.ToList());

		/// <summary>
		/// Change FluentRestHttpSettings for the scope of this HttpTest.
		/// </summary>
		/// <param name="action">Action defining the settings changes.</param>
		/// <returns>This HttpTest</returns>
		public HttpTest Configure(Action<TestFluentRestHttpSettings> action) {
			action(Settings);
			return this;
		}

		/// <summary>
		/// Fluently creates and returns a new request-specific test setup. 
		/// </summary>
		public FilteredHttpTestSetup ForCallsTo(params string[] urlPatterns) {
			var setup = new FilteredHttpTestSetup(Settings, urlPatterns);
			_filteredSetups.Add(setup);
			return setup;
		}

		internal HttpTestSetup FindSetup(FluentRestDetail call) {
			return _filteredSetups.FirstOrDefault(ts => ts.IsMatch(call)) ?? (HttpTestSetup)this;
		}

		/// <summary>
		/// Asserts whether matching URL was called, throwing HttpCallAssertException if it wasn't.
		/// </summary>
		/// <param name="urlPattern">URL that should have been called. Can include * wildcard character.</param>
		public HttpCallAssertion ShouldHaveCalled(string urlPattern) {
			return new HttpCallAssertion(CallLog).WithUrlPattern(urlPattern);
		}

		/// <summary>
		/// Asserts whether matching URL was NOT called, throwing HttpCallAssertException if it was.
		/// </summary>
		/// <param name="urlPattern">URL that should not have been called. Can include * wildcard character.</param>
		public void ShouldNotHaveCalled(string urlPattern) {
			new HttpCallAssertion(CallLog, true).WithUrlPattern(urlPattern);
		}

		/// <summary>
		/// Asserts whether any HTTP call was made, throwing HttpCallAssertException if none were.
		/// </summary>
		public HttpCallAssertion ShouldHaveMadeACall() {
			return new HttpCallAssertion(CallLog).WithUrlPattern("*");
		}

		/// <summary>
		/// Asserts whether no HTTP calls were made, throwing HttpCallAssertException if any were.
		/// </summary>
		public void ShouldNotHaveMadeACall() {
			new HttpCallAssertion(CallLog, true).WithUrlPattern("*");
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		public void Dispose() {
			SetCurrentTest(null);
		}

		private static readonly System.Threading.AsyncLocal<HttpTest> _test = new System.Threading.AsyncLocal<HttpTest>();
		private static void SetCurrentTest(HttpTest test) => _test.Value = test;
		private static HttpTest GetCurrentTest() => _test.Value;
	}
}