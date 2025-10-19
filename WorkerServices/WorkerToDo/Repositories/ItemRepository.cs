using Microsoft.EntityFrameworkCore;
using SharedLibreries.Models;
using WorkerServices.WorkerToDo.Data;

namespace WorkerServices.WorkerToDo.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly ToDoDbContext _context;

        public ItemRepository(ToDoDbContext context)
        {
            _context = context;
        }

        public async Task<Item> GetByIdAsync(Guid id)
        {
            return await _context.Items.FindAsync(id);
        }

        public async Task<IEnumerable<Item>> GetAllAsync()
        {
            return await _context.Items.ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsByUserIdAsync(Guid userId)
        {
            return await _context.Items.Where(item => item.UserId == userId).ToListAsync();
        }

        public async Task AddAsync(Item entity)
        {
            await _context.Items.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Item entity)
        {
            _context.Items.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.UtcNow;
                _context.Items.Update(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
