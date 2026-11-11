
using Avemel.HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avemel.HR.Infrastructure.Persistence.Configurations;

public class DivisionConfiguration : IEntityTypeConfiguration<Division>
{
    public void Configure(EntityTypeBuilder<Division> builder)
    {
        builder.ToTable("divisions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.Code)
            .HasColumnName("code")
            .IsRequired();

        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(d => d.LegalName)
            .HasColumnName("legal_name")
            .HasDefaultValue("");

        builder.Property(d => d.Colour)
            .HasColumnName("colour")
            .HasDefaultValue("#0f766e");

        builder.Property(d => d.Active)
            .HasColumnName("active")
            .HasDefaultValue(true);

        builder.Property(d => d.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);
    }
}
