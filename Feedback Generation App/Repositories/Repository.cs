using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        private readonly FeedbackContext _context;

        public Repository(FeedbackContext context)
        {
            _context = context;
        }


        public IQueryable<T> GetQueryable()
        {
            return _context.Set<T>();
        }

        public async Task<T?> AddAsync(T entity)
        {
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> DeleteAsync(K id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"Entity with id {id} not found.");

            _context.Remove(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<T>?> GetAllAsync()
        {
            var entities = _context.Set<T>();
            if (entities == null)
                throw new InvalidOperationException("No entities found.");

            return await entities.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(K id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"Entity with id {id} not found.");

            return entity;
        }

        public async Task<T?> UpdateAsync(K key, T entity)
        {
            var existingEntity = await GetByIdAsync(key);
            if (existingEntity == null)
                throw new KeyNotFoundException($"Entity with id {key} not found.");

            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();

            return existingEntity;
        }



    }
}
