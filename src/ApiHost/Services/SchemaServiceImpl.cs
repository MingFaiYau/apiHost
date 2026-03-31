using SharedLib.Schema.Models;
using SharedLib.Schema.Services;

namespace ApiHost.Services;

/// <summary>
/// Implementation of schema service / Schema 服務的實現
/// </summary>
public class SchemaServiceImpl : ISchemaService
{
    private readonly Dictionary<int, SchemaInfo> _schemas = new();

    public SchemaInfo? GetSchema(int functionId)
    {
        return _schemas.TryGetValue(functionId, out var schema) ? schema : null;
    }

    public IReadOnlyList<SchemaInfo> GetAllSchemas()
    {
        return _schemas.Values.ToList();
    }

    public SchemaInfo RegisterSchema(SchemaInfo schema)
    {
        _schemas[schema.FunctionId] = schema;
        return schema;
    }
}