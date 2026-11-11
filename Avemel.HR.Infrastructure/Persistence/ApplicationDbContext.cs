using Avemel.HR.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Avemel.HR.Domain.Entities;


namespace Avemel.HR.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    //configures entity mappings
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }

}