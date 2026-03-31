# Plugin-Based Web API Host Proposal

## 1. Overview / 概述

### 1.1 Project Vision / 項目願景

This proposal describes a plugin-based web API hosting framework built on .NET 9+, designed to provide a flexible and extensible foundation for hosting multiple web services with dynamic plugin loading capabilities and hot-reload support.

本提案描述了一個基於 .NET 9+ 構建的插件式 Web API 托管框架，旨在提供一個靈活且可擴展的基礎設施，用於托管具有動態插件加載功能和熱重載支持的多個 Web 服務。

### 1.2 Core Features / 核心功能

- **Dynamic Plugin Loading** - Load plugins from `plugins/functions/` folder at runtime
- **Hot-Reload Support** - Add, remove, or modify plugins without restarting the host
- **Generic CRUD Service** - Built-in support for generic Create, Read, Update, Delete operations
- **Custom Business Services** - Support for additional custom web services beyond CRUD
- **Schema Discovery Service** - Built-in service providing schema information for all registered plugins

---

## 2. Architecture Design / 架構設計

### 2.1 High-Level Architecture / 高層架構

```
┌─────────────────────────────────────────────────────────────────┐
│                      Web API Host                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Core Services                           │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐     │   │
│  │  │   Router     │ │  Plugin      │ │   Schema    │     │   │
│  │  │   Manager    │ │  Loader      │ │  Service    │     │   │
│  │  │              │ │  (Hot-Reload)│ │             │     │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Shared Library                          │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐           │   │
│  │  │Plugin  │ │Schema  │ │ Config │ │  IPlugin    │           │   │
│  │  │Abstraction│ │        │ │        │ │Interfaces  │           │   │
│  │  └────────┘ └────────┘ └────────┘ └────────┘           │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Plugin Folders                         │   │
│  │  plugins/functions/Plugin1000.dll                       │   │
│  │  plugins/functions/Plugin2000.dll                       │   │
│  │  plugins/functions/Plugin3000.dll                       │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Component Descriptions / 組件說明

| Component | Description | 功能說明 |
|-----------|-------------|---------|
| **Plugin Router** | Routes incoming requests to appropriate plugins dynamically | 動態將傳入請求路由到相應的插件 |
| **Plugin Loader** | Dynamically loads, manages, and hot-reloads plugin assemblies | 動態加載、管理和熱重載插件程序集 |
| **Schema Service** | Provides schema information for all registered plugins | 為所有已註冊的插件提供 schema 信息 |
| **Shared Library** | Common interfaces (IPlugin, ICrudPlugin, ICustomPlugin) | 通用介面（IPlugin、ICrudPlugin、ICustomPlugin） |

---

## 3. Folder Structure / 資料夾結構

### 3.1 Project Organization / 項目組織

```
project-root/
├── src/
│   ├── ApiHost/                    # Main API Host project
│   │   ├── Services/                # Core services
│   │   │   ├── PluginLoader.cs     # Plugin loader with hot-reload
│   │   │   ├── PluginRouter.cs     # Dynamic request router
│   │   │   └── SchemaServiceImpl.cs
│   │   └── appsettings.json
│   ├── SharedLib/                  # Shared interfaces & models
│   │   ├── IPlugin.cs              # Base plugin interface
│   │   ├── ICrudPlugin.cs          # CRUD plugin interface
│   │   ├── ICustomPlugin.cs        # Custom plugin interface
│   │   ├── Config/                 # Configuration models
│   │   └── Schema/                 # Schema definitions
│   └── Plugins/                    # Plugin projects
│       ├── PluginBase/             # Base classes for plugins
│       ├── Plugin1000/             # Example: Product CRUD
│       └── Plugin2000/             # Example: Custom Report
├── plugins/
│   └── functions/                  # Runtime plugin storage
│       ├── Plugin1000.dll
│       ├── Plugin2000.dll
│       └── *.dll                   # Add more plugins here
└── README.md
```

### 3.2 Plugin Location / 插件位置

All plugin DLLs are placed directly in `plugins/functions/` folder:

```
plugins/functions/
├── Plugin1000.dll    # Function ID 1000
├── Plugin2000.dll    # Function ID 2000
└── Plugin3000.dll    # Function ID 3000
```

---

## 4. Plugin Interface Specification / 插件介面規範

### 4.1 Core Plugin Interface / 核心插件介面

All plugins must implement the `IPlugin` interface:

```csharp
namespace SharedLib.Plugin.Abstractions
{
    public interface IPlugin
    {
        int FunctionId { get; }
        string Name { get; }
        string Version { get; }
        Task InitializeAsync(IServiceProvider serviceProvider);
        IReadOnlyList<PluginRoute> GetRoutes();
    }

    public class PluginRoute
    {
        public string Path { get; init; }
        public string Method { get; init; }  // "GET", "POST", "PUT", "DELETE"
        public Type? RequestType { get; init; }
        public Type? ResponseType { get; init; }
    }
}
```

### 4.2 CRUD Service Interface / CRUD 服務介面

For generic CRUD functionality, plugins inherit from `CrudPluginBase`:

```csharp
namespace PluginBase
{
    public abstract class CrudPluginBase<TEntity> : PluginBase, ICrudPlugin<TEntity> where TEntity : class
    {
        public Type EntityType => typeof(TEntity);

        public abstract Task<IReadOnlyList<TEntity>> GetAllAsync();
        public abstract Task<TEntity?> GetByIdAsync(int id);
        public abstract Task<TEntity> CreateAsync(TEntity entity);
        public abstract Task<TEntity> UpdateAsync(TEntity entity);
        public abstract Task<bool> DeleteAsync(int id);

        protected SchemaInfo BuildSchema(string name, string description);
        protected void BuildSchemaProperties();
        protected void RegisterSchema(SchemaInfo schema);
    }
}
```

### 4.3 Custom Service Interface / 自定義服務介面

For non-CRUD custom services, plugins inherit from `CustomPluginBase`:

```csharp
namespace PluginBase
{
    public abstract class CustomPluginBase : PluginBase, ICustomPlugin
    {
        public abstract IReadOnlyDictionary<string, RequestHandler> Handlers { get; }

        protected void RegisterCustomSchema(string name, string description);
    }

    public delegate Task RequestHandler(object context);
}
```

---

## 5. Shared Library Components / 共用庫組件

### 5.1 Schema Attributes / Schema 屬性

```csharp
namespace SharedLib.Schema.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SchemaAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; set; }
        public bool IsCrudEnabled { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SchemaPropertyAttribute : Attribute
    {
        public string Description { get; set; }
        public bool IsRequired { get; set; } = false;
        public int MaxLength { get; set; }
    }
}
```

### 5.2 Schema Models / Schema 模型

```csharp
namespace SharedLib.Schema.Models
{
    public record SchemaInfo(
        int FunctionId,
        string Name,
        string Description,
        IReadOnlyList<SchemaPropertyInfo> Properties,
        string ServiceType); // "Crud" or "Custom"

    public record SchemaPropertyInfo(
        string Name,
        string TypeName,
        string Description,
        bool IsRequired,
        int? MaxLength);
}
```

### 5.3 Configuration / 配置

```csharp
namespace SharedLib.Config
{
    public class PluginHostOptions
    {
        public string PluginsPath { get; set; } = "plugins";
        public bool EnableHotReload { get; set; } = false;
        public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(1);
        public string BaseUrl { get; set; } = "http://localhost:5000";
    }

    public class DatabaseOptions
    {
        public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
        public string ConnectionString { get; set; } = "Data Source=pluginhost.db";
    }

    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql,
        MySql,
        Sqlite
    }
}
```

---

## 6. Core Services Implementation / 核心服務實現

### 6.1 Plugin Loader with Hot-Reload / 帶熱重載的插件加載器

```csharp
namespace ApiHost.Services
{
    public class PluginLoader : IPluginLoader, IDisposable
    {
        private readonly Dictionary<int, IPlugin> _plugins = new();
        private readonly ConcurrentDictionary<string, FileInfo> _loadedFiles = new();
        private readonly ConcurrentDictionary<int, string> _pluginToFileMap = new();
        private readonly Timer _scanTimer;

        public IReadOnlyDictionary<int, IPlugin> Plugins => _plugins;
        public event EventHandler<PluginsChangedEventArgs>? PluginsChanged;

        public async Task LoadAndInitializePluginsAsync(IServiceProvider serviceProvider)
        {
            await LoadPluginsFromFolderAsync();
            if (_options.EnableHotReload)
            {
                _scanTimer.Start();
            }
        }

        private async Task ScanForChangesAsync()
        {
            // Detect added/removed/modified DLLs
            // Raise PluginsChanged event for router to update
        }
    }
}
```

### 6.2 Dynamic Router / 動態路由管理器

```csharp
namespace ApiHost.Services
{
    public class PluginRouter
    {
        private readonly IPluginLoader _pluginLoader;

        public void RegisterRoutes(WebApplication app)
        {
            // Use middleware for dynamic routing
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.ToString();
                foreach (var plugin in _pluginLoader.Plugins.Values)
                {
                    var routes = plugin.GetRoutes();
                    var matchingRoute = routes.FirstOrDefault(r => MatchPath(path, r.Path));
                    if (matchingRoute != null)
                    {
                        await HandleRequest(context, plugin, matchingRoute);
                        return;
                    }
                }
                await next();
            });
        }
    }
}
```

---

## 7. Service Endpoints / 服務端點

### 7.1 Schema Discovery Endpoints / Schema 發現端點

| Method | Path | Description |
|--------|------|-------------|
| GET | `/schema` | Get all registered schemas |
| GET | `/schema/{functionId}` | Get schema for specific function |

### 7.2 Plugin Endpoints / 插件端點

Endpoints are defined by each plugin via `GetRoutes()` method. Examples:

**Product Service (Function ID: 1000)**

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/products` | Get all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |

**Order Report Service (Function ID: 2000)**

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/reports/orders/daily` | Get daily report |
| GET | `/api/reports/orders/summary` | Get summary report |

---

## 8. Example Plugin Implementation / 插件實現示例

### 8.1 CRUD Plugin Example / CRUD 插件示例

```csharp
// Plugin1000/ProductPlugin.cs
using PluginBase;
using SharedLib.Schema.Attributes;

namespace Plugin1000;

[Schema("Product", Description = "Product management")]
public class Product
{
    public int Id { get; set; }
    [SchemaProperty(Description = "Product name", IsRequired = true, MaxLength = 100)]
    public string Name { get; set; } = "";
    [SchemaProperty(Description = "Product price")]
    public decimal Price { get; set; }
}

public class ProductPlugin : CrudPluginBase<Product>
{
    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Product A", Price = 100.00m },
        new Product { Id = 2, Name = "Product B", Price = 200.00m }
    };

    public override int FunctionId => 1000;
    public override string Name => "Product Service";
    public override string Version => "1.0.0";

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        return base.InitializeAsync(serviceProvider).ContinueWith(_ =>
        {
            BuildSchemaProperties();
            var schema = BuildSchema("Product", "Product CRUD");
            RegisterSchema(schema);
        });
    }

    public override IReadOnlyList<PluginRoute> GetRoutes() => new List<PluginRoute>
    {
        new("/api/products", "GET"),
        new("/api/products/{id}", "GET"),
        new("/api/products", "POST"),
        new("/api/products/{id}", "PUT"),
        new("/api/products/{id}", "DELETE")
    };

    public override Task<IReadOnlyList<Product>> GetAllAsync() 
        => Task.FromResult<IReadOnlyList<Product>>(_products.ToList());

    public override Task<Product?> GetByIdAsync(int id) 
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    // Implement CreateAsync, UpdateAsync, DeleteAsync...
}
```

### 8.2 Custom Plugin Example / 自定義插件示例

```csharp
// Plugin2000/OrderReportPlugin.cs
using PluginBase;

namespace Plugin2000;

public class OrderReportPlugin : CustomPluginBase
{
    private readonly Dictionary<string, RequestHandler> _handlers = new();

    public override int FunctionId => 2000;
    public override string Name => "Order Report Service";
    public override string Version => "1.0.0";

    public override IReadOnlyDictionary<string, RequestHandler> Handlers => _handlers;

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        return base.InitializeAsync(serviceProvider).ContinueWith(_ =>
        {
            RegisterCustomSchema("OrderReport", "Order reporting service");
            _handlers["/api/reports/orders/daily"] = HandleDailyReport;
        });
    }

    public override IReadOnlyList<PluginRoute> GetRoutes() => new List<PluginRoute>
    {
        new("/api/reports/orders/daily", "GET"),
        new("/api/reports/orders/summary", "GET")
    };

    private Task HandleDailyReport(object context)
    {
        if (context is HttpContext httpContext)
        {
            var report = new { Date = DateTime.Now, TotalOrders = 150 };
            return httpContext.Response.WriteAsJsonAsync(report);
        }
        return Task.CompletedTask;
    }
}
```

---

## 9. Configuration / 配置

### appsettings.json

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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 10. Hot-Reload Feature / 熱重載功能

### How It Works / 工作原理

1. **Scan Interval**: Timer scans `plugins/functions/` folder every `ScanInterval` (default: 10 seconds)
2. **Detect Changes**: Compare current DLL files with previously loaded files
3. **Add Plugin**: New DLL detected → Load and initialize → Register routes
4. **Remove Plugin**: DLL removed → Unload plugin → Remove routes
5. **Modify Plugin**: DLL changed → Unload old → Load new → Update routes

### Configuration / 配置

```json
"EnableHotReload": true,    // Enable/disable hot-reload
"ScanInterval": "00:00:10"  // Scan every 10 seconds
```

---

## 11. Advantages of This Architecture / 此架構的優勢

### 11.1 Scalability / 可擴展性
- Add new services by simply copying DLL to plugins/functions/
- No modification to core host required
- Function IDs can be allocated to different teams/domains

### 11.2 Maintainability / 可維助性
- PluginBase provides consistent patterns across all plugins
- Clear separation of concerns
- Independent plugin development and deployment

### 11.3 Flexibility / 靈活性
- Support both generic CRUD and custom business logic
- Dynamic loading with hot-reload (no restart needed)
- Schema discovery for API documentation

### 11.4 Hot-Reload / 熱重載
- Add plugins at runtime without stopping the server
- Remove plugins at runtime
- Modify and reload plugins at runtime

---

## 12. Implementation Status / 實現狀態

### Completed / 已完成

- [x] Create solution structure
- [x] Implement SharedLib components
- [x] Build Plugin Loader service with hot-reload
- [x] Define IPlugin interface
- [x] Implement CRUD and Custom plugin base classes
- [x] Create Dynamic Router
- [x] Implement Web API host
- [x] Add Schema Service endpoints
- [x] Configure dependency injection
- [x] Create sample CRUD plugin (Plugin1000)
- [x] Create sample custom plugin (Plugin2000)
- [x] Test end-to-end functionality
- [x] Implement hot-reload (add/remove/modify)

---

## 13. Summary / 總結

This proposal outlines a comprehensive architecture for a plugin-based web API host that:

1. **Provides dynamic plugin loading** from `plugins/functions/` folder
2. **Supports hot-reload** - add, remove, modify plugins without restart
3. **Supports both generic CRUD and custom services** through specialized base classes
4. **Includes schema discovery** for all registered plugins
5. **Built on modern .NET 9+** with full dependency injection support

The architecture is designed to be maintainable, scalable, and flexible for future expansion.

---

本提案概述了一個全面的插件式 Web API 主機架構，其特點包括：

1. **從 `plugins/functions/` 資料夾動態加載插件**
2. **支持熱重載** - 無需重啟即可添加、移除、修改插件
3. **通過專門的基類同時支持通用 CRUD 和自定義服務**
4. **為所有已註冊的插件提供 schema 發現功能**
5. **基於現代 .NET 9+**，具有完整的依賴注入支持

該架構旨在實現可維護性、可擴展性和未來擴展的靈活性。