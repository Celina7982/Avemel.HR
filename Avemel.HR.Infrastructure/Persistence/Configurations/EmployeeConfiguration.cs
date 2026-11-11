
using Avemel.HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avemel.HR.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration
    : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Existing internal employee numbers are preserved.
        builder.Property(e => e.EmployeeNumber)
            .HasColumnName("employee_number")
            .IsRequired()
            .ValueGeneratedNever();

        builder.HasIndex(e => e.EmployeeNumber)
            .IsUnique();

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired();

        builder.HasIndex(e => e.Email);

        // Company division
        builder.Property(e => e.DivisionId)
            .HasColumnName("division_id");

        builder.HasOne(e => e.Division)
            .WithMany()
            .HasForeignKey(e => e.DivisionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Payroll and working arrangements
        builder.Property(e => e.PayBasis)
            .HasColumnName("pay_basis")
            .HasDefaultValue("MONTHLY");

        builder.Property(e => e.WorkDaysPerWeek)
            .HasColumnName("work_days_per_week")
            .HasDefaultValue(5);

        builder.Property(e => e.OrdinaryHoursPerDay)
            .HasColumnName("ordinary_hours_per_day")
            .HasPrecision(6, 2);

        builder.Property(e => e.OrdinaryHoursPerWeek)
            .HasColumnName("ordinary_hours_per_week")
            .HasPrecision(6, 2);

        builder.Property(e => e.RestDays)
            .HasColumnName("rest_days")
            .HasDefaultValue("");

        builder.Property(e => e.WorksRoster)
            .HasColumnName("works_roster")
            .HasDefaultValue(false);

        builder.Property(e => e.AnnualLeaveDays)
            .HasColumnName("annual_leave_days")
            .HasPrecision(6, 2)
            .HasDefaultValue(0m);

        // Job information
        builder.Property(e => e.JobTitle)
            .HasColumnName("job_title")
            .HasDefaultValue("");

        builder.Property(e => e.Department)
            .HasColumnName("department")
            .HasDefaultValue("");

        // Reporting manager
        builder.Property(e => e.ManagerId)
            .HasColumnName("manager_id");

        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Personal and contact information
        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasDefaultValue("");

        builder.Property(e => e.DateOfBirth)
            .HasColumnName("date_of_birth");

        builder.Property(e => e.IdNumber)
            .HasColumnName("id_number")
            .HasDefaultValue("");

        builder.Property(e => e.EmergencyContactName)
            .HasColumnName("emergency_contact_name")
            .HasDefaultValue("");

        builder.Property(e => e.EmergencyContactPhone)
            .HasColumnName("emergency_contact_phone")
            .HasDefaultValue("");

        // Employment information
        builder.Property(e => e.BasicSalary)
            .HasColumnName("basic_salary")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.ProbationEndDate)
            .HasColumnName("probation_end_date");

        builder.Property(e => e.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(e => e.EndDate)
            .HasColumnName("end_date");

        builder.Property(e => e.EmploymentType)
            .HasColumnName("employment_type")
            .HasDefaultValue("PERMANENT");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasDefaultValue("ACTIVE");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");


        builder.Property(e => e.IdentityUserId)
            .HasColumnName("identity_user_id");

        builder.HasIndex(e => e.IdentityUserId)
            .IsUnique();

        // Each employee can have one ASP.NET Core Identity account.
        // Employees can also exist before their login is created.
        builder.HasOne<Avemel.HR.Infrastructure.Identity.ApplicationUser>()
            .WithOne()
            .HasForeignKey<Employee>(e => e.IdentityUserId)
            .OnDelete(DeleteBehavior.SetNull);


        // Original Avemel business rules
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_employees_pay_basis",
                "pay_basis IN ('MONTHLY', 'WEEKLY', 'HOURLY')");

            t.HasCheckConstraint(
                "CK_employees_work_days",
                "work_days_per_week IN (5, 6)");

            t.HasCheckConstraint(
                "CK_employees_employment_type",
                "employment_type IN ('PERMANENT', 'FIXED_TERM', 'PART_TIME', 'CONTRACTOR', 'INTERN')");

            t.HasCheckConstraint(
                "CK_employees_status",
                "status IN ('ACTIVE', 'ON_NOTICE', 'TERMINATED')");
        });

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.DivisionId);
        builder.HasIndex(e => e.ManagerId);
    }
}
