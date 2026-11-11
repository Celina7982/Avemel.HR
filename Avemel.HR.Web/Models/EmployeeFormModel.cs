
using System;
using System.ComponentModel.DataAnnotations;

namespace Avemel.HR.Web.Models;

public class EmployeeFormModel : IValidatableObject
{
    public string EmployeeNumber { get; set; } = "";

    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    public int? DivisionId { get; set; }

    public string Phone { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public string IdNumber { get; set; } = "";

    public string JobTitle { get; set; } = "";
    public string Department { get; set; } = "";
    public int? ManagerId { get; set; }

    public string Role { get; set; } = "STAFF";
    public string EmploymentType { get; set; } = "PERMANENT";
    public string PayBasis { get; set; } = "MONTHLY";

    [Range(0, double.MaxValue, ErrorMessage = "Basic pay cannot be negative.")]
    public decimal? BasicSalary { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly? StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? ProbationEndDate { get; set; }

    public string Status { get; set; } = "ACTIVE";
    public DateOnly? EndDate { get; set; }

    public int WorkDaysPerWeek { get; set; } = 5;
    public bool WorksRoster { get; set; }

    [Range(0, 60, ErrorMessage = "Annual leave must be between 0 and 60 days.")]
    public decimal? AnnualLeaveDays { get; set; }

    public string RestDays { get; set; } = "";

    [Range(typeof(decimal), "0.01", "24", ErrorMessage = "Ordinary hours per day must be greater than zero and at most 24.")]
    public decimal OrdinaryHoursPerDay { get; set; } = 8m;

    [Range(typeof(decimal), "0.01", "45", ErrorMessage = "Ordinary hours per week must be greater than zero and at most 45.")]
    public decimal OrdinaryHoursPerWeek { get; set; } = 45m;

    public string EmergencyContactName { get; set; } = "";
    public string EmergencyContactPhone { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (WorkDaysPerWeek is not (5 or 6))
            yield return new ValidationResult("Working days must be five or six per week.", [nameof(WorkDaysPerWeek)]);

        var minimumLeave = WorkDaysPerWeek == 6 ? 18 : 15;
        if (AnnualLeaveDays is > 0 and < 15 || (WorkDaysPerWeek == 6 && AnnualLeaveDays is > 0 and < 18))
            yield return new ValidationResult($"Annual leave cannot be below {minimumLeave} days for a {WorkDaysPerWeek}-day week.", [nameof(AnnualLeaveDays)]);

        var restDays = RestDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validDays = new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
        if (WorksRoster && restDays.Length > 0)
            yield return new ValidationResult("Rotating roster employees cannot have fixed rest days.", [nameof(RestDays)]);
        if (restDays.Distinct(StringComparer.OrdinalIgnoreCase).Count() != restDays.Length)
            yield return new ValidationResult("Rest days cannot contain duplicates.", [nameof(RestDays)]);
        if (restDays.Any(day => !validDays.Contains(day, StringComparer.OrdinalIgnoreCase)))
            yield return new ValidationResult("Rest days must use valid weekday codes.", [nameof(RestDays)]);
        if (!WorksRoster && restDays.Length > 0 && restDays.Length != 7 - WorkDaysPerWeek)
            yield return new ValidationResult($"A {WorkDaysPerWeek}-day week must have {7 - WorkDaysPerWeek} fixed rest day(s).", [nameof(RestDays)]);

        if (ProbationEndDate is not null && StartDate is not null && ProbationEndDate < StartDate)
            yield return new ValidationResult("Probation end date cannot be before the start date.", [nameof(ProbationEndDate)]);
        if (EndDate is not null && StartDate is not null && EndDate < StartDate)
            yield return new ValidationResult("Last day cannot be before the start date.", [nameof(EndDate)]);
        if (Status == "TERMINATED" && EndDate is null)
            yield return new ValidationResult("A terminated employee must have a last day.", [nameof(EndDate)]);
        if (Status != "TERMINATED" && EndDate is not null)
            yield return new ValidationResult("Only terminated employees may have a last day.", [nameof(EndDate)]);
    }
}
