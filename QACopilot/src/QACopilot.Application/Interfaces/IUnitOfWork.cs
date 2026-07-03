using QACopilot.Application.Interfaces.Repositories;

namespace QACopilot.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IDocumentRepository Documents { get; }
    ITestCaseHistoryRepository TestCaseHistories { get; }
    IAuditMetricRepository AuditMetrics { get; }
    Task<int> SaveChangesAsync();
}