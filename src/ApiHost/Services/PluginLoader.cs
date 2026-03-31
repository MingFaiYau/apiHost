using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLib.Config;
using SharedLib.Plugin.Abstractions;
using Timer = System.Timers.Timer;

namespace ApiHost.Services;

/// <summary>
/// Service for loading and managing plugins with hot-reload support / 帶熱重載支持的插件加載和管理服務
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Gets all loaded plugins / 獲取所有已加載的插件
    /// </summary>
    IReadOnlyDictionary<int, IPlugin> Plugins { get; }

    /// <summary>
    /// Loads and initializes all plugins / 加載並初始化所有插件
    /// </summary>
    Task LoadAndInitializePluginsAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// Event raised when plugins change / 插件變更時觸發的事件
    /// </summary>
    event EventHandler<PluginsChangedEventArgs>? PluginsChanged;
}

/// <summary>
/// Event args for plugins changed / 插件變更的事件參數
/// </summary>
public class PluginsChangedEventArgs : EventArgs
{
    public List<IPlugin> Added { get; }
    public List<int> Removed { get; }

    public PluginsChangedEventArgs(List<IPlugin> added, List<int> removed)
    {
        Added = added;
        Removed = removed;
    }
}

/// <summary>
/// Plugin loader implementation with hot-reload / 帶熱重載的插件加載器實現
/// </summary>
public class PluginLoader : IPluginLoader, IDisposable
{
    private readonly PluginHostOptions _options;
    private readonly ILogger<PluginLoader> _logger;
    private readonly Dictionary<int, IPlugin> _plugins = new();
    private readonly ConcurrentDictionary<string, FileInfo> _loadedFiles = new();
    private readonly ConcurrentDictionary<int, string> _pluginToFileMap = new(); // Track plugin FunctionId -> fileName
    private readonly Timer _scanTimer;
    private IServiceProvider? _serviceProvider;
    private bool _disposed;

    public IReadOnlyDictionary<int, IPlugin> Plugins => _plugins;

    public event EventHandler<PluginsChangedEventArgs>? PluginsChanged;

    public PluginLoader(PluginHostOptions options, ILogger<PluginLoader> logger)
    {
        _options = options;
        _logger = logger;

        // Create timer for periodic scanning
        _scanTimer = new Timer(_options.ScanInterval.TotalMilliseconds);
        _scanTimer.Elapsed += OnScanTimerElapsed;
    }

    public async Task LoadAndInitializePluginsAsync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Initial load
        await LoadPluginsFromFolderAsync();

        // Start hot-reload timer if enabled
        if (_options.EnableHotReload)
        {
            _scanTimer.Start();
            _logger.LogInformation("Hot-reload enabled with interval: {Interval}", _options.ScanInterval);
        }
    }

    private async void OnScanTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await ScanForChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for plugin changes");
        }
    }

    private async Task ScanForChangesAsync()
    {
        if (_serviceProvider == null) return;

        var functionsPath = GetFunctionsPath();
        if (!Directory.Exists(functionsPath)) return;

        var currentFiles = Directory.GetFiles(functionsPath, "*.dll", SearchOption.TopDirectoryOnly)
            .ToDictionary(Path.GetFileName!, f => new FileInfo(f));

        var added = new List<IPlugin>();
        var removed = new List<int>();

        // Check for removed plugins
        foreach (var (fileName, _) in _loadedFiles)
        {
            if (!currentFiles.ContainsKey(fileName))
            {
                // Find plugin by file name mapping
                var removedFunctionId = _pluginToFileMap.FirstOrDefault(x => x.Value == fileName).Key;

                if (removedFunctionId != 0 && _plugins.TryGetValue(removedFunctionId, out var plugin))
                {
                    _plugins.Remove(removedFunctionId);
                    _pluginToFileMap.TryRemove(removedFunctionId, out _);
                    removed.Add(removedFunctionId);
                    _loadedFiles.TryRemove(fileName, out _);
                    _logger.LogInformation("Plugin removed: {Name} (Function ID: {FunctionId})",
                        plugin.Name, plugin.FunctionId);
                }
            }
        }

        // Check for new/changed plugins
        foreach (var (fileName, fileInfo) in currentFiles)
        {
            if (!_loadedFiles.ContainsKey(fileName))
            {
                // New plugin
                try
                {
                    var assembly = Assembly.LoadFrom(fileInfo.FullName);
                    var plugin = await LoadSinglePluginAsync(assembly);

                    if (plugin != null && !_plugins.ContainsKey(plugin.FunctionId))
                    {
                        _plugins[plugin.FunctionId] = plugin;
                        _pluginToFileMap[plugin.FunctionId] = fileName;
                        _loadedFiles[fileName] = fileInfo;
                        added.Add(plugin);
                        _logger.LogInformation("Plugin added: {Name} (Function ID: {FunctionId})",
                            plugin.Name, plugin.FunctionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load new plugin: {FileName}", fileName);
                }
            }
            else
            {
                // Check if file was modified
                var existingInfo = _loadedFiles[fileName];
                if (fileInfo.LastWriteTime > existingInfo.LastWriteTime)
                {
                    // Plugin was modified - reload it
                    _logger.LogInformation("Plugin modified, reloading: {FileName}", fileName);

                    // Remove old version - find by file name
                    var oldFunctionId = _pluginToFileMap.FirstOrDefault(x => x.Value == fileName).Key;
                    if (oldFunctionId != 0 && _plugins.TryGetValue(oldFunctionId, out var oldPlugin))
                    {
                        _plugins.Remove(oldFunctionId);
                        removed.Add(oldFunctionId);
                    }

                    // Load new version
                    try
                    {
                        var assembly = Assembly.LoadFrom(fileInfo.FullName);
                        var plugin = await LoadSinglePluginAsync(assembly);

                        if (plugin != null && !_plugins.ContainsKey(plugin.FunctionId))
                        {
                            _plugins[plugin.FunctionId] = plugin;
                            _pluginToFileMap[plugin.FunctionId] = fileName;
                            _loadedFiles[fileName] = fileInfo;
                            added.Add(plugin);
                            _logger.LogInformation("Plugin reloaded: {Name} (Function ID: {FunctionId})",
                                plugin.Name, plugin.FunctionId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to reload plugin: {FileName}", fileName);
                    }
                }
            }
        }

        // Notify if there were changes
        if (added.Count > 0 || removed.Count > 0)
        {
            _logger.LogInformation("Plugins changed - Added: {AddedCount}, Removed: {RemovedCount}",
                added.Count, removed.Count);

            PluginsChanged?.Invoke(this, new PluginsChangedEventArgs(added, removed));
        }
    }

    private async Task<IPlugin?> LoadSinglePluginAsync(Assembly assembly)
    {
        if (_serviceProvider == null) return null;

        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract);

        foreach (var type in pluginTypes)
        {
            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(type)!;
                await plugin.InitializeAsync(_serviceProvider);
                return plugin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create plugin instance: {Type}", type.FullName);
            }
        }

        return null;
    }

    private string GetFunctionsPath()
    {
        var basePath = AppContext.BaseDirectory;
        var pluginsPath = _options.PluginsPath;

        if (!Path.IsPathRooted(pluginsPath))
        {
            pluginsPath = Path.GetFullPath(Path.Combine(basePath, pluginsPath));
        }

        return Path.Combine(pluginsPath, "functions");
    }

    private async Task LoadPluginsFromFolderAsync()
    {
        var functionsPath = GetFunctionsPath();

        _logger.LogInformation("Looking for plugins in: {Path}", functionsPath);

        if (!Directory.Exists(functionsPath))
        {
            _logger.LogWarning("Plugins functions folder not found: {Path}", functionsPath);
            return;
        }

        var dllFiles = Directory.GetFiles(functionsPath, "*.dll", SearchOption.TopDirectoryOnly);

        if (dllFiles.Length == 0)
        {
            _logger.LogWarning("No DLL files found in plugins functions folder: {Path}", functionsPath);
            return;
        }

        foreach (var dll in dllFiles)
        {
            try
            {
                var fileName = Path.GetFileName(dll);
                var fileInfo = new FileInfo(dll);
                _loadedFiles[fileName] = fileInfo;

                _logger.LogDebug("Loading assembly: {Assembly} from {Path}", fileName, dll);
                var assembly = Assembly.LoadFrom(dll);
                await LoadPluginsFromAssemblyAsync(assembly, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin from {Path}", dll);
            }
        }

        _logger.LogInformation("Loaded {Count} plugins from {Path}", _plugins.Count, functionsPath);
    }

    private async Task LoadPluginsFromAssemblyAsync(Assembly assembly, string fileName)
    {
        if (_serviceProvider == null) return;

        try
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t)
                            && !t.IsInterface
                            && !t.IsAbstract);

            foreach (var type in pluginTypes)
            {
                try
                {
                    var plugin = (IPlugin)Activator.CreateInstance(type)!;

                    if (_plugins.ContainsKey(plugin.FunctionId))
                    {
                        _logger.LogWarning("Plugin with Function ID {FunctionId} already loaded, skipping: {Name}",
                            plugin.FunctionId, plugin.Name);
                        continue;
                    }

                    _plugins[plugin.FunctionId] = plugin;
                    _pluginToFileMap[plugin.FunctionId] = fileName;
                    await plugin.InitializeAsync(_serviceProvider);

                    _logger.LogInformation("Loaded plugin: {Name} (Function ID: {FunctionId})",
                        plugin.Name, plugin.FunctionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create plugin instance: {Type}", type.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not scan assembly: {Assembly}", assembly.FullName);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _scanTimer.Stop();
        _scanTimer.Dispose();
        _disposed = true;
    }
}