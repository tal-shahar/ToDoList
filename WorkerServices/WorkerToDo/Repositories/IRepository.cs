using Microsoft.EntityFrameworkCore;
using SharedLibreries.Models;
using WorkerServices.WorkerToDo.Data;

namespace WorkerServices.WorkerToDo.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
    }

    public interface IItemRepository : IRepository<Item>
    {
        Task<IEnumerable<Item>> GetItemsByUserIdAsync(Guid userId);
        Task SoftDeleteAsync(Guid id);
    }
}
