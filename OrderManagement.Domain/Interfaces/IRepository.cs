using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Interfaces;

public interface IRepository<TEntity> 
    where TEntity : IEntity
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(Pagination pagination);
    Task<TEntity?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<TEntity> CreateAsync(TEntity order);
    Task UpdateAsync(TEntity order);
    Task DeleteAsync(TEntity order);
}
