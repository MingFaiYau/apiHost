using System.Reflection;
using ApiHost.Services;
using SharedLib.Config;
using SharedLib.Plugin.Abstractions;
using SharedLib.Schema.Models;
using SharedLib.Schema.Services;

// Create builder
var builder = WebApplication.CreateBuilder(args);

// Load configuration
var pluginOptions = builder.Configuration.GetSection("PluginHost").Get<PluginHostOptions>() ?? new PluginHostOptions();
var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

// Register configuration as singleton
builder.Services.AddSingleton(pluginOptions);
builder.Services.AddSingleton(dbOptions);

// Register core services
builder.Services.AddSingleton<ISchemaService, SchemaServiceImpl>();
builder.Services.AddSingleton<IPluginLoader, PluginLoader>();
builder.Services.AddSingleton<PluginRouter>();

// Add services
builder.Services.AddOpenApi();

var app = builder.Build();

// Get services
var pluginLoader = app.Services.GetRequiredService<IPluginLoader>() as PluginLoader;
var pluginRouter = app.Services.GetRequiredService<PluginRouter>();

// Load and initialize plugins
await pluginLoader.LoadAndInitializePluginsAsync(app.Services);

// Register plugin routes
pluginRouter.RegisterRoutes(app);

// Subscribe to plugin changes for hot-reload
if (pluginLoader != null && pluginOptions.EnableHotReload)
{
    pluginLoader.PluginsChanged += async (sender, args) =>
    {
        Console.WriteLine($"[Hot-Reload] Plugins changed - Added: {args.Added.Count}, Removed: {args.Removed.Count}");

        // Re-register all routes
        pluginRouter.RegisterRoutes(app);
    };
}

// Map schema endpoints
app.MapGet("/schema", () =>
{
    var schemaService = app.Services.GetRequiredService<ISchemaService>();
    return schemaService.GetAllSchemas();
});

app.MapGet("/schema/{functionId:int}", (int functionId) =>
{
    var schemaService = app.Services.GetRequiredService<ISchemaService>();
    var schema = schemaService.GetSchema(functionId);
    if (schema == null)
    {
        return Results.NotFound(new { error = $"Schema not found for function ID: {functionId}" });
    }
    return Results.Ok(schema);
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

Console.WriteLine($"Plugin API Host started at: {pluginOptions.BaseUrl}");
Console.WriteLine($"Plugins path: {pluginOptions.PluginsPath}");
Console.WriteLine($"Hot-reload enabled: {pluginOptions.EnableHotReload}");

app.Run(pluginOptions.BaseUrl);