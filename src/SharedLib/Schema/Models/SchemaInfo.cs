using System.Reflection;

namespace SharedLib.Schema.Models;

/// <summary>
/// Schema information for a service / 服務的 schema 信息
/// </summary>
public record SchemaInfo(
    /// <summary>
    /// Function ID / 功能 ID
    /// </summary>
    int FunctionId,

    /// <summary>
    /// Schema name / Schema 名稱
    /// </summary>
    string Name,

    /// <summary>
    /// Schema description / Schema 描述
    /// </summary>
    string Description,

    /// <summary>
    /// Properties of the schema / Schema 的屬性
    /// </summary>
    IReadOnlyList<SchemaPropertyInfo> Properties,

    /// <summary>
    /// Service type: "Crud" or "Custom" / 服務類型："Crud" 或 "Custom"
    /// </summary>
    string ServiceType);

/// <summary>
/// Schema property information / Schema 屬性信息
/// </summary>
public record SchemaPropertyInfo(
    /// <summary>
    /// Property name / 屬性名稱
    /// </summary>
    string Name,

    /// <summary>
    /// Property type / 屬性類型
    /// </summary>
    string TypeName,

    /// <summary>
    /// Property description / 屬性描述
    /// </summary>
    string Description,

    /// <summary>
    /// Whether the property is required / 屬性是否必需
    /// </summary>
    bool IsRequired,

    /// <summary>
    /// Maximum length (for string types) / 最大長度（用於字串類型）
    /// </summary>
    int? MaxLength);