using System.Text;
using System.Threading.Tasks;
using FluentRest.SourceGenerator;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = SourceGenerator.Tests.CSharpSourceGeneratorVerifier<FluentRest.SourceGenerator.FluentRestSourceGenerator>;

namespace SourceGenerator.Tests
{
    [TestClass]
    public class TestGenerator
    {
        /// <summary>
        /// Will fail, but enabled debugging of the generator by debugging the unit test
        /// see https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators
        /// </summary>
        [TestMethod]
        public async Task TestGeneration()
        {
            var code = "initial code";
            var generated = "expected generated code";

            await new VerifyCS.Test
            {
                TestState = 
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(FluentRestSourceGenerator), "GeneratedFileName", SourceText.From(generated, Encoding.UTF8, SourceHashAlgorithm.Sha256)),
                    },
                },
            }.RunAsync();
        }
    }
}
