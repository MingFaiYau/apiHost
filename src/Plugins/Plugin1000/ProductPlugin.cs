using System.Collections.Generic;
using PluginBase;
using SharedLib.Plugin.Abstractions;
using SharedLib.Schema.Attributes;
using SharedLib.Schema.Models;

namespace Plugin1000;

/// <summary>
/// Product entity / 產品實體
/// </summary>
[Schema("Product", Description = "Product management / 產品管理", IsCrudEnabled = true)]
public class Product
{
    public int Id { get; set; }

    [SchemaProperty(Description = "Product name / 產品名稱", IsRequired = true, MaxLength = 100)]
    public string Name { get; set; } = "";

    [SchemaProperty(Description = "Product price / 產品價格")]
    public decimal Price { get; set; }

    [SchemaProperty(Description = "Product description / 產品描述", MaxLength = 500)]
    public string? Description { get; set; }
}

/// <summary>
/// Product CRUD plugin (Function ID: 1000) / 產品 CRUD 插件（功能 ID: 1000）
/// </summary>
public class ProductPlugin : CrudPluginBase<Product>
{
    // In-memory storage for demo purposes
    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Product A", Price = 100.00m, Description = "First product" },
        new Product { Id = 2, Name = "Product B", Price = 200.00m, Description = "Second product" },
        new Product { Id = 3, Name = "Product C", Price = 300.00m, Description = "Third product" }
    };

    private static int _nextId = 4;

    public override int FunctionId => 1000;
    public override string Name => "Product Service";
    public override string Version => "1.0.0";

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Initialize base first (sets up SchemaService)
        return base.InitializeAsync(serviceProvider).ContinueWith(_ =>
        {
            // Register schema for this plugin
            BuildSchemaProperties();
            var schema = BuildSchema("Product", "Product CRUD operations");
            RegisterSchema(schema);
        });
    }

    public override IReadOnlyList<PluginRoute> GetRoutes()
    {
        return new List<PluginRoute>
        {
            new("/api/products", "GET", null, typeof(List<Product>)),
            new("/api/products/{id}", "GET", typeof(int), typeof(Product)),
            new("/api/products", "POST", typeof(Product), typeof(Product)),
            new("/api/products/{id}", "PUT", typeof(Product), typeof(Product)),
            new("/api/products/{id}", "DELETE", null, typeof(bool))
        };
    }

    public override Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Product>>(_products.ToList());
    }

    public override Task<Product?> GetByIdAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public override Task<Product> CreateAsync(Product entity)
    {
        entity.Id = _nextId++;
        _products.Add(entity);
        return Task.FromResult(entity);
    }

    public override Task<Product> UpdateAsync(Product entity)
    {
        var index = _products.FindIndex(p => p.Id == entity.Id);
        if (index >= 0)
        {
            _products[index] = entity;
        }
        return Task.FromResult(entity);
    }

    public override Task<bool> DeleteAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            _products.Remove(product);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}