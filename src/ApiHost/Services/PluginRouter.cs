using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedLib.Plugin.Abstractions;

namespace ApiHost.Services;

/// <summary>
/// Service for routing requests to plugins / 用於將請求路由到插件的服務
/// </summary>
public class PluginRouter
{
    private readonly IPluginLoader _pluginLoader;
    private readonly ILogger<PluginRouter> _logger;

    public PluginRouter(IPluginLoader pluginLoader, ILogger<PluginRouter> logger)
    {
        _pluginLoader = pluginLoader;
        _logger = logger;
    }

    /// <summary>
    /// Register all plugin routes using a catch-all middleware / 使用catch-all中介軟體註冊所有插件路由
    /// </summary>
    public void RegisterRoutes(WebApplication app)
    {
        // Use a catch-all route that forwards to plugins dynamically
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.ToString();

            // Try to find a matching plugin route
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

            // No matching route found, continue to next middleware
            await next();
        });

        _logger.LogInformation("Registered dynamic routing for {Count} plugins", _pluginLoader.Plugins.Count);
    }

    private bool MatchPath(string requestPath, string routePath)
    {
        // Simple path matching - convert route path to regex pattern
        // Handle: /api/products, /api/products/{id}
        if (routePath.EndsWith("{id}"))
        {
            var basePath = routePath.Substring(0, routePath.Length - 4); // Remove {id}
            return requestPath.StartsWith(basePath) && requestPath.Length > basePath.Length;
        }
        return requestPath == routePath;
    }

    private async Task HandleRequest(HttpContext context, IPlugin plugin, PluginRoute route)
    {
        try
        {
            // Check if it's a CRUD plugin
            var crudInterface = plugin.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICrudPlugin<>));

            if (crudInterface != null)
            {
                await HandleCrudRequest(context, plugin, route);
            }
            else if (plugin is ICustomPlugin customPlugin)
            {
                await HandleCustomRequest(context, customPlugin, route);
            }
            else
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Unknown plugin type");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request for plugin {PluginName}", plugin.Name);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Error: {ex.Message}");
        }
    }

    private async Task HandleCrudRequest(HttpContext context, IPlugin plugin, PluginRoute route)
    {
        var pluginType = plugin.GetType();
        var getAllAsync = pluginType.GetMethod("GetAllAsync");
        var getByIdAsync = pluginType.GetMethod("GetByIdAsync");
        var deleteAsync = pluginType.GetMethod("DeleteAsync");

        var path = context.Request.Path.ToString();
        var segments = path.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToList();
        var idSegment = segments.LastOrDefault();

        switch (route.Method.ToUpperInvariant())
        {
            case "GET":
                if (int.TryParse(idSegment, out var id) && getByIdAsync != null)
                {
                    var task = (Task)getByIdAsync.Invoke(plugin, new object[] { id })!;
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    var entity = resultProperty?.GetValue(task);
                    if (entity == null)
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                    await context.Response.WriteAsJsonAsync(entity);
                }
                else if (getAllAsync != null)
                {
                    var task = (Task)getAllAsync.Invoke(plugin, null)!;
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    var list = resultProperty?.GetValue(task);
                    await context.Response.WriteAsJsonAsync(list);
                }
                break;

            case "DELETE":
                if (int.TryParse(idSegment, out var deleteId) && deleteAsync != null)
                {
                    var task = (Task)deleteAsync.Invoke(plugin, new object[] { deleteId })!;
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    var success = (bool)(resultProperty?.GetValue(task) ?? false);
                    await context.Response.WriteAsJsonAsync(new { success });
                }
                break;
        }
    }

    private async Task HandleCustomRequest(HttpContext context, ICustomPlugin plugin, PluginRoute route)
    {
        if (plugin.Handlers.TryGetValue(route.Path, out var handler))
        {
            await handler(context);
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"Handler not found for path: {route.Path}");
        }
    }
}