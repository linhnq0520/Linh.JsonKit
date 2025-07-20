# Linh.JsonKit

**Linh.JsonKit** is a powerful, flexible, and high-performance .NET toolkit for working with JSON. It is built on top of `System.Text.Json` and enhanced with features inspired by `Newtonsoft.Json` and the performance of `SpanJson` to provide a comprehensive solution for serializing, deserializing, and querying JSON data.

## ✨ Key Features

-   **`JConvert` - Intelligent Serialization & Deserialization:**
    -   A simple and familiar API, inspired by Newtonsoft's `JsonConvert`.
    -   Supports multiple naming conventions: `PascalCase`, `camelCase`, `snake_case`, and `kebab-case`.
    -   Highly flexible customization via the "options pattern," allowing fine-grained control.
    -   Smartly handles reference loops with `Throw`, `Ignore`, or `Preserve` options.
    -   Flexible enum handling: Deserializes from strings or numbers, and allows serializing as strings or numbers.
    -   Automatically converts `dynamic` and `Dictionary<string, object>` to native .NET types without intermediate `JsonElement` objects.
    -   Includes high-performance extension methods powered by `SpanJson` for critical hot paths.
-   **`JPath` - Powerful JSON Queries:**
    -   Query `JsonElement` and `JsonNode` structures using a simple path syntax (e.g., `data.users[0].name`).
    -   High-performance, zero-allocation path parsing powered by `ReadOnlySpan<char>`.
    -   Supports positive and negative array indexing (e.g., `[-1]` for the last element).
    -   Provides a type-safe (`TryGetValueByPath`) and flexible (`SelectNode`, `SelectElement`) API.

## 🚀 Installation

Install `Linh.JsonKit` via the NuGet Package Manager.

```shell
dotnet add package Linh.JsonKit
```

Or via the Package Manager Console:

```powershell
Install-Package Linh.JsonKit
```

## 📚 How to Use

### 1. `JConvert` - Serialization and Deserialization

Simply add `using Linh.JsonKit;` and `using Linh.JsonKit.Enums;` to get access to the powerful extension methods and configuration enums.

#### Serialize an Object to a JSON String

**Basic Usage:**

```csharp
var user = new { FirstName = "Linh", LastName = "Nguyen", IsActive = true };
string json = user.ToJson();
// Output: {"FirstName":"Linh","LastName":"Nguyen","IsActive":true}
```

**Customizing Serialization:**

Use the `options` pattern to configure naming conventions, indentation, and enum serialization.

```csharp
public enum UserStatus { Inactive, Active }
var userWithStatus = new { Name = "Linh", Status = UserStatus.Active };

string customizedJson = userWithStatus.ToJson(options =>
{
    options.NamingConvention = NamingConvention.CamelCase;
    options.EnumSerialization = EnumSerializationMode.AsString; // Serialize enum as "Active"
    options.WriteIndented = true;
});

/* Output:
{
  "name": "Linh",
  "status": "Active"
}
*/
```

**Advanced Customization:**

Access the underlying `System.Text.Json` options for more control.

```csharp
var product = new { ProductName = "Super Gadget", Price = 99.95m };

string advancedJson = product.ToJson(options =>
{
    options.NamingConvention = NamingConvention.KebabCaseLower;
    options.SystemTextJsonOptions.NumberHandling = JsonNumberHandling.WriteAsString;
});
// Output: {"product-name":"Super Gadget","price":"99.95"}
```

#### Deserialize a JSON String to an Object

Use the `FromJson<T>()` extension method.

**Basic Deserialization:**

By default, deserialization is case-insensitive.

```csharp
public record User(string FirstName, string LastName);

string json = @"{ ""firstName"": ""Linh"", ""lastName"": ""Nguyen"" }";
User? user = json.FromJson<User>();

Console.WriteLine(user?.FirstName); // Output: Linh
```

**Flexible Enum Deserialization:**

The built-in enum converter intelligently handles strings (case-insensitive names or numbers) and integer values without extra configuration.

```csharp
public record UserWithStatus(string Name, UserStatus Status);

// All of these will deserialize correctly to UserStatus.Active
"{\"Name\":\"Test\", \"Status\":\"Active\"}".FromJson<UserWithStatus>();
"{\"Name\":\"Test\", \"Status\":1}".FromJson<UserWithStatus>();
"{\"Name\":\"Test\", \"Status\":\"1\"}".FromJson<UserWithStatus>();
```

#### High-Performance `SpanJson` Helpers

For maximum performance in hot paths, use the built-in extension methods powered by `SpanJson`.

**Note:** These methods use a separate `SpanJson` pipeline and **do not** apply the options (like `NamingConvention`) configured for the main `ToJson` method.

```csharp
var user = new { FirstName = "Linh", LastName = "Nguyen" };

// Serialize
byte[] utf8Bytes = user.ToJsonUtf8Bytes();
string utf16String = user.ToJsonUtf16();

// Deserialize
var userFromBytes = utf8Bytes.FromJsonUtf8<User>();
```

---

### 2. `JPath` - JSON Queries

`JPath` allows you to access deeply nested values within `JsonElement` or `JsonNode` safely and efficiently.

**Setup:**

```csharp
var json = """
{
  "store": {
    "books": [
      { "title": "The Hitchhiker's Guide", "price": 12.50 },
      { "title": "Ready Player One", "price": 15.99 }
    ],
    "owner": { "name": "Linh" }
  }
}
""";
var rootElement = JsonDocument.Parse(json).RootElement;
```

**Get a Value with `TryGetValueByPath`:**

This is the safest way to extract data. It returns `true` if the value is found and successfully converted.

```csharp
// Get a string value from a nested object
if (rootElement.TryGetValueByPath("store.owner.name", out string? ownerName))
{
    Console.WriteLine($"Owner: {ownerName}"); // Output: Owner: Linh
}

// Get a numeric value from an array using a negative index
if (rootElement.TryGetValueByPath("store.books[-1].price", out decimal price))
{
    Console.WriteLine($"Last book price: {price}"); // Output: Last book price: 15.99
}

// A non-existent path will safely return false
if (!rootElement.TryGetValueByPath("store.manager.name", out string? managerName))
{
    Console.WriteLine("Manager not found."); // Output: Manager not found.
}
```

**Select a Node/Element with `SelectElement`:**

Use this if you need a subsection of the JSON tree for further processing.

```csharp
// Select a child object
JsonElement? ownerElement = rootElement.SelectElement("store.owner");
if (ownerElement.HasValue)
{
    // You can use JConvert to re-serialize the selected part
    Console.WriteLine(ownerElement.Value.ToJson(o => o.WriteIndented = true));
    /* Output:
     {
       "name": "Linh"
     }
    */
}
```

---

### 🏛️ Architecture & Performance Notes

`Linh.JsonKit` is designed with performance and flexibility in mind.

-   **Optimized Caching:** `JConvert` pre-builds and caches `JsonSerializerOptions` instances for common configurations. This significantly speeds up repeated serialization tasks.

-   **Safe Customization:** When you provide custom configurations, `JConvert` creates a safe, temporary copy of the cached settings. This guarantees thread-safety and prevents "read-only" errors in multi-threaded or benchmark scenarios.

-   **Zero-Allocation Path Parsing:** `JPath` uses `ReadOnlySpan<char>` and `ref struct` parsers to navigate JSON paths without allocating memory on the managed heap, making it extremely efficient for querying tasks.

## ❤️ Contributing

Contributions are always welcome! If you have ideas, suggestions, or find a bug, please open an [issue](https://github.com/linhnq0520/Linh.JsonKit/issues) or create a [pull request](https://github.com/linhnq0520/Linh.JsonKit/pulls).

---
Made with ❤️ by Linh Nguyen.