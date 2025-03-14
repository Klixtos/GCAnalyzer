# GCAnalyzer

A Roslyn analyzer to enforce best practices for .NET garbage collection and resource management.

## Features

This analyzer provides the following diagnostics:

| ID      | Title                            | Category     | Severity | Description                                                                 |
|---------|----------------------------------|-------------|----------|-----------------------------------------------------------------------------|
| GCA001  | Avoid using GC.Collect directly | Performance | Warning  | Explicit garbage collection can cause performance issues and should be avoided in most cases. |
| GCA002  | Consider using GC.KeepAlive     | Reliability | Info     | In some scenarios, objects might be collected prematurely if they're not referenced elsewhere in the code. |
| GCA003  | Implement proper resource disposal | Usage    | Warning  | Classes that implement IDisposable should have their Dispose method called when they are no longer needed. |
| GCA004  | Interface methods should not throw exceptions | Design | Warning | Interface methods should define contracts without implementation details like exceptions. |
| GCA005  | Use underscore prefix for instance fields | Naming | Warning | Instance fields should follow the naming convention of starting with an underscore (_) followed by camelCase. |
| GCA006  | Avoid hardcoded string literals | Maintainability | Info | Hardcoded string literals can make code less maintainable and harder to localize. Use constants, resource files, or configuration values instead. |

## Installation

To use this analyzer in your project:

1. Clone this repository or download the source code
2. Build the analyzer project using Visual Studio or the .NET CLI
3. Edit your project file (.csproj) to add a reference to the analyzer:

```xml
<ItemGroup>
  <Analyzer Include="..\Path\To\GCAnalyzer.dll" />
</ItemGroup>
```

Replace `..\Path\To\GCAnalyzer.dll` with the actual path to the compiled GCAnalyzer.dll file.

To make the analyzer more strict, you can treat warnings as errors by adding this to your project file:

```xml
<PropertyGroup>
  <WarningsAsErrors>GCA001;GCA002;GCA003;GCA004;GCA005;GCA006</WarningsAsErrors>
</PropertyGroup>
```

## Usage Examples

### GCA001: Avoid using GC.Collect directly

❌ Bad:
```csharp
// Explicitly calling garbage collection
GC.Collect();
```

✅ Good:
```csharp
// Let the garbage collector do its work automatically
// If memory pressure is an issue, consider other approaches:
// - Use WeakReference
// - Implement proper IDisposable pattern
// - Look for memory leaks
```

### GCA002: Consider using GC.KeepAlive

❌ Potential issue:
```csharp
[DllImport("native.dll")]
static extern void ProcessBuffer(IntPtr buffer);

void ProcessData(object data)
{
    var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
    try
    {
        ProcessBuffer(handle.AddrOfPinnedObject());
    }
    finally
    {
        handle.Free();
    }
    // GC could potentially collect 'data' before the P/Invoke call completes
}
```

✅ Better:
```csharp
[DllImport("native.dll")]
static extern void ProcessBuffer(IntPtr buffer);

void ProcessData(object data)
{
    var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
    try
    {
        ProcessBuffer(handle.AddrOfPinnedObject());
    }
    finally
    {
        handle.Free();
    }
    // Ensure data is not collected prematurely
    GC.KeepAlive(data);
}
```

### GCA003: Implement proper resource disposal

❌ Bad:
```csharp
void ProcessFile(string path)
{
    // BAD: FileStream is created but never disposed
    var fileStream = new FileStream(path, FileMode.Open);
    
    // Do work with fileStream
    byte[] buffer = new byte[1024];
    fileStream.Read(buffer, 0, buffer.Length);
    
    // Method ends without disposing fileStream, leading to resource leak
}
```

✅ Good (using declaration, C# 8.0+):
```csharp
void ProcessFile(string path)
{
    // GOOD: 'using' keyword ensures disposal
    using var fileStream = new FileStream(path, FileMode.Open);
    
    // Do work with fileStream
    byte[] buffer = new byte[1024];
    fileStream.Read(buffer, 0, buffer.Length);
    
    // fileStream will be automatically disposed at the end of the method
}
```

✅ Good (using statement, traditional):
```csharp
void ProcessFile(string path)
{
    // GOOD: Traditional using block ensures disposal
    using (var fileStream = new FileStream(path, FileMode.Open))
    {
        // Do work with fileStream
        byte[] buffer = new byte[1024];
        fileStream.Read(buffer, 0, buffer.Length);
        
        // fileStream will be disposed when the block ends
    }
}
```

### GCA004: Interface methods should not throw exceptions

❌ Bad:
```csharp
public interface IDataProcessor
{
    // This violates the guideline by explicitly throwing an exception
    void ProcessData(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        // Processing logic...
    }
    
    /// <summary>
    /// Validates the input
    /// </summary>
    /// <param name="input">Input to validate</param>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    bool Validate(string input);
}
```

✅ Good:
```csharp
public interface IDataProcessor
{
    // No exception thrown in the interface definition
    void ProcessData(byte[] data);
    
    /// <summary>
    /// Validates the input
    /// </summary>
    /// <param name="input">Input to validate</param>
    /// <returns>True if input is valid, false otherwise</returns>
    bool Validate(string input);
}

// Implementation can handle exceptions
public class DataProcessor : IDataProcessor
{
    public void ProcessData(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        // Processing logic...
    }
    
    public bool Validate(string input)
    {
        try
        {
            // Validation logic
            return true;
        }
        catch (Exception ex)
        {
            // Log the exception
            return false;
        }
    }
}
```

### GCA005: Use underscore prefix for instance fields

❌ Bad:
```csharp
public class User
{
    private string name; // Incorrect: Missing underscore prefix
    private int userId;  // Incorrect: Missing underscore prefix
    private bool IsActive; // Incorrect: Missing underscore and starts with uppercase
    
    public User(string name, int userId)
    {
        this.name = name;
        this.userId = userId;
    }
}
```

✅ Good:
```csharp
public class User
{
    private string _name; // Correct: Underscore prefix with camelCase
    private int _userId;  // Correct: Underscore prefix with camelCase
    private bool _isActive; // Correct: Underscore prefix with camelCase
    
    // These don't need underscore prefixes:
    private const int MaxNameLength = 50; // Constant
    private static readonly int DefaultUserId = 0; // Static readonly
    
    public User(string name, int userId)
    {
        _name = name;
        _userId = userId;
    }
}
```

### GCA006: Avoid hardcoded string literals

❌ Bad:
```csharp
public class NotificationService
{
    public void SendMessage(string recipient)
    {
        // Hardcoded strings that would be better as constants or resources
        var message = "Welcome to our application! We hope you enjoy your experience.";
        var subject = "Welcome to Our App";
        
        // Send notification logic...
        Console.WriteLine($"Sending '{subject}' to {recipient} with message: {message}");
    }
    
    public void HandleError(Exception ex)
    {
        // Hardcoded error message
        LogError("An unexpected error occurred while processing your request. Please try again later.");
    }
}
```

✅ Good:
```csharp
public class NotificationService
{
    // Using constants
    private const string WelcomeSubject = "Welcome to Our App";
    
    // Or better yet, use a resource file
    public void SendMessage(string recipient)
    {
        var message = Resources.WelcomeMessage;
        var subject = Resources.WelcomeSubject;
        
        // Send notification logic...
        Console.WriteLine($"Sending '{subject}' to {recipient} with message: {message}");
    }
    
    public void HandleError(Exception ex)
    {
        // Using resource for localization
        LogError(Resources.GenericErrorMessage);
    }
}
```

## Building from Source

1. Clone the repository
2. Build the solution using Visual Studio or the .NET CLI:

```
dotnet build
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details. 