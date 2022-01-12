using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace FluentRest.SourceGenerator
{
	/// <summary>
	/// https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md
	/// </summary>
	[Generator]
	public class FluentRestSourceGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			// No initialization required for this one
		}

		public void Execute(GeneratorExecutionContext context)
		{
			var (url,http) = Main();

			if(url != null)
				context.AddSource($"GeneratedExtensions.Urls.g.cs", url);			
			if(http != null)
				context.AddSource($"GeneratedExtensions.Http.g.cs", http);
		}

		(string?,string?) Main() 
		{
			var sbUrl = new StringBuilder();
			var sbHttp = new StringBuilder();
			
			try 
			{
				//Url
				var writer = new CodeWriter(sbUrl);
				writer
					.WriteLine("// This file was auto-generated. Do not edit directly.")
					.WriteLine("using System;")
					.WriteLine("using System.Collections.Generic;")
					.WriteLine("")
					.WriteLine("namespace FluentRest.Urls")
					.WriteLine("{")
					.WriteLine("/// <summary>")
					.WriteLine("/// Fluent URL-building extension methods on String and Uri.")
					.WriteLine("/// </summary>")
					.WriteLine("public static class GeneratedExtensions")
					.WriteLine("{");

				WriteUrlBuilderExtensionMethods(writer);

				writer
					.WriteLine("}")
					.WriteLine("}");
			}
			catch (Exception ex) 
			{
				ShowError(ex.ToString());
				return (null, null);
			}

			try 
			{
				//Http
				var writer = new CodeWriter(sbHttp);
				writer
					.WriteLine("// This file was auto-generated. Do not edit directly.")
					.WriteLine("using System;")
					.WriteLine("using System.Collections.Generic;")
					.WriteLine("using System.IO;")
					.WriteLine("using System.Net;")
					.WriteLine("using System.Net.Http;")
					.WriteLine("using System.Threading;")
					.WriteLine("using System.Threading.Tasks;")
					.WriteLine("using FluentRest.Http.Configuration;")
					.WriteLine("using FluentRest.Http.Content;")
                    .WriteLine("using FluentRest.Urls;")
					.WriteLine("")
					.WriteLine("namespace FluentRest.Http")
					.WriteLine("{")
					.WriteLine("/// <summary>")
					.WriteLine("/// Fluent extension methods on String, Url, Uri, and IFluentRestRequest.")
					.WriteLine("/// </summary>")
					.WriteLine("public static class GeneratedExtensions")
					.WriteLine("{");

				WriteHttpExtensionMethods(writer);

				writer
					.WriteLine("}")
					.WriteLine("}");
			}
			catch (Exception ex) 
			{
				ShowError(ex.ToString());
				return (null, null);
			}

			return (
				sbUrl.ToString(),
                sbHttp.ToString()
			);
		}

		private static void ShowError(string error) 
		{
			// Console.ForegroundColor = ConsoleColor.Red;
			// Console.WriteLine(error);
			// Console.ReadLine();
		}

		private static MethodArg[] _extendedArgs = new[] {
			new MethodArg { Name = "request", Type = "IFluentRestRequest", Description = "This IFluentRestRequest" },
			new MethodArg { Name = "url", Type = "Url", Description = "This FluentRest.Url." },
			new MethodArg { Name = "url", Type = "string", Description = "This URL." },
			new MethodArg { Name = "uri", Type = "Uri", Description = "This System.Uri." }
		};

		private static void WriteUrlBuilderExtensionMethods(CodeWriter writer) {
			foreach (var xarg in _extendedArgs.Skip(2)) { // skip 2 because we only auto-gen for string and Uri
				foreach (var xm in ApiMetadata.GetUrlReturningExtensions(xarg)) {
					Console.WriteLine($"writing {xm.Name} for {xarg.Type}...");
					xm.Write(writer, $"new Url({xarg.Name})");
				}
			}
		}

		private static void WriteHttpExtensionMethods(CodeWriter writer) 
        {
			var reqArg = _extendedArgs[0];

			foreach (var xm in ApiMetadata.GetHttpCallingExtensions(reqArg)) 
            {
				Console.WriteLine($"writing {xm.Name} for IFluentRestRequest...");
				xm.Write(writer, () => {
					var args = new List<string>();
					var genericArg = xm.IsGeneric ? "<T>" : "";

					args.Add(
						xm.HttpVerb == null ? "verb" :
						xm.HttpVerb == "Patch" ? "new HttpMethod(\"PATCH\")" : // there's no HttpMethod.Patch
						"HttpMethod." + xm.HttpVerb);

					if (xm.HasRequestBody)
						args.Add("content: content");

					args.Add("cancellationToken: cancellationToken");
					args.Add("completionOption: completionOption");

					if (xm.RequestBodyType != null) {
						writer.WriteLine("var content = new Captured@0Content(@1);",
							xm.RequestBodyType,
							xm.RequestBodyType == "String" ? "data" : $"request.Settings.{xm.RequestBodyType}Serializer.Serialize(data)");
					}

					var receive = (xm.ResponseBodyType != null) ? $".Receive{xm.ResponseBodyType}{genericArg}()" : "";
					writer.WriteLine($"return {reqArg.Name}.SendAsync({string.Join(", ", args)}){receive};");
				});
			}

			foreach (var xarg in _extendedArgs.Skip(1)) 
            { 
                // skip 1 because these don't apply to IFluentRestRequest
				foreach (var xm in ApiMetadata.GetHttpCallingExtensions(xarg).Concat(ApiMetadata.GetRequestReturningExtensions(xarg))) 
                {
					Console.WriteLine($"writing {xm.Name} for {xarg.Type}...");
					xm.Write(writer, $"new FluentRestRequest({xarg.Name})");
				}
			}
		}
	}
}