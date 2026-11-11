using System.ComponentModel.DataAnnotations;

namespace Avemel.HR.Application.Employees;

public sealed record EmployeeCreationRequest
{
    public string EmployeeNumber { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public int? DivisionId { get; init; }
    public string Phone { get; init; } = "";
    public DateOnly? DateOfBirth { get; init; }
    public string IdNumber { get; init; } = "";
    public string JobTitle { get; init; } = "";
    public string Department { get; init; } = "";
    public int? ManagerId { get; init; }
    public string Role { get; init; } = "STAFF";
    public string EmploymentType { get; init; } = "PERMANENT";
    public string PayBasis { get; init; } = "MONTHLY";
    public decimal BasicSalary { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public string Status { get; init; } = "ACTIVE";
    public DateOnly? EndDate { get; init; }
    public int WorkDaysPerWeek { get; init; } = 5;
    public bool WorksRoster { get; init; }
    public decimal AnnualLeaveDays { get; init; }
    public IReadOnlyCollection<string> RestDays { get; init; } = Array.Empty<string>();
    public decimal OrdinaryHoursPerDay { get; init; }
    public decimal OrdinaryHoursPerWeek { get; init; }
    public string EmergencyContactName { get; init; } = "";
    public string EmergencyContactPhone { get; init; } = "";

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        var roles = new[] { "STAFF", "MANAGER", "ADMIN" };
        var employmentTypes = new[] { "PERMANENT", "FIXED_TERM", "PART_TIME", "CONTRACTOR", "INTERN" };
        var payBases = new[] { "MONTHLY", "WEEKLY", "HOURLY" };
        var statuses = new[] { "ACTIVE", "ON_NOTICE", "TERMINATED" };
        var validRestDays = new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

        if (string.IsNullOrWhiteSpace(FirstName)) errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(LastName)) errors.Add("Last name is required.");
        if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email)) errors.Add("Enter a valid email address.");
        if (StartDate is null) errors.Add("Start date is required.");
        if (!roles.Contains(Role, StringComparer.OrdinalIgnoreCase)) errors.Add("Select a valid access level.");
        if (!employmentTypes.Contains(EmploymentType, StringComparer.OrdinalIgnoreCase)) errors.Add("Select a valid employment type.");
        if (!payBases.Contains(PayBasis, StringComparer.OrdinalIgnoreCase)) errors.Add("Select a valid pay basis.");
        if (!statuses.Contains(Status, StringComparer.OrdinalIgnoreCase)) errors.Add("Select a valid employment status.");
        if (WorkDaysPerWeek is not (5 or 6)) errors.Add("Working days must be five or six per week.");
        if (OrdinaryHoursPerDay <= 0 || OrdinaryHoursPerDay > 24) errors.Add("Ordinary hours per day must be greater than zero and at most 24.");
        if (OrdinaryHoursPerWeek <= 0 || OrdinaryHoursPerWeek > 45) errors.Add("Ordinary hours per week must be greater than zero and at most 45.");
        if (BasicSalary < 0) errors.Add("Basic pay cannot be negative.");

        var minimumLeave = WorkDaysPerWeek == 6 ? 18 : 15;
        if (AnnualLeaveDays < 0 || AnnualLeaveDays > 60) errors.Add("Annual leave must be between the standard minimum and 60 days.");
        if (AnnualLeaveDays > 0 && AnnualLeaveDays < minimumLeave) errors.Add($"Annual leave cannot be below {minimumLeave} days for a {WorkDaysPerWeek}-day week.");

        var restDayList = RestDays.Select(day => day.Trim().ToUpperInvariant()).ToList();
        if (WorksRoster && restDayList.Count != 0) errors.Add("Rotating roster employees cannot have fixed rest days.");
        if (restDayList.Count != restDayList.Distinct(StringComparer.Ordinal).Count()) errors.Add("Rest days cannot contain duplicates.");
        if (restDayList.Any(day => !validRestDays.Contains(day))) errors.Add("Rest days must use valid weekday codes.");
        if (!WorksRoster && restDayList.Count != 0 && restDayList.Count != 7 - WorkDaysPerWeek) errors.Add($"A {WorkDaysPerWeek}-day week must have {7 - WorkDaysPerWeek} fixed rest day(s).");

        if (ProbationEndDate is not null && StartDate is not null && ProbationEndDate < StartDate) errors.Add("Probation end date cannot be before the start date.");
        if (EndDate is not null && StartDate is not null && EndDate < StartDate) errors.Add("Last day cannot be before the start date.");
        if (Status == "TERMINATED" && EndDate is null) errors.Add("A terminated employee must have a last day.");
        if (Status != "TERMINATED" && EndDate is not null) errors.Add("Only terminated employees may have a last day.");

        return errors;
    }
}
