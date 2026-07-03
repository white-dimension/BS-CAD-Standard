# Future C# Migration Plan

The current MaxScript implementation is a fast MVP shell. It is structured so the project can move to C# without changing the product model.

## Target Stack

- C#
- .NET Framework matching the installed 3ds Max version
- 3ds Max SDK
- WPF or custom WinForms host
- Dockable panel integration through 3ds Max APIs

## Suggested Namespaces

```text
BSMaxStudio.Core
BSMaxStudio.UI
BSMaxStudio.Modules.Layer
BSMaxStudio.Modules.Material
BSMaxStudio.Modules.Corona
BSMaxStudio.Modules.Model
BSMaxStudio.Modules.Camera
BSMaxStudio.Utils
```

## First Migration Step

Keep the same module contract:

```csharp
public interface IBSModule
{
    string Id { get; }
    string Title { get; }
    IReadOnlyList<IBSCommand> Commands { get; }
}
```

The MaxScript `BSMS_RegisterModule` function is the prototype for that future interface.
