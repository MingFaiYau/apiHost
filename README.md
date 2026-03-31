# Plugin-Based Web API Host

A dynamic plugin-based web API hosting framework built on .NET 9+, designed to provide a flexible and extensible foundation for hosting multiple web services with hot-reload support.

## Features

- **Dynamic Plugin Loading** - Load plugins from `plugins/functions/` folder at runtime
- **Hot-Reload Support** - Add, remove, or modify plugins without restarting the host
- **Generic CRUD Service** - Built-in support for generic Create, Read, Update, Delete operations
- **Custom Business Services** - Support for additional custom web services beyond CRUD
- **Schema Discovery Service** - Built-in service providing schema information for all registered plugins

## Project Structure

```
PluginWebApiHost/
├── src/
│   ├── ApiHost/                  # Main Web API Host
│   │   ├── Services/             # Core services (PluginLoader, PluginRouter)
│   │   └── appsettings.json      # Configuration
│   ├── SharedLib/                # Shared interfaces & models
│   │   ├── IPlugin.cs            # Base plugin interface
│   │   ├── ICrudPlugin.cs        # CRUD plugin interface
│   │   ├── ICustomPlugin.cs      # Custom plugin interface
│   │   ├── Config/               # Configuration models
│   │   └── Schema/               # Schema definitions
│   └── Plugins/
│       ├── PluginBase/           # Base classes for plugins
│       ├── Plugin1000/           # Example: Product CRUD plugin
│       └── Plugin2000/           # Example: Custom Order Report plugin
├── plugins/
│   └── functions/                # Plugin DLLs location (copy .dll files here)
└── README.md
```

## Quick Start

### 1. Clone and Build

```bash
git clone https://github.com/MingFaiYau/apiHost.git
cd apiHost
dotnet build
```

### 2. Setup Plugins

Copy your plugin DLLs to the `plugins/functions/` folder:

```bash
mkdir -p plugins/functions
# Copy Plugin1000.dll and Plugin2000.dll to plugins/functions/
```

Or copy from build output:

```bash
cp src/Plugins/Plugin1000/bin/Debug/net9.0/Plugin1000.dll plugins/functions/
cp src/Plugins/Plugin2000/bin/Debug/net9.0/Plugin2000.dll plugins/functions/
```

### 3. Run the Host

```bash
dotnet run --project src/ApiHost
```

The API will be available at `http://localhost:5000`

## API Endpoints

### Schema Discovery

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/schema` | Get all registered schemas |
| GET | `/schema/{functionId}` | Get schema for specific function |

### Example: Product CRUD (Plugin1000)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create new product (not implemented in demo) |
| PUT | `/api/products/{id}` | Update product (not implemented in demo) |
| DELETE | `/api/products/{id}` | Delete product |

### Example: Order Report (Plugin2000)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/reports/orders/daily` | Get daily order report |
| GET | `/api/reports/orders/summary` | Get order summary report |

## Configuration

Edit `src/ApiHost/appsettings.json`:

```json
{
  "PluginHost": {
    "PluginsPath": "plugins",
    "EnableHotReload": true,
    "ScanInterval": "00:00:10",
    "BaseUrl": "http://localhost:5000"
  },
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=pluginhost.db"
  }
}
```

| Setting | Description |
|---------|-------------|
| `PluginsPath` | Path to plugins folder (relative to output directory) |
| `EnableHotReload` | Enable automatic plugin detection |
| `ScanInterval` | How often to scan for changes (default: 10 seconds) |
| `BaseUrl` | URL to bind the server |

## Creating a New Plugin

### 1. Create a new plugin project

```bash
cd src/Plugins
dotnet new classlib -n Plugin3000 -f net9.0
```

### 2. Add reference to PluginBase

Edit `Plugin3000.csproj`:

```xml
<ProjectReference Include="..\PluginBase\PluginBase.csproj" />
```

### 3. Implement the plugin

**CRUD Plugin Example:**

```csharp
using System.Collections.Generic;
using PluginBase;
using SharedLib.Schema.Attributes;

namespace Plugin3000;

[Schema("Customer", Description = "Customer management")]
public class Customer
{
    public int Id { get; set; }
    [SchemaProperty(Description = "Customer name", IsRequired = true, MaxLength = 100)]
    public string Name { get; set; } = "";
}

public class CustomerPlugin : CrudPluginBase<Customer>
{
    private static readonly List<Customer> _customers = new();

    public override int FunctionId => 3000;
    public override string Name => "Customer Service";
    public override string Version => "1.0.0";

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        return base.InitializeAsync(serviceProvider).ContinueWith(_ =>
        {
            BuildSchemaProperties();
            var schema = BuildSchema("Customer", "Customer CRUD");
            RegisterSchema(schema);
        });
    }

    public override IReadOnlyList<PluginRoute> GetRoutes()
    {
        return new List<PluginRoute>
        {
            new("/api/customers", "GET"),
            new("/api/customers/{id}", "GET"),
            new("/api/customers", "POST"),
            new("/api/customers/{id}", "PUT"),
            new("/api/customers/{id}", "DELETE")
        };
    }

    public override Task<IReadOnlyList<Customer>> GetAllAsync() 
        => Task.FromResult<IReadOnlyList<Customer>>(_customers.ToList());

    public override Task<Customer?> GetByIdAsync(int id) 
        => Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));

    // Implement CreateAsync, UpdateAsync, DeleteAsync...
}
```

**Custom Plugin Example:**

```csharp
public class ReportPlugin : CustomPluginBase
{
    private readonly Dictionary<string, RequestHandler> _handlers = new();

    public override int FunctionId => 3000;
    public override string Name => "Report Service";

    public override IReadOnlyDictionary<string, RequestHandler> Handlers => _handlers;

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        return base.InitializeAsync(serviceProvider).ContinueWith(_ =>
        {
            RegisterCustomSchema("Report", "Report service");
            _handlers["/api/reports"] = HandleReport;
        });
    }

    public override IReadOnlyList<PluginRoute> GetRoutes() 
        => new List<PluginRoute> { new("/api/reports", "GET") };

    private Task HandleReport(object context)
    {
        // Your custom logic
        return Task.CompletedTask;
    }
}
```

### 4. Build and Copy

```bash
# Build the plugin
dotnet build src/Plugins/Plugin3000/Plugin3000.csproj

# Copy to plugins folder
cp src/Plugins/Plugin3000/bin/Debug/net9.0/Plugin3000.dll plugins/functions/
```

The plugin will be automatically loaded (within 10 seconds if hot-reload is enabled).

## Hot-Reload

When `EnableHotReload` is set to `true` in appsettings.json:

- **Add plugin**: Copy new DLL to `plugins/functions/` → Automatically detected and loaded
- **Remove plugin**: Delete DLL from folder → Automatically unloaded
- **Modify plugin**: Replace DLL → Automatically reloaded

## License

MIT