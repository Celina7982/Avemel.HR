using System.Security.Claims;
using Avemel.HR.Application.Employees;
using Avemel.HR.Domain.Entities;
using Avemel.HR.Infrastructure.Identity;
using Avemel.HR.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Avemel.HR.Infrastructure.Services;

public sealed class EmployeeCreationService : IEmployeeCreationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly EmployeeNumberService _numberService;
    private readonly ILogger<EmployeeCreationService> _logger;

    public EmployeeCreationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        EmployeeNumberService numberService,
        ILogger<EmployeeCreationService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _numberService = numberService;
        _logger = logger;
    }

    public async Task<EmployeeCreationResult> CreateAsync(
        EmployeeCreationRequest request,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default)
    {
        if (!(actingUser.Identity?.IsAuthenticated == true && actingUser.IsInRole("ADMIN")))
            return EmployeeCreationResult.Failure("Only an authenticated administrator may create employees.");

        var errors = request.Validate().ToList();
        if (errors.Count != 0)
            return new EmployeeCreationResult(false, null, errors);

        var email = request.Email.Trim();
        var employeeNumber = request.EmployeeNumber.Trim();
        var role = request.Role.Trim().ToUpperInvariant();

        if (request.DivisionId is not null && !await _db.Divisions.AnyAsync(d => d.Id == request.DivisionId && d.Active, cancellationToken))
            errors.Add("The selected division does not exist or is inactive.");
        if (request.ManagerId is not null && !await _db.Employees.AnyAsync(e => e.Id == request.ManagerId && e.Status != "TERMINATED", cancellationToken))
            errors.Add("The selected reporting manager does not exist or is no longer active.");
        if (!await _roleManager.RoleExistsAsync(role))
            errors.Add("The selected Identity role is not available.");
        if (await _db.Employees.AnyAsync(e => e.Email.ToUpper() == email.ToUpper(), cancellationToken))
            errors.Add("An employee with this email address already exists.");
        if (!string.IsNullOrWhiteSpace(employeeNumber) && await _db.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber, cancellationToken))
            errors.Add("An employee with this employee number already exists.");

        if (errors.Count != 0)
            return new EmployeeCreationResult(false, null, errors);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        ApplicationUser? identityUser = null;

        try
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
                employeeNumber = await _numberService.GenerateNextAsync(cancellationToken);

            identityUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                LockoutEnabled = true
            };

            var temporaryPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)) + "aA1!";
            var userResult = await _userManager.CreateAsync(identityUser, temporaryPassword);
            if (!userResult.Succeeded)
                return await RollbackFailureAsync(transaction, identityUser, userResult.Errors.Select(e => e.Description).ToArray(), cancellationToken);

            var roleResult = await _userManager.AddToRoleAsync(identityUser, role);
            if (!roleResult.Succeeded)
                return await RollbackFailureAsync(transaction, identityUser, roleResult.Errors.Select(e => e.Description).ToArray(), cancellationToken);

            var employee = new Employee
            {
                EmployeeNumber = employeeNumber,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                DivisionId = request.DivisionId,
                Phone = request.Phone.Trim(),
                DateOfBirth = request.DateOfBirth,
                IdNumber = request.IdNumber.Trim(),
                JobTitle = request.JobTitle.Trim(),
                Department = request.Department.Trim(),
                ManagerId = request.ManagerId,
                PayBasis = request.PayBasis.Trim().ToUpperInvariant(),
                WorkDaysPerWeek = request.WorkDaysPerWeek,
                OrdinaryHoursPerDay = request.OrdinaryHoursPerDay,
                OrdinaryHoursPerWeek = request.OrdinaryHoursPerWeek,
                RestDays = request.WorksRoster ? string.Empty : string.Join(',', request.RestDays.Select(d => d.Trim().ToUpperInvariant()).Distinct()),
                WorksRoster = request.WorksRoster,
                AnnualLeaveDays = request.AnnualLeaveDays,
                EmergencyContactName = request.EmergencyContactName.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone.Trim(),
                BasicSalary = request.BasicSalary,
                ProbationEndDate = request.ProbationEndDate,
                StartDate = request.StartDate!.Value,
                EndDate = request.EndDate,
                EmploymentType = request.EmploymentType.Trim().ToUpperInvariant(),
                Status = request.Status.Trim().ToUpperInvariant(),
                IdentityUserId = identityUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync(cancellationToken);

            var actingUserId = _userManager.GetUserId(actingUser) ?? actingUser.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            _db.AuditEvents.Add(new AuditEvent
            {
                Action = "STAFF_CREATED",
                ActingUserId = actingUserId,
                EmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return EmployeeCreationResult.Success(employee.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Employee creation failed because of a database constraint.");
            if (identityUser is not null)
                await _userManager.DeleteAsync(identityUser);
            await transaction.RollbackAsync(cancellationToken);
            return EmployeeCreationResult.Failure("The employee could not be saved because a database constraint was violated. Check the employee number and email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating an employee.");
            if (identityUser is not null)
                await _userManager.DeleteAsync(identityUser);
            await transaction.RollbackAsync(cancellationToken);
            return EmployeeCreationResult.Failure("The employee could not be created. No changes were saved.");
        }
    }

    private async Task<EmployeeCreationResult> RollbackFailureAsync(
        IDbContextTransaction transaction,
        ApplicationUser identityUser,
        string[] errors,
        CancellationToken cancellationToken)
    {
        await _userManager.DeleteAsync(identityUser);
        await transaction.RollbackAsync(cancellationToken);
        return errors.Length == 0
            ? EmployeeCreationResult.Failure("Identity account creation failed.")
            : EmployeeCreationResult.Failure(errors);
    }
}
