using Microsoft.Extensions.Logging;

namespace SharedLib.Plugin.Abstractions;

/// <summary>
/// Interface for generic CRUD operations / 通用 CRUD 操作的介面
/// </summary>
public interface ICrudPlugin : IPlugin
{
    /// <summary>
    /// Entity type for this CRUD service / 此 CRUD 服務的實體類型
    /// </summary>
    Type EntityType { get; }
}

/// <summary>
/// Generic CRUD plugin with type parameter / 帶類型參數的通用 CRUD 插件
/// </summary>
public interface ICrudPlugin<TEntity> : ICrudPlugin where TEntity : class
{
    /// <summary>
    /// Gets all entities / 獲取所有實體
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync();

    /// <summary>
    /// Gets entity by ID / 根據 ID 獲取實體
    /// </summary>
    Task<TEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new entity / 創建新實體
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity);

    /// <summary>
    /// Updates an existing entity / 更新現有實體
    /// </summary>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Deletes an entity by ID / 根據 ID 刪除實體
    /// </summary>
    Task<bool> DeleteAsync(int id);
}