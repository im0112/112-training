using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStockReport_FiltersAtOrBelowThreshold_AndOrdersByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-EQUAL", stock: 10);
        TestSetup.AddProduct(db, sku: "SKU-LOWER", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-HIGHER-BELOW", stock: 7);
        TestSetup.AddProduct(db, sku: "SKU-ABOVE", stock: 20);

        var report = await service.GetLowStockReportAsync(10);

        Assert.Equal(3, report.Count);
        Assert.Equal("SKU-LOWER", report[0].Sku);
        Assert.Equal("SKU-HIGHER-BELOW", report[1].Sku);
        Assert.Equal("SKU-EQUAL", report[2].Sku);
        Assert.DoesNotContain(report, p => p.Sku == "SKU-ABOVE");
    }

    [Fact]
    public async Task GetLowStockReport_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-ACTIVE", stock: 5, isActive: true);
        TestSetup.AddProduct(db, sku: "SKU-INACTIVE", stock: 5, isActive: false);

        var report = await service.GetLowStockReportAsync(10);

        Assert.Single(report);
        Assert.Equal("SKU-ACTIVE", report[0].Sku);
    }

    [Fact]
    public async Task GetLowStockReport_Sold30Days_ExcludesCancelledAndOutOfRangeOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, sku: "SKU-A001", stock: 5);

        var orderOutOfRange = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Shipped,
            CreatedAt = DateTime.UtcNow.AddDays(-31)
        };
        orderOutOfRange.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 100, UnitPriceSnapshot = product.UnitPrice });

        var orderCancelled = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        orderCancelled.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 200, UnitPriceSnapshot = product.UnitPrice });

        var orderCounted = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Shipped,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        orderCounted.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 3, UnitPriceSnapshot = product.UnitPrice });

        db.Orders.AddRange(orderOutOfRange, orderCancelled, orderCounted);
        db.SaveChanges();

        var report = await service.GetLowStockReportAsync(10);

        Assert.Single(report);
        Assert.Equal(3, report[0].Sold30Days);
    }
}
