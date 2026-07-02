using Microsoft.EntityFrameworkCore;
using Segfy.Domain.Policies;

namespace Segfy.Infrastructure.Persistence;

public sealed class SegfyDbContext : DbContext
{
    public SegfyDbContext(DbContextOptions<SegfyDbContext> options) : base(options) { }

    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyStatusHistory> PolicyStatusHistory => Set<PolicyStatusHistory>();
    public DbSet<PolicyNumberSequenceRow> PolicyNumberSequences => Set<PolicyNumberSequenceRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SegfyDbContext).Assembly);
    }
}
