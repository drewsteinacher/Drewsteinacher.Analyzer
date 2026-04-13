// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Drewsteinacher.Analyzer.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    private class Outer
    {
        public Inner NestedWithoutInitialization { get; set; }
        public Inner NestedWithInitialization { get; set; }  = new();
    }

    private class Inner
    {
        public int Number { get; set; }
    }
    
    public static void SimpleDemo()
    {
        _ = new Outer
        {
            NestedWithInitialization =
            {
                Number = 1
            },
            NestedWithoutInitialization = //new() // This blows up without new()
            {
                Number = 2
            }
        };
    }
    
    
    private class Level1
    {
        public Level2 NestedLevel2WithInitialization { get; set; } = new();
        public Level2 NestedLevel2WithoutInitialization { get; set; }
    }

    private class Level2
    {
        public Level3 NestedLevel3WithInitialization { get; set; } = new();
        public Level3 NestedLevel3WithoutInitialization { get; set; }
    }

    private class Level3
    {
        public int Value { get; set; }
    }
  
    public static void ComplexDemo()
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
            NestedLevel2WithoutInitialization = //new() // This blows up without new()
            {
                NestedLevel3WithoutInitialization = //new() // This blows up without new()
                {
                    Value = 2
                }
            }
        };
    }
}