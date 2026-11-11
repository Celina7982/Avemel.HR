
namespace Avemel.HR.Domain.Entities;

public class Employee
{
    public int Id { get; set; }

    // Employee identification
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Company division
    public int? DivisionId { get; set; }
    public Division? Division { get; set; }

    // Payroll and working arrangements
    public string PayBasis { get; set; } = "MONTHLY";
    public int WorkDaysPerWeek { get; set; } = 5;
    public decimal OrdinaryHoursPerDay { get; set; } = 9m;
    public decimal OrdinaryHoursPerWeek { get; set; } = 45m;


    // Working schedules and leave arrangements

    // Employee's scheduled days off.
    // Preserve the original stored format until we verify
    // how the prototype encodes and reads rest days.
    public string RestDays { get; set; } = string.Empty;

    // True when the employee follows a rotating roster.
    public bool WorksRoster { get; set; } = false;

    // Individual annual leave entitlement.
    // Zero means the standard entitlement applies.
    public decimal AnnualLeaveDays { get; set; } = 0m;


    // Job information
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Reporting manager
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; }
        = new List<Employee>();

    // Personal and contact information
    public string Phone { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;

    // Employment information
    public decimal BasicSalary { get; set; } = 0m;
    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string EmploymentType { get; set; } = "PERMANENT";
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Link to ASP.NET Core Identity
    public string? IdentityUserId { get; set; }
}
