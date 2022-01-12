using System;
using System.IO;
using System.Text.Json;
using FluentRest.Rest.Configuration;

namespace FluentRest.Http.Configuration
{
	public class SystemTextJsonSerializer : ISerializer
	{
		public static ISerializer Default { get; } = new SystemTextJsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.General));

		private readonly JsonSerializerOptions? options;

		public SystemTextJsonSerializer(JsonSerializerOptions? options = null) => this.options = options;

		public string Serialize(object obj) => JsonSerializer.Serialize(obj, options);
		public T? Deserialize<T>(string s) => JsonSerializer.Deserialize<T>(s, options);
		public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream);
	}
}
