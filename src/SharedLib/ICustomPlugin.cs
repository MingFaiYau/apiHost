namespace SharedLib.Plugin.Abstractions;

/// <summary>
/// Delegate for handling custom requests / 用於處理自定義請求的委託
/// </summary>
public delegate Task RequestHandler(object context);

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