using Microsoft.EntityFrameworkCore;
using QACopilot.Application.Interfaces.Repositories;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;
namespace QACopilot.Infrastructure.Repositories;
public class TestCaseHistoryRepository : BaseRepository<TestCaseHistory>, ITestCaseHistoryRepository
{
    public TestCaseHistoryRepository(QACopilotDbContext context) : base(context) { }

    public async Task<IEnumerable<TestCaseHistory>> GetByUserIdAsync(Guid userId) =>
        await _dbSet.Where(t => t.UserId == userId).ToListAsync();

    public async Task<IEnumerable<TestCaseHistory>> GetByDocumentIdAsync(Guid documentId) =>
        await _dbSet.Where(t => t.DocumentId == documentId).ToListAsync();

    // Sin filtro (mantener compatibilidad)
    public async Task<(IEnumerable<TestCaseHistory> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var total = await _dbSet.CountAsync();
        var items = await _dbSet
            .OrderByDescending(t => t.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Document)
            .ToListAsync();
        return (items, total);
    }

    // Con filtro por usuario
    public async Task<(IEnumerable<TestCaseHistory> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid userId)
    {
        var query = _dbSet.Where(t => t.UserId == userId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Document)
            .ToListAsync();
        return (items, total);
    }
}
