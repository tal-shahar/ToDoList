using Microsoft.EntityFrameworkCore;
using SharedLibreries.Models;
using WorkerServices.WorkerUser.Data;

namespace WorkerServices.WorkerUser.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
    }

    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
    }

}
