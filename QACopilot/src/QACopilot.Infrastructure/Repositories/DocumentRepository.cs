using Microsoft.EntityFrameworkCore;
using QACopilot.Application.Interfaces.Repositories;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Repositories;

public class DocumentRepository : BaseRepository<Document>, IDocumentRepository
{
    public DocumentRepository(QACopilotDbContext context) : base(context) { }

    public async Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId) =>
        await _dbSet.Where(d => d.UserId == userId).ToListAsync();

    public async Task<Document?> GetByFileNameAsync(string fileName) =>
        await _dbSet.FirstOrDefaultAsync(d => d.FileName == fileName);
}