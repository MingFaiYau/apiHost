using SharedLib.Schema.Models;

namespace SharedLib.Schema.Services;

/// <summary>
/// Interface for schema discovery service / Schema 發現服務的介面
/// </summary>
public interface ISchemaService
{
    /// <summary>
    /// Gets schema for a specific function ID / 獲取特定功能 ID 的 schema
    /// </summary>
    SchemaInfo? GetSchema(int functionId);

    /// <summary>
    /// Gets all registered schemas / 獲取所有已註冊的 schema
    /// </summary>
    IReadOnlyList<SchemaInfo> GetAllSchemas();

    /// <summary>
    /// Registers a new schema / 註冊新的 schema
    /// </summary>
    SchemaInfo RegisterSchema(SchemaInfo schema);
}