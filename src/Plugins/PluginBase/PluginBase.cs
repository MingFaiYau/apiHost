using System.Reflection;
using Microsoft.Extensions.Logging;
using SharedLib.Plugin.Abstractions;
using SharedLib.Schema.Models;
using SharedLib.Schema.Services;

namespace PluginBase;

/// <summary>
/// Base class for all plugins / 所有插件的基類
/// </summary>
public abstract class PluginBase : IPlugin
{
    public abstract int FunctionId { get; }
    public abstract string Name { get; }
    public abstract string Version { get; }

    protected ILogger? Logger { get; private set; }
    protected IServiceProvider? ServiceProvider { get; private set; }
    protected ISchemaService? SchemaService { get; private set; }

    public virtual Task InitializeAsync(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Logger = serviceProvider.GetService(typeof(ILogger<PluginBase>)) as ILogger<PluginBase>;
        SchemaService = serviceProvider.GetService(typeof(ISchemaService)) as ISchemaService;
        return Task.CompletedTask;
    }

    public abstract IReadOnlyList<PluginRoute> GetRoutes();

    protected void RegisterSchema(SchemaInfo schema)
    {
        SchemaService?.RegisterSchema(schema);
    }
}

/// <summary>
/// Base class for CRUD plugins / CRUD 插件的基類
/// </summary>
public abstract class CrudPluginBase<TEntity> : PluginBase, ICrudPlugin<TEntity> where TEntity : class
{
    private readonly List<SchemaPropertyInfo> _properties = new();

    public Type EntityType => typeof(TEntity);

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        return base.InitializeAsync(serviceProvider);
    }

    protected void BuildSchemaProperties()
    {
        var properties = typeof(TEntity).GetProperties();
        foreach (var prop in properties)
        {
            var schemaAttr = prop.GetCustomAttribute<SharedLib.Schema.Attributes.SchemaPropertyAttribute>();
            _properties.Add(new SchemaPropertyInfo(
                prop.Name,
                prop.PropertyType.Name,
                schemaAttr?.Description ?? "",
                schemaAttr?.IsRequired ?? false,
                schemaAttr?.MaxLength));
        }
    }

    public abstract Task<IReadOnlyList<TEntity>> GetAllAsync();
    public abstract Task<TEntity?> GetByIdAsync(int id);
    public abstract Task<TEntity> CreateAsync(TEntity entity);
    public abstract Task<TEntity> UpdateAsync(TEntity entity);
    public abstract Task<bool> DeleteAsync(int id);

    protected SchemaInfo BuildSchema(string name, string description)
    {
        var schemaAttr = typeof(TEntity).GetCustomAttribute<SharedLib.Schema.Attributes.SchemaAttribute>();
        var schemaName = schemaAttr?.Name ?? name;
        var schemaDesc = schemaAttr?.Description ?? description;

        return new SchemaInfo(FunctionId, schemaName, schemaDesc, _properties, "Crud");
    }
}

/// <summary>
/// Base class for custom plugins / 自定義插件的基類
/// </summary>
public abstract class CustomPluginBase : PluginBase, ICustomPlugin
{
    public abstract IReadOnlyDictionary<string, RequestHandler> Handlers { get; }

    protected void RegisterCustomSchema(string name, string description)
    {
        var schema = new SchemaInfo(FunctionId, name, description, Array.Empty<SchemaPropertyInfo>(), "Custom");
        RegisterSchema(schema);
    }
}