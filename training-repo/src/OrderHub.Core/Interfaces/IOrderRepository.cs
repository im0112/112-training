using OrderHub.Core.Common;
using OrderHub.Core.Domain;

namespace OrderHub.Core.Interfaces;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetPagedAsync(int page, int pageSize, OrderStatus? status);
    Task<Order?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId);
    Task<Dictionary<int, int>> GetSoldQuantitySinceAsync(DateTime since, OrderStatus excludeStatus);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}
