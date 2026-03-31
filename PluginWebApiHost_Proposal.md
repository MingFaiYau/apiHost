# Plugin-Based Web API Host Proposal

## 1. Overview / 概述

### 1.1 Project Vision / 項目願景

This proposal describes a plugin-based web API hosting framework built on .NET 8+, designed to provide a flexible and extensible foundation for hosting multiple web services with dynamic plugin loading capabilities.

本提案描述了一個基於 .NET 8+ 構建的插件式 Web API 托管框架，旨在提供一個靈活且可擴展的基礎設施，用於托管具有動態插件加載功能的多個 Web 服務。

### 1.2 Core Features / 核心功能

- **Generic CRUD Service** - Built-in support for generic Create, Read, Update, Delete operations
- **Custom Business Services** - Support for additional custom web services beyond CRUD
- **Schema Discovery Service** - A dedicated service providing schema information for all other services
- **Dynamic Plugin Loading** - Plugins identified by function ID and loaded from structured folders

---

## 2. Architecture Design / 架構設計

### 2.1 High-Level Architecture / 高層架構

```
┌─────────────────────────────────────────────────────────────────┐
│                      Web API Host                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Core Services                           │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐     │   │
│  │  │   Router     │ │  Plugin      │ │   Schema     │     │   │
│  │  │   Manager    │ │  Loader      │ │   Service    │     │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Shared Library (Common)                     │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ │   │
│  │  │ DB     │ │ Schema │ │ Config │ │Logging │ │Utility│ │   │
│  │  │ Layer  │ │        │ │        │ │        │ │       │ │   │
│  │  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘ │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Plugin Folders                         │   │
│  │  plugins\1\1000\    plugins\2\2000\    plugins\3\3000\   │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Component Descriptions / 組件說明

| Component | Description | 功能說明 |
|-----------|-------------|---------|
| **Router Manager** | Routes incoming requests to appropriate plugins based on function ID | 根據功能 ID 將傳入請求路由到相應的插件 |
| **Plugin Loader** | Dynamically loads and manages plugin assemblies | 動態加載和管理插件程序集 |
| **Schema Service** | Provides schema information for all registered services | 為所有已註冊的服務提供 schema 信息 |
| **Shared Library** | Common utilities, DB layer, logging, configuration | 通用工具、數據庫層、日誌、配置 |

---

## 3. Folder Structure / 資料夾結構

### 3.1 Plugin Directory Organization / 插件目錄組織

```
project-root/
├── src/
│   ├── ApiHost/                    # Main API Host project
│   ├── SharedLib/                  # Shared library (Common)
│   │   ├── SharedLib.DB/            # Database layer
│   │   ├── SharedLib.Schema/       # Schema definitions
│   │   ├── SharedLib.Config/       # Configuration
│   │   ├── SharedLib.Logging/      # Logging infrastructure
│   │   └── SharedLib.Utils/         # Utility functions
│   └── Plugins/                    # Plugin projects (example)
│       ├── Plugins.sln
│       ├── Plugin1000/             # Function ID 1000
│       ├── Plugin2000/             # Function ID 2000
│       └── Plugin3000/             # Function ID 3000
└── plugins/                        # Runtime plugin storage (deployed)
    ├── 1/                          # First digit: 1
    │   └── 1000/                   # Function ID 1000
    │       └── Plugin1000.dll
    ├── 2/                          # First digit: 2
    │   └── 2000/                   # Function ID 2000
    │       └── Plugin2000.dll
    └── 3/                          # First digit: 3
        └── 3000/                   # Function ID 3000
            └── Plugin3000.dll
```

### 3.2 Plugin Folder Naming Convention / 插件資料夾命名規範

- Function ID `1XXX` → `plugins/1/1XXX/`
- Function ID `2XXX` → `plugins/2/2XXX/`
- Function ID `9XXX` → `plugins/9/9XXX/`

This pattern provides:
- Easy manual navigation to specific functions
- Logical grouping by function category (1xxx, 2xxx, etc.)
- Prevents folder bloat in root directory

---

## 4. Plugin Interface Specification / 插件介面規範

### 4.1 Core Plugin Interface / 核心插件介面

All plugins must implement the `IPlugin` interface:

```csharp
namespace SharedLib.Plugin.Abstractions
{
    /// <summary>
    /// Base interface for all plugins / 所有插件的基礎介面
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Unique function ID / 唯一的功能 ID
        /// </summary>
        int FunctionId { get; }

        /// <summary>
        /// Plugin name / 插件名稱
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plugin version / 插件版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Initialize the plugin / 初始化插件
        /// </summary>
        Task InitializeAsync(IServiceProvider serviceProvider);

        /// <summary>
        /// Get the HTTP routes handled by this plugin / 獲取此插件處理的 HTTP 路由
        /// </summary>
        IReadOnlyList<PluginRoute> GetRoutes();
    }

    /// <summary>
    /// Represents a route exposed by a plugin / 表示插件公開的路由
    /// </summary>
    public record PluginRoute(
        string Path,
        HttpMethod Method,
        Type RequestType,
        Type ResponseType);
}
```

### 4.2 CRUD Service Interface / CRUD 服務介面

For generic CRUD functionality, plugins implement `ICrudPlugin`:

```csharp
namespace SharedLib.Plugin.Abstractions
{
    /// <summary>
    /// Interface for generic CRUD operations / 通用 CRUD 操作的介面
    /// </summary>
    public interface ICrudPlugin : IPlugin
    {
        /// <summary>
        /// Entity type for this CRUD service / 此 CRUD 服務的實體類型
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Repository for data access / 數據訪問的儲存庫
        /// </summary>
        IRepository<Entity> Repository { get; }
    }

    /// <summary>
    /// Generic CRUD plugin with type parameter / 帶類型參數的通用 CRUD 插件
    /// </summary>
    public interface ICrudPlugin<TEntity> : ICrudPlugin where TEntity : class
    {
        IRepository<TEntity> Repository { get; }
    }
}
```

### 4.3 Custom Service Interface / 自定義服務介面

For non-CRUD custom services, plugins implement `ICustomPlugin`:

```csharp
namespace SharedLib.Plugin.Abstractions
{
    /// <summary>
    /// Interface for custom business logic services / 自定義業務邏輯服務的介面
    /// </summary>
    public interface ICustomPlugin : IPlugin
    {
        /// <summary>
        /// Custom service handlers / 自定義服務處理程序
        /// </summary>
        IReadOnlyDictionary<string, RequestHandler> Handlers { get; }
    }

    /// <summary>
    /// Delegate for handling custom requests / 用於處理自定義請求的委託
    /// </summary>
    public delegate Task<IResult> RequestHandler(HttpContext context);
}
```

---

## 5. Shared Library Components / 共用庫組件

### 5.1 SharedLib.DB (Database Layer) / SharedLib.DB（數據庫層）

```csharp
// SharedLib.DB/Repositories/IRepository.cs
namespace SharedLib.DB.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
        IRepository<T> GetRepository<T>() where T : class;
    }
}
```

### 5.2 SharedLib.Schema (Schema Definitions) / SharedLib.Schema（Schema 定義）

```csharp
// SharedLib.Schema/Attributes/SchemaAttribute.cs
namespace SharedLib.Schema.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SchemaAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsCrudEnabled { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SchemaPropertyAttribute : Attribute
    {
        public string Description { get; }
        public bool IsRequired { get; }
        public int? MaxLength { get; }
    }
}

// SharedLib.Schema/ISchemaService.cs
namespace SharedLib.Schema.Services
{
    public interface ISchemaService
    {
        SchemaInfo GetSchema(int functionId);
        IReadOnlyList<SchemaInfo> GetAllSchemas();
        SchemaInfo RegisterSchema(SchemaInfo schema);
    }

    public record SchemaInfo(
        int FunctionId,
        string Name,
        string Description,
        IReadOnlyList<PropertyInfo> Properties,
        string ServiceType); // "Crud" or "Custom"
}
```

### 5.3 SharedLib.Config (Configuration) / SharedLib.Config（配置）

```csharp
// SharedLib.Config/ConfigurationExtensions.cs
namespace SharedLib.Config
{
    public class PluginHostOptions
    {
        public string PluginsPath { get; set; } = "plugins";
        public bool EnableHotReload { get; set; } = false;
        public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(1);
    }

    public class DatabaseOptions
    {
        public string ConnectionString { get; set; } = "";
        public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
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

### 5.4 SharedLib.Logging (Logging Infrastructure) / SharedLib.Logging（日誌基礎設施）

```csharp
// SharedLib.Logging/IPluginLogger.cs
namespace SharedLib.Logging
{
    public interface IPluginLogger
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, Exception? ex = null, params object[] args);
        void LogDebug(string message, params object[] args);
    }

    public interface ILogFormatter
    {
        string Format(LogLevel level, string message, Exception? ex);
    }
}
```

### 5.5 SharedLib.Utils (Utility Functions) / SharedLib.Utils（工具函數）

```csharp
// SharedLib.Utils/CommonUtilities.cs
namespace SharedLib.Utils
{
    public static class FunctionIdHelper
    {
        public static string GetPluginPath(int functionId, string basePath)
            => Path.Combine(basePath, functionId.ToString()[0].ToString(), functionId.ToString());
    }

    public static class AssemblyHelper
    {
        public static IEnumerable<Type> GetDerivedTypes<TBase>(Assembly assembly);
        public static IPlugin? CreatePluginInstance(Type pluginType);
    }
}
```

---

## 6. Core Services Implementation / 核心服務實現

### 6.1 Plugin Loader / 插件加載器

```csharp
// ApiHost/Services/PluginLoader.cs
namespace ApiHost.Services
{
    public class PluginLoader : IPluginLoader
    {
        private readonly PluginHostOptions _options;
        private readonly ILogger<PluginLoader> _logger;
        private readonly Dictionary<int, IPlugin> _loadedPlugins = new();

        public async Task<IReadOnlyDictionary<int, IPlugin>> LoadPluginsAsync()
        {
            var directories = Directory.GetDirectories(_options.PluginsPath, "*", SearchOption.AllDirectories);

            foreach (var dir in directories)
            {
                var dllFiles = Directory.GetFiles(dir, "*.dll");
                foreach (var dll in dllFiles)
                {
                    await LoadPluginFromDllAsync(dll);
                }
            }

            return _loadedPlugins;
        }

        private async Task LoadPluginFromDllAsync(string dllPath)
        {
            var assembly = Assembly.LoadFrom(dllPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

            foreach (var type in pluginTypes)
            {
                var plugin = (IPlugin)Activator.CreateInstance(type)!;
                _loadedPlugins[plugin.FunctionId] = plugin;
            }
        }
    }
}
```

### 6.2 Router Manager / 路由管理器

```csharp
// ApiHost/Services/RouterManager.cs
namespace ApiHost.Services
{
    public class RouterManager : IRouterManager
    {
        private readonly Dictionary<int, IPlugin> _plugins;
        private readonly ISchemaService _schemaService;

        public void RegisterRoutes(WebApplication app)
        {
            foreach (var plugin in _plugins.Values)
            {
                var routes = plugin.GetRoutes();
                foreach (var route in routes)
                {
                    app.MapMethods(route.Path, new[] { route.Method.ToString() },
                        async (HttpContext context) => await HandleRequest(plugin, context));
                }
            }

            // Register schema service endpoint
            app.MapGet("/schema", GetAllSchemas);
            app.MapGet("/schema/{functionId}", GetSchemaByFunctionId);
        }

        private async Task HandleRequest(IPlugin plugin, HttpContext context)
        {
            // Delegate to plugin's request handler
            // Use reflection to invoke appropriate handler based on route
        }
    }
}
```

### 6.3 Schema Service / Schema 服務

```csharp
// ApiHost/Services/SchemaServiceImpl.cs
namespace ApiHost.Services
{
    public class SchemaServiceImpl : ISchemaService
    {
        private readonly Dictionary<int, SchemaInfo> _schemas = new();

        public SchemaInfo GetSchema(int functionId)
        {
            if (!_schemas.TryGetValue(functionId, out var schema))
                throw new KeyNotFoundException($"Schema not found for function ID: {functionId}");
            return schema;
        }

        public IReadOnlyList<SchemaInfo> GetAllSchemas() => _schemas.Values.ToList();

        public SchemaInfo RegisterSchema(SchemaInfo schema)
        {
            _schemas[schema.FunctionId] = schema;
            return schema;
        }
    }
}
```

---

## 7. Service Endpoints / 服務端點

### 7.1 Generic CRUD Endpoints / 通用 CRUD 端點

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/{functionId}` | Get all entities |
| GET | `/api/{functionId}/{id}` | Get entity by ID |
| POST | `/api/{functionId}` | Create new entity |
| PUT | `/api/{functionId}/{id}` | Update entity |
| DELETE | `/api/{functionId}/{id}` | Delete entity |

### 7.2 Custom Service Endpoints / 自定義服務端點

Custom endpoints are defined per plugin via `GetRoutes()` method.

### 7.3 Schema Discovery Endpoints / Schema 發現端點

| Method | Path | Description |
|--------|------|-------------|
| GET | `/schema` | Get all registered schemas |
| GET | `/schema/{functionId}` | Get schema for specific function |

---

## 8. Example Plugin Implementation / 插件實現示例

### 8.1 CRUD Plugin Example / CRUD 插件示例

```csharp
// Plugins/Plugin1000/ProductPlugin.cs
using SharedLib.Plugin.Abstractions;
using SharedLib.DB.Repositories;
using SharedLib.Schema.Attributes;

namespace Plugins.Plugin1000
{
    [Schema(Name = "Product", Description = "Product management", IsCrudEnabled = true)]
    public class ProductPlugin : ICrudPlugin<Product>
    {
        public int FunctionId => 1000;
        public string Name => "Product Service";
        public string Version => "1.0.0";
        public Type EntityType => typeof(Product);

        public IRepository<Product> Repository { get; }

        public ProductPlugin(IRepository<Product> repository)
        {
            Repository = repository;
        }

        public Task InitializeAsync(IServiceProvider serviceProvider)
        {
            // Initialization logic
            return Task.CompletedTask;
        }

        public IReadOnlyList<PluginRoute> GetRoutes()
        {
            return new List<PluginRoute>
            {
                new("/api/products", HttpMethod.Get, typeof(void), typeof(List<Product>)),
                new("/api/products/{id}", HttpMethod.Get, typeof(int), typeof(Product)),
                new("/api/products", HttpMethod.Post, typeof(Product), typeof(Product)),
                new("/api/products/{id}", HttpMethod.Put, typeof(Product), typeof(Product)),
                new("/api/products/{id}", HttpMethod.Delete, typeof(void), typeof(bool))
            };
        }
    }

    public class Product
    {
        public int Id { get; set; }
        [SchemaProperty(Description = "Product name", IsRequired = true, MaxLength = 100)]
        public string Name { get; set; } = "";
        [SchemaProperty(Description = "Product price")]
        public decimal Price { get; set; }
    }
}
```

### 8.2 Custom Plugin Example / 自定義插件示例

```csharp
// Plugins/Plugin2000/OrderReportPlugin.cs
using SharedLib.Plugin.Abstractions;

namespace Plugins.Plugin2000
{
    public class OrderReportPlugin : ICustomPlugin
    {
        public int FunctionId => 2000;
        public string Name => "Order Report Service";
        public string Version => "1.0.0";

        public IReadOnlyDictionary<string, RequestHandler> Handlers => new Dictionary<string, RequestHandler>
        {
            ["/api/reports/orders/daily"] = HandleDailyReport,
            ["/api/reports/orders/summary"] = HandleSummaryReport
        };

        public Task InitializeAsync(IServiceProvider serviceProvider)
            => Task.CompletedTask;

        public IReadOnlyList<PluginRoute> GetRoutes()
        {
            return new List<PluginRoute>
            {
                new("/api/reports/orders/daily", HttpMethod.Get, typeof(void), typeof(byte[])),
                new("/api/reports/orders/summary", HttpMethod.Get, typeof(void), typeof(byte[]))
            };
        }

        private async Task<IResult> HandleDailyReport(HttpContext context)
        {
            // Custom logic for daily order report
            return Results.Ok(new { message = "Daily report generated" });
        }

        private async Task<IResult> HandleSummaryReport(HttpContext context)
        {
            // Custom logic for order summary
            return Results.Ok(new { message = "Summary report generated" });
        }
    }
}
```

---

## 9. Dependency Injection Configuration / 依賴注入配置

### 9.1 Host Program.cs / 主程序配置

```csharp
// ApiHost/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load configuration
var pluginOptions = builder.Configuration.GetSection("PluginHost").Get<PluginHostOptions>();
var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>();

// Register shared services
builder.Services.AddSingleton(pluginOptions);
builder.Services.AddSingleton(dbOptions);

// Register database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (dbOptions.Provider)
    {
        case DatabaseProvider.SqlServer:
            options.UseSqlServer(dbOptions.ConnectionString);
            break;
        case DatabaseProvider.PostgreSql:
            options.UseNpgsql(dbOptions.ConnectionString);
            break;
        // ... other providers
    }
});

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register core services
builder.Services.AddSingleton<ISchemaService, SchemaServiceImpl>();
builder.Services.AddSingleton<IPluginLoader, PluginLoader>();

// Add plugin services
builder.Services.AddSingleton<IPluginRegistry, PluginRegistry>();

var app = builder.Build();

// Load plugins
var pluginLoader = app.Services.GetRequiredService<IPluginLoader>();
var plugins = await pluginLoader.LoadPluginsAsync();

// Register routes
var routerManager = app.Services.GetRequiredService<IRouterManager>();
routerManager.RegisterRoutes(app);

app.Run();
```

---

## 10. Configuration File / 配置文件

### 10.1 appsettings.json

```json
{
  "PluginHost": {
    "PluginsPath": "plugins",
    "EnableHotReload": true,
    "ScanInterval": "00:01:00"
  },
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=PluginHostDb;Trusted_Connection=true;"
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

## 11. Advantages of This Architecture / 此架構的優勢

### 11.1 Scalability / 可擴展性

- Add new services by simply creating a new plugin folder and DLL
- No modification to core host required
- Function IDs can be allocated to different teams/domains

### 11.2 Maintainability / 可維護性

- Shared library ensures consistent patterns across all plugins
- Clear separation of concerns
- Independent plugin development and deployment

### 11.3 Flexibility / 靈活性

- Support both generic CRUD and custom business logic
- Dynamic loading without restart (optional)
- Schema discovery for API documentation

### 11.4 Security / 安全性

- Plugins run in isolated context
- Each plugin has its own folder preventing unauthorized access
- Centralized configuration management

---

## 12. Implementation Roadmap / 實現路線圖

### Phase 1: Core Infrastructure / 第一階段：核心基礎設施
- [ ] Create solution structure
- [ ] Implement SharedLib components
- [ ] Build Plugin Loader service

### Phase 2: Plugin System / 第二階段：插件系統
- [ ] Define IPlugin interface
- [ ] Implement CRUD and Custom plugin base classes
- [ ] Create Router Manager

### Phase 3: API Host / 第三階段：API 主機
- [ ] Implement Web API host
- [ ] Add Schema Service endpoints
- [ ] Configure dependency injection

### Phase 4: Example Plugins / 第四階段：示例插件
- [ ] Create sample CRUD plugin
- [ ] Create sample custom plugin
- [ ] Test end-to-end functionality

---

## 13. Summary / 總結

This proposal outlines a comprehensive architecture for a plugin-based web API host that:

1. **Provides dynamic plugin loading** based on function ID with structured folder organization
2. **Supports both generic CRUD and custom services** through specialized interfaces
3. **Includes schema discovery** for all registered services
4. **Leverages a shared library** for consistent DB, config, logging, and utilities
5. **Built on modern .NET 8+** with full dependency injection support

The architecture is designed to be maintainable, scalable, and flexible for future expansion.

---

本提案概述了一個全面的插件式 Web API 主機架構，其特點包括：

1. **基於功能 ID 的動態插件加載**，採用結構化資料夾組織
2. **同時支持通用 CRUD 和自定義服務**，通過專門的介面
3. **包含所有已註冊服務的 schema 發現功能**
4. **使用共用庫**確保一致的數據庫、配置、日誌和工具函數
5. **基於現代 .NET 8+**，具有完整的依賴注入支持

該架構旨在實現可維護性、可擴展性和未來擴展的靈活性。