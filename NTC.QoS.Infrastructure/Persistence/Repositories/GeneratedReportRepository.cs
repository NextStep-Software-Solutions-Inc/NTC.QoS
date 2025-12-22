using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for GeneratedReport entity
/// </summary>
internal sealed class GeneratedReportRepository(ApplicationDbContext context) 
    : Repository<GeneratedReport>(context), IGeneratedReportRepository
{
    public async Task<List<GeneratedReport>> GetUserReportsAsync(
        Guid userId, 
        int? skip = null, 
        int? take = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<GeneratedReport> query = Query(r => r.UserId == userId)
            .OrderByDescending(r => r.GeneratedAt);

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await ToListAsync(query, cancellationToken);
    }

    public async Task<GeneratedReport?> GetUserReportByIdAsync(
        Guid reportId, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await FirstOrDefaultAsync(
            Query(r => r.Id == reportId && r.UserId == userId)
                .Include(r => r.User),
            cancellationToken);
    }

    public async Task<int> CountUserReportsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Query(r => r.UserId == userId).CountAsync(cancellationToken);
    }

    public async Task<List<string>> FindSimilarReportsAsync(
        float[] queryEmbedding, 
        int maxResults, 
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(queryEmbedding);
        
        return await Query(r => r.Embedding != null)
            .OrderBy(r => r.Embedding!.L2Distance(queryVector))
            .Take(maxResults)
            .Select(r => r.Content)
            .ToListAsync(cancellationToken);
    }

    public async Task AddReportWithEmbeddingAsync(
        GeneratedReport report, 
        float[]? embedding, 
        CancellationToken cancellationToken = default)
    {
        // Set the embedding if provided
        if (embedding != null)
        {
            report.Embedding = new Vector(embedding);
        }

        await AddAsync(report, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
}
