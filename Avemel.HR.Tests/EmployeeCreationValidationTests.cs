using Avemel.HR.Application.Employees;

namespace Avemel.HR.Tests;

public sealed class EmployeeCreationValidationTests
{
    private static EmployeeCreationRequest ValidRequest() => new()
    {
        EmployeeNumber = "AVM-001",
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.com",
        StartDate = new DateOnly(2026, 1, 1),
        WorkDaysPerWeek = 5,
        OrdinaryHoursPerDay = 9,
        OrdinaryHoursPerWeek = 45,
        RestDays = ["SAT", "SUN"]
    };

    [Fact]
    public void Required_fields_are_rejected()
    {
        var errors = new EmployeeCreationRequest().Validate();

        Assert.Contains(errors, error => error.Contains("First name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Last name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Start date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Five_day_week_requires_at_least_fifteen_custom_leave_days()
    {
        var request = ValidRequest() with { AnnualLeaveDays = 14 };

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Contains("15", StringComparison.Ordinal));
    }

    [Fact]
    public void Six_day_week_requires_eighteen_custom_leave_days()
    {
        var request = ValidRequest() with
        {
            WorkDaysPerWeek = 6,
            OrdinaryHoursPerDay = 8,
            RestDays = ["SUN"],
            AnnualLeaveDays = 17
        };

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Contains("18", StringComparison.Ordinal));
    }

    [Fact]
    public void Rotating_roster_cannot_have_fixed_rest_days()
    {
        var request = ValidRequest() with { WorksRoster = true };

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Contains("rotating roster", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Duplicate_or_invalid_rest_days_are_rejected()
    {
        var request = ValidRequest() with { RestDays = ["SAT", "SAT", "FUNDAY"] };

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Contains("duplicates", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("valid weekday", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Hours_above_forty_five_are_rejected()
    {
        var request = ValidRequest() with { OrdinaryHoursPerWeek = 45.01m };

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Contains("at most 45", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Invalid_role_pay_basis_and_dates_are_rejected()
    {
        var request = ValidRequest() with
        {
            Role = "OWNER",
            PayBasis = "DAILY",
            ProbationEndDate = new DateOnly(2025, 12, 31),
            EndDate = new DateOnly(2025, 12, 31)
        };

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Contains("access level", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("pay basis", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Probation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Last day", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Blank_employee_number_is_allowed_for_save_time_generation()
    {
        var request = ValidRequest() with { EmployeeNumber = "" };

        Assert.DoesNotContain(request.Validate(), error => error.Contains("employee number", StringComparison.OrdinalIgnoreCase));
    }
}
