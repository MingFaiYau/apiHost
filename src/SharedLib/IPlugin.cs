using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedLib.Plugin.Abstractions;

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
public class PluginRoute
{
    public string Path { get; init; }
    public string Method { get; init; }  // HTTP method as string: "GET", "POST", "PUT", "DELETE"
    public Type? RequestType { get; init; }
    public Type? ResponseType { get; init; }

    public PluginRoute(string path, string method, Type? requestType = null, Type? responseType = null)
    {
        Path = path;
        Method = method;
        RequestType = requestType;
        ResponseType = responseType;
    }
}