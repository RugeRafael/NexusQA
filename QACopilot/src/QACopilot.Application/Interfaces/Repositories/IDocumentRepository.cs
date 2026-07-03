using QACopilot.Domain.Entities;

namespace QACopilot.Application.Interfaces.Repositories;

public interface IDocumentRepository : IBaseRepository<Document>
{
    Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId);
    Task<Document?> GetByFileNameAsync(string fileName);
}