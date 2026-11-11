using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avemel.HR.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Action).IsRequired().HasMaxLength(100).HasColumnName("action");
        builder.Property(e => e.ActingUserId).IsRequired().HasMaxLength(450).HasColumnName("acting_user_id");
        builder.Property(e => e.EmployeeId).HasColumnName("employee_id");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.HasIndex(e => e.EmployeeId).HasDatabaseName("IX_audit_events_employee_id");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_audit_events_created_at");
    }
}
