using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentRest.Rest.Configuration;

namespace FluentRest.Http.Configuration
{
	public class SystemTextJsonSerializer : ISerializer
	{
		private static readonly JsonSerializerOptions? DefaultOptions;
		public static ISerializer Default { get; }
		private readonly JsonSerializerOptions? options;

		static SystemTextJsonSerializer()
		{
			DefaultOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
			{
				PropertyNameCaseInsensitive = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
			};
			Default = new SystemTextJsonSerializer(DefaultOptions);
		}

		public SystemTextJsonSerializer(JsonSerializerOptions? options = null) => this.options = options ?? DefaultOptions;

		public string Serialize(object obj) => JsonSerializer.Serialize(obj, options);
		public T? Deserialize<T>(string s) => JsonSerializer.Deserialize<T>(s, options);
		public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, options);
	}
}
