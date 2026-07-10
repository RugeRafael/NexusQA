using QACopilot.Domain.Entities;
namespace QACopilot.Application.Interfaces.Repositories;
public interface ITestCaseHistoryRepository : IBaseRepository<TestCaseHistory>
{
    Task<IEnumerable<TestCaseHistory>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<TestCaseHistory>> GetByDocumentIdAsync(Guid documentId);
    Task<(IEnumerable<TestCaseHistory> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<(IEnumerable<TestCaseHistory> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid userId);
}
