using Avemel.HR.Application.Employees;
using Avemel.HR.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace Avemel.HR.Tests;

public sealed class EmployeeUpdateValidationTests
{
    private static EmployeeUpdateRequest ValidRequest() => new()
    {
        EmployeeId = 42,
        EmployeeNumber = "AVM-002",
        FirstName = "Grace",
        LastName = "Hopper",
        Email = "grace@example.com",
        Role = "MANAGER",
        StartDate = new DateOnly(2026, 1, 1),
        WorkDaysPerWeek = 5,
        OrdinaryHoursPerDay = 9,
        OrdinaryHoursPerWeek = 45,
        RestDays = ["SAT", "SUN"]
    };

    [Fact]
    public void Valid_update_request_passes_creation_rules()
    {
        var errors = ValidRequest().Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Employee_number_is_required_for_an_update()
    {
        var errors = (ValidRequest() with { EmployeeNumber = "" }).Validate();

        Assert.Contains(errors, error => error.Contains("employee number", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Update_validation_reuses_shared_business_rules()
    {
        var errors = (ValidRequest() with
        {
            PayBasis = "DAILY",
            Status = "TERMINATED",
            EndDate = null
        }).Validate();

        Assert.Contains(errors, error => error.Contains("pay basis", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("last day", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Update_request_maps_all_employee_fields_to_creation_validation_request()
    {
        var request = ValidRequest() with
        {
            DivisionId = 7,
            Phone = "555-0100",
            DateOfBirth = new DateOnly(1990, 5, 6),
            IdNumber = "ID-42",
            JobTitle = "Chief Engineer",
            Department = "Engineering",
            ManagerId = 9,
            EmploymentType = "FIXED_TERM",
            PayBasis = "WEEKLY",
            BasicSalary = 1200m,
            ProbationEndDate = new DateOnly(2026, 4, 1),
            Status = "ACTIVE",
            WorkDaysPerWeek = 6,
            WorksRoster = false,
            AnnualLeaveDays = 18,
            RestDays = ["SUN"],
            OrdinaryHoursPerDay = 8m,
            OrdinaryHoursPerWeek = 40m,
            EmergencyContactName = "Test Contact",
            EmergencyContactPhone = "555-0101"
        };

        var mapped = request.ToCreationRequest();

        Assert.Equal(request.EmployeeNumber, mapped.EmployeeNumber);
        Assert.Equal(request.Email, mapped.Email);
        Assert.Equal(request.DivisionId, mapped.DivisionId);
        Assert.Equal(request.ManagerId, mapped.ManagerId);
        Assert.Equal(request.BasicSalary, mapped.BasicSalary);
        Assert.Equal(request.RestDays, mapped.RestDays);
        Assert.Equal(request.EmergencyContactPhone, mapped.EmergencyContactPhone);
    }

    [Fact]
    public async Task Unauthenticated_update_is_rejected_before_database_access()
    {
        var service = new EmployeeUpdateService(
            null!,
            null!,
            null!,
            NullLogger<EmployeeUpdateService>.Instance);

        var result = await service.UpdateAsync(ValidRequest(), new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("administrator", StringComparison.OrdinalIgnoreCase));
    }
}
