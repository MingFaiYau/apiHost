namespace SharedLib.Config;

/// <summary>
/// Configuration options for the plugin host / 插件主機的配置選項
/// </summary>
public class PluginHostOptions
{
    /// <summary>
    /// Base path for plugins folder / 插件資料夾的基本路徑
    /// </summary>
    public string PluginsPath { get; set; } = "plugins";

    /// <summary>
    /// Enable hot reload for plugins / 啟用插件熱重載
    /// </summary>
    public bool EnableHotReload { get; set; } = false;

    /// <summary>
    /// Scan interval for hot reload / 熱重載的掃描間隔
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Base URL for the API / API 的基本 URL
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";
}

/// <summary>
/// Database configuration options / 資料庫配置選項
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Database provider type / 資料庫供應商類型
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;

    /// <summary>
    /// Connection string / 連接字串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=pluginhost.db";
}

/// <summary>
/// Supported database providers / 支援的資料庫供應商
/// </summary>
public enum DatabaseProvider
{
    SqlServer,
    PostgreSql,
    MySql,
    Sqlite
}