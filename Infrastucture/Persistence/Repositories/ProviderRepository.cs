using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class ProviderRepository(ApplicationDbContext context) : IProviderRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Provider?> GetByIdAsync(int id)
    {
        return await _context.Providers.FindAsync(id);
    }

    public async Task<List<Provider>> GetAllAsync()
    {
        return await _context.Providers.ToListAsync();
    }

    public async Task AddAsync(Provider product)
    {
        await _context.Providers.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    // ... other methods (Update, Delete, etc.)
}
