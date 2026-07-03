using Microsoft.EntityFrameworkCore;
using QACopilot.Application.Interfaces.Repositories;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly QACopilotDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(QACopilotDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) =>
        await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _dbSet.ToListAsync();

    public async Task AddAsync(T entity) =>
        await _dbSet.AddAsync(entity);

    public void Update(T entity) =>
        _dbSet.Update(entity);

    public void Delete(T entity) =>
        _dbSet.Remove(entity);
}