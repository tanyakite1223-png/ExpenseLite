using ExpenseLite.Application.Projects;
using ExpenseLite.Domain.Projects;
using ExpenseLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.Projects;

public sealed class EfProjectRepository : IProjectRepository
{
    private readonly ExpenseLiteDbContext _dbContext;

    public EfProjectRepository(ExpenseLiteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
