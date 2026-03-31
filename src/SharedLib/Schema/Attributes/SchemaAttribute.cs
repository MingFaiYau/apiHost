namespace SharedLib.Schema.Attributes;

/// <summary>
/// Attribute to define schema metadata for an entity / 用於定義實體 schema 元數據的屬性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SchemaAttribute : Attribute
{
    /// <summary>
    /// Schema name / Schema 名稱
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Schema description / Schema 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether CRUD operations are enabled / 是否啟用 CRUD 操作
    /// </summary>
    public bool IsCrudEnabled { get; set; } = true;

    public SchemaAttribute(string name)
    {
        Name = name;
        Description = "";
    }
}

/// <summary>
/// Attribute to define schema property metadata / 用於定義 schema 屬性元數據的屬性
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SchemaPropertyAttribute : Attribute
{
    /// <summary>
    /// Property description / 屬性描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether the property is required / 屬性是否必需
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Maximum length for string properties / 字串屬性的最大長度
    /// </summary>
    public int MaxLength { get; set; }

    public SchemaPropertyAttribute()
    {
        Description = "";
    }
}