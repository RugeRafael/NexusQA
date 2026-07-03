using Microsoft.EntityFrameworkCore;
using QACopilot.Application.Interfaces.Repositories;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Repositories;

public class AuditMetricRepository : BaseRepository<AuditMetric>, IAuditMetricRepository
{
    public AuditMetricRepository(QACopilotDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditMetric>> GetByModuleAsync(string module) =>
        await _dbSet.Where(a => a.Module == module).ToListAsync();

    public async Task<int> GetTotalByDateRangeAsync(DateTime from, DateTime to) =>
        await _dbSet.CountAsync(a => a.OccurredAt >= from && a.OccurredAt <= to);
}