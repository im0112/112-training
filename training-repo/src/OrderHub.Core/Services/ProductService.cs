using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockReportAsync(int threshold)
    {
        var products = await _productRepository.GetLowStockAsync(threshold);
        if (products.Count == 0)
            return Array.Empty<LowStockProduct>();

        var since = DateTime.UtcNow.AddDays(-30);
        var soldQuantities = await _orderRepository.GetSoldQuantitySinceAsync(since, OrderStatus.Cancelled);

        return products
            .Select(p => new LowStockProduct(
                p.Sku,
                p.Name,
                p.StockQuantity,
                soldQuantities.TryGetValue(p.Id, out var qty) ? qty : 0))
            .ToList();
    }
}
