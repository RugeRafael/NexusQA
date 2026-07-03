using QACopilot.Domain.Entities;

namespace QACopilot.Application.Interfaces.Repositories;

public interface IAuditMetricRepository : IBaseRepository<AuditMetric>
{
    Task<IEnumerable<AuditMetric>> GetByModuleAsync(string module);
    Task<int> GetTotalByDateRangeAsync(DateTime from, DateTime to);
}