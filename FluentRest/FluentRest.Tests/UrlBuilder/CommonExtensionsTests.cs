﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentRest.Urls;

namespace FluentRest.Test.UrlBuilder
{
	[TestClass]
	public class CommonExtensionsTests
	{
		[TestMethod]
		public void can_parse_object_to_kv()
		{
			var kv = new {
				one = 1,
				two = "foo",
				three = (string)null
			}.ToKeyValuePairs();

			AssertKV(kv, ("one", 1), ("two", "foo"), ("three", null));
		}

		[TestMethod]
		public void can_parse_dictionary_to_kv()
		{
			var kv = new Dictionary<string, object> {
				{ "one", 1 },
				{ "two", "foo" },
				{ "three", null }
			}.ToKeyValuePairs();

			AssertKV(kv, ( "one", 1 ), ( "two", "foo" ), ( "three", null ));
		}

		[TestMethod]
		public void can_parse_collection_of_kvp_to_kv()
		{
			var kv = new[] {
				new KeyValuePair<object, object>("one", 1),
				new KeyValuePair<object, object>("two", "foo"),
				new KeyValuePair<object, object>("three", null),
				new KeyValuePair<object, object>(null, "four"),
			}.ToKeyValuePairs();

			AssertKV(kv, ("one", 1), ("two", "foo"), ("three", null));
		}

		[TestMethod]
		public void can_parse_collection_of_conventional_objects_to_kv()
		{
			// convention is to accept collection of any arbitrary type that contains
			// a property called Key or Name and a property called Value
			var kv = new object[] {
				new { Key = "one", Value = 1 },
				new { key = "two", value = "foo" }, // lower-case should work too
				new { Key = (string)null, Value = 3 }, // null keys should get skipped
				new { Name = "three", Value = (string)null },
				new { name = "four", value = "bar" } // lower-case should work too
			}.ToKeyValuePairs().ToList();

			AssertKV(kv, ("one", 1), ("two", "foo"), ("three", null), ("four", "bar"));
		}

		[TestMethod]
		public void can_parse_string_to_kv()
		{
			var kv = "one=1&two=foo&three".ToKeyValuePairs();
			AssertKV(kv, ("one", "1"), ("two", "foo"), ("three", null));
		}

		[TestMethod]
		public void cannot_parse_null_to_kv()
		{
			object obj = null;
			Assert.ThrowsException<ArgumentNullException>(() => obj.ToKeyValuePairs());
		}

		private class ToKvMe
		{
			public string Read1 { get; private set; }
			public string Read2 { get; private set; }

			private string PrivateRead1 { get; set; } = "c";
			public string PrivateRead2 { private get; set; }

			public string WriteOnly {
				set {
					Read1 = value.Split(',')[0];
					Read2 = value.Split(',')[1];
				}
			}
		}

		[TestMethod] // #373
		public void object_to_kv_ignores_props_not_publicly_readable() {
			var kv = new ToKvMe {
				PrivateRead2 = "foo",
				WriteOnly = "a,b"
			}.ToKeyValuePairs();

			AssertKV(kv, ("Read1", "a"), ("Read2", "b"));
		}

		[TestMethod]
		public void SplitOnFirstOccurence_works() {
			var result = "hello/how/are/you".SplitOnFirstOccurence("/");
			CollectionAssert.AreEqual(new[] { "hello", "how/are/you" }, result);
		}

		[TestMethod]
		[DataRow("   \"\thi there \"  \t\t ", "\thi there ")]
		[DataRow("   '  hi there  '   ", "  hi there  ")]
		[DataRow("  hi there  ", "  hi there  ")]
		public void StripQuotes_works(string s, string expected)
		{
			var r = s.StripQuotes();
			Assert.AreEqual(expected, r);
		}

		[TestMethod]
		public void ToInvariantString_serializes_dates_to_iso() {
			Assert.AreEqual("2017-12-01T02:34:56.7890000", new DateTime(2017, 12, 1, 2, 34, 56, 789, DateTimeKind.Unspecified).ToInvariantString());
			Assert.AreEqual("2017-12-01T02:34:56.7890000Z", new DateTime(2017, 12, 1, 2, 34, 56, 789, DateTimeKind.Utc).ToInvariantString());
			Assert.AreEqual("2017-12-01T02:34:56.7890000-06:00", new DateTimeOffset(2017, 12, 1, 2, 34, 56, 789, TimeSpan.FromHours(-6)).ToInvariantString());
		}

		private void AssertKV(IEnumerable<(string, object)> actual, params (string, object)[] expected) 
		{
			CollectionAssert.AreEqual(expected, actual.ToList());
		}
	}
}
