using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<Drewsteinacher.Analyzer.UninitializedPropertyInitializerAnalyzer,
        Drewsteinacher.Analyzer.UninitializedPropertyInitializerCodeFixProvider>;

namespace Drewsteinacher.Analyzer.Tests;

public class UninitializedPropertyInitializerCodeFixProviderTests
{
    [Fact]
    public async Task AddsNewObjectSyntax()
    {
        const string exampleCode
            = """
              public class Example
              {
                private class Outer
                {
                    public Inner NestedWithoutInitialization { get; set; }
                    public Inner NestedWithInitialization { get; set; } = new Inner();
                }

                private class Inner
                {
                    public int Number { get; set; }
                }
                
                public void RuntimeErrorDemo()
                {
                    _ = new Outer
                    {
                        NestedWithInitialization =
                        {
                            Number = 1
                        },
                        NestedWithoutInitialization =
                        {
                            Number = 2
                        },
                    };
                }
              }
              """;

        const string expectedCode
            = """
              public class Example
              {
                private class Outer
                {
                    public Inner NestedWithoutInitialization { get; set; }
                    public Inner NestedWithInitialization { get; set; } = new Inner();
                }

                private class Inner
                {
                    public int Number { get; set; }
                }
                
                public void RuntimeErrorDemo()
                {
                    _ = new Outer
                    {
                        NestedWithInitialization =
                        {
                            Number = 1
                        },
                        NestedWithoutInitialization =
                        new Inner
                        {
                            Number = 2
                        },
                    };
                }
              }
              """;

        var expected = Verifier.Diagnostic()
            .WithSpan(23, 11, 25, 12)
            .WithArguments("NestedWithoutInitialization");
        await Verifier.VerifyCodeFixAsync(exampleCode, expected, expectedCode).ConfigureAwait(false);
    }
}