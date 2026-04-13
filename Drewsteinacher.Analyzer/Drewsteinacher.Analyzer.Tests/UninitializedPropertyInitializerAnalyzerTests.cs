using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        Drewsteinacher.Analyzer.UninitializedPropertyInitializerAnalyzer>;

namespace Drewsteinacher.Analyzer.Tests;

public class MySyntaxAnalyzerTests
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

    [Fact]
    public void RuntimeErrorDemo()
    {
        var action = () =>
        {
            _ = new Outer
            {
                NestedWithInitialization =
                {
                    Number = 1
                },
                NestedWithoutInitialization = // new() // This blows up without new()
                {
                    Number = 2
                }
            };
        };

        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task ClassWithoutProblem_IsIgnored()
    {
        const string text
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
                        NestedWithoutInitialization = new()
                        {
                            Number = 2
                        },
                    };
                }
              }
              """;

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task ClassWithProblem_IsDetected()
    {
        const string text
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

        var expected = Verifier.Diagnostic()
            .WithSpan(23, 11, 25, 12)
            .WithArguments("NestedWithoutInitialization");
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task ClassWithProblem_NestedTwoLayersDeeper_IsDetected()
    {
        const string text
            = """
              public class Example
              {
                private class Level1
                {
                    public Level2 NestedLevel2WithInitialization { get; set; } = new Level2();
                    public Level2 NestedLevel2WithoutInitialization { get; set; }
                }

                private class Level2
                {
                    public Level3 NestedLevel3WithInitialization { get; set; } = new Level3();
                    public Level3 NestedLevel3WithoutInitialization { get; set; }
                }

                private class Level3
                {
                    public int Value { get; set; }
                }
                
                public void Test()
                {
                    _ = new Level1
                    {
                        NestedLevel2WithInitialization =
                        {
                            NestedLevel3WithInitialization =
                            {
                                Value = 1
                            }
                        },
                        NestedLevel2WithoutInitialization = new()
                        {
                            NestedLevel3WithoutInitialization =
                            {
                                Value = 2
                            }
                        }
                    };
                }
              }
              """;

        var expected = Verifier.Diagnostic()
            .WithSpan(34, 15, 36, 16)
            .WithArguments("NestedLevel3WithoutInitialization");
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}