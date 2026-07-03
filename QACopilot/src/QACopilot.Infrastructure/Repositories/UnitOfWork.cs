using QACopilot.Application.Interfaces;
using QACopilot.Application.Interfaces.Repositories;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly QACopilotDbContext _context;

    public IDocumentRepository Documents { get; }
    public ITestCaseHistoryRepository TestCaseHistories { get; }
    public IAuditMetricRepository AuditMetrics { get; }

    public UnitOfWork(QACopilotDbContext context,
        IDocumentRepository documents,
        ITestCaseHistoryRepository testCaseHistories,
        IAuditMetricRepository auditMetrics)
    {
        _context = context;
        Documents = documents;
        TestCaseHistories = testCaseHistories;
        AuditMetrics = auditMetrics;
    }

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public void Dispose() =>
        _context.Dispose();
}