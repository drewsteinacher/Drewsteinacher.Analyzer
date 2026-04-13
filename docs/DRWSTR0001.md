# DRWSTR0001 - Uninitialized Property Assigned in Member Initializer

### Example Message
To prevent runtime errors, add `new()` here or initialize property `SomeProperty` with a non-null default value.

### Description
Properties without initializers cannot be used directly in a member initializer without causing a `NullReferenceException` at runtime.
Either use `new()` syntax in the member initializer or initialize the property with a default value (e.g.,`Property { get; set; } = new ClassName()`).

### Background
When a property is not initialized, it defaults to `null`:
```csharp
public class Outer {
    public Inner SomeProperty { get; set; }  // Not initialized, defaults to null
    public Inner SomePropertyWithDefault { get; set; } = new(); // Initialized with a non-null default value
}

public class Inner {
    public int Value { get; set; }
}
```

Initializing an object with class-typed properties can be done [with or without the `new` keyword](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/object-and-collection-initializers#key-differences):
```csharp
_ = new Outer
    {
        SomeProperty = new() // Creates a new instance of Inner
        {
            Value = 42
        },
        SomePropertyWithDefault = // Uses the already initialized instance of Inner
        {
            Value = 42
        }
    };
```

However, if you omit the `new()` syntax for a non-initialized property, it will always lead to a runtime error, despite being compilable and syntactically valid:
```csharp
_ = new Outer
    {
        SomeProperty = // Uses the pre-existing default value of null
        {
            Value = 42 // Tries to assign the property to null, throwing NullReferenceException at runtime
        }
    };
```

### Solutions
To fix this issue, either:
1. Add `new()` syntax in the member initializer:
```csharp
_ = new Outer
    {
        SomeProperty = new() // Creates a new instance of Inner
        {
            Value = 42 // Successfully assigns the property to it
        }
    };
```

2. Initialize the property with a default value:
```csharp
public class Outer {
    public Inner SomeProperty { get; set; } = new(); // Initialized with a non-null default value
}

_ = new Outer
    {
        SomeProperty = // Uses the pre-existing default value
        {
            Value = 42 // Successfully assigns the property to it
        }
    };
```

### Remarks
This issue warranted a dedicated analyzer rule for several reasons:
- It is completely valid syntax that compiles successfully without any warnings or errors.
- It is guaranteed to cause a runtime error if executed.
- It often appears in AI-generated code.
- It is easily missed by human reviewers.
- It is not easily detected by or explained in sufficient detail by AI tools.
- Hunting it down, correcting it, and understanding it can waste a lot of time.
- It is almost always a simple typo that is overlooked.
