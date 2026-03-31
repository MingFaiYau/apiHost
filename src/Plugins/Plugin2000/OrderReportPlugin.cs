using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Http.Results;
using PluginBase;
using SharedLib.Plugin.Abstractions;

namespace Plugin2000;

/// <summary>
/// Custom plugin for order reporting (Function ID: 2000) / 訂單報告自定義插件（功能 ID: 2000）
/// </summary>
public class OrderReportPlugin : CustomPluginBase
{
    private readonly Dictionary<string, RequestHandler> _handlers = new();

    public override int FunctionId => 2000;
    public override string Name => "Order Report Service";
    public override string Version => "1.0.0";

    public override IReadOnlyDictionary<string, RequestHandler> Handlers => _handlers;

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Initialize base first
        return base.InitializeAsync(serviceProvider).ContinueWith(_ =>
        {
            // Register custom schema
            RegisterCustomSchema("OrderReport", "Order reporting service");

            // Initialize handlers
            _handlers["/api/reports/orders/daily"] = HandleDailyReport;
            _handlers["/api/reports/orders/summary"] = HandleSummaryReport;
        });
    }

    public override IReadOnlyList<PluginRoute> GetRoutes()
    {
        return new List<PluginRoute>
        {
            new("/api/reports/orders/daily", "GET", null, typeof(DailyReportResult)),
            new("/api/reports/orders/summary", "GET", null, typeof(SummaryReportResult))
        };
    }

    private Task HandleDailyReport(object context)
    {
        if (context is not HttpContext httpContext)
            return Task.CompletedTask;

        var report = new DailyReportResult
        {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            TotalOrders = 150,
            TotalRevenue = 25000.00m,
            TopProducts = new List<string> { "Product A", "Product B", "Product C" }
        };

        return httpContext.Response.WriteAsJsonAsync(report);
    }

    private Task HandleSummaryReport(object context)
    {
        if (context is not HttpContext httpContext)
            return Task.CompletedTask;

        var report = new SummaryReportResult
        {
            Period = "2026-Q1",
            TotalOrders = 4500,
            TotalRevenue = 750000.00m,
            AverageOrderValue = 166.67m,
            TopCategories = new List<string> { "Electronics", "Clothing", "Books" }
        };

        return httpContext.Response.WriteAsJsonAsync(report);
    }
}

/// <summary>
/// Daily report result / 每日報告結果
/// </summary>
public class DailyReportResult
{
    public string Date { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<string> TopProducts { get; set; } = new();
}

/// <summary>
/// Summary report result / 摘要報告結果
/// </summary>
public class SummaryReportResult
{
    public string Period { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<string> TopCategories { get; set; } = new();
}