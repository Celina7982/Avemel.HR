using System.Security.Claims;
using Avemel.HR.Application.Employees;
using Avemel.HR.Domain.Entities;
using Avemel.HR.Infrastructure.Identity;
using Avemel.HR.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avemel.HR.Infrastructure.Services;

public sealed class EmployeeUpdateService : IEmployeeUpdateService
{
    private static readonly string[] SupportedRoles = ["ADMIN", "MANAGER", "STAFF"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<EmployeeUpdateService> _logger;

    public EmployeeUpdateService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<EmployeeUpdateService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<EmployeeUpdateResult> UpdateAsync(
        EmployeeUpdateRequest request,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default)
    {
        if (actingUser.Identity?.IsAuthenticated != true || !actingUser.IsInRole("ADMIN"))
            return EmployeeUpdateResult.Failure("Only an authenticated administrator may update employees.");

        var errors = request.Validate().ToList();
        var email = request.Email.Trim();
        var employeeNumber = request.EmployeeNumber.Trim();
        var role = request.Role.Trim().ToUpperInvariant();

        if (errors.Count != 0)
            return new EmployeeUpdateResult(false, request.EmployeeId, errors);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
            return EmployeeUpdateResult.Failure("The employee could not be found.");

        if (request.DivisionId is not null && !await _db.Divisions.AnyAsync(
                d => d.Id == request.DivisionId && d.Active, cancellationToken))
            errors.Add("The selected division does not exist or is inactive.");

        if (request.ManagerId == request.EmployeeId)
            errors.Add("An employee cannot report to themselves.");
        else if (request.ManagerId is not null)
        {
            var manager = await _db.Employees.FirstOrDefaultAsync(
                e => e.Id == request.ManagerId && e.Status != "TERMINATED", cancellationToken);
            if (manager is null)
                errors.Add("The selected reporting manager does not exist or is no longer active.");
            else if (await CreatesReportingCycleAsync(request.EmployeeId, request.ManagerId.Value, cancellationToken))
                errors.Add("The selected reporting manager would create a circular reporting hierarchy.");
        }

        if (!await _roleManager.RoleExistsAsync(role))
            errors.Add("The selected Identity role is not available.");

        if (await _db.Employees.AnyAsync(
                e => e.Id != request.EmployeeId && e.Email.ToUpper() == email.ToUpper(), cancellationToken))
            errors.Add("An employee with this email address already exists.");

        if (await _db.Employees.AnyAsync(
                e => e.Id != request.EmployeeId && e.EmployeeNumber == employeeNumber, cancellationToken))
            errors.Add("An employee with this employee number already exists.");

        var identityUser = string.IsNullOrWhiteSpace(employee.IdentityUserId)
            ? null
            : await _userManager.FindByIdAsync(employee.IdentityUserId);

        if (identityUser is null)
            errors.Add("The employee does not have a linked Identity account that can be updated.");

        if (identityUser is not null)
        {
            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail is not null && existingByEmail.Id != identityUser.Id)
                errors.Add("An Identity account with this email address already exists.");

            var existingByName = await _userManager.FindByNameAsync(email);
            if (existingByName is not null && existingByName.Id != identityUser.Id)
                errors.Add("An Identity account with this username already exists.");
        }

        var actingUserId = _userManager.GetUserId(actingUser)
            ?? actingUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actingUserId))
            errors.Add("The authenticated administrator could not be identified.");

        if (identityUser is not null && identityUser.Id == actingUserId && role != "ADMIN")
            errors.Add("You cannot remove your own administrator access.");

        if (identityUser is not null && role != "ADMIN" && await _userManager.IsInRoleAsync(identityUser, "ADMIN"))
        {
            var administratorCount = await CountAdministratorsAsync(cancellationToken);
            if (administratorCount <= 1)
                errors.Add("The last administrator cannot be removed.");
        }

        if (errors.Count != 0)
            return new EmployeeUpdateResult(false, request.EmployeeId, errors);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            employee.EmployeeNumber = employeeNumber;
            employee.FirstName = request.FirstName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.Email = email;
            employee.DivisionId = request.DivisionId;
            employee.Phone = request.Phone.Trim();
            employee.DateOfBirth = request.DateOfBirth;
            employee.IdNumber = request.IdNumber.Trim();
            employee.JobTitle = request.JobTitle.Trim();
            employee.Department = request.Department.Trim();
            employee.ManagerId = request.ManagerId;
            employee.PayBasis = request.PayBasis.Trim().ToUpperInvariant();
            employee.WorkDaysPerWeek = request.WorkDaysPerWeek;
            employee.OrdinaryHoursPerDay = request.OrdinaryHoursPerDay;
            employee.OrdinaryHoursPerWeek = request.OrdinaryHoursPerWeek;
            employee.RestDays = request.WorksRoster
                ? string.Empty
                : string.Join(',', request.RestDays.Select(d => d.Trim().ToUpperInvariant()).Distinct());
            employee.WorksRoster = request.WorksRoster;
            employee.AnnualLeaveDays = request.AnnualLeaveDays;
            employee.EmergencyContactName = request.EmergencyContactName.Trim();
            employee.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
            employee.BasicSalary = request.BasicSalary;
            employee.ProbationEndDate = request.ProbationEndDate;
            employee.StartDate = request.StartDate!.Value;
            employee.EndDate = request.EndDate;
            employee.EmploymentType = request.EmploymentType.Trim().ToUpperInvariant();
            employee.Status = request.Status.Trim().ToUpperInvariant();

            var emailResult = await _userManager.SetEmailAsync(identityUser!, email);
            if (!emailResult.Succeeded)
                return await RollbackFailureAsync(transaction, emailResult.Errors, cancellationToken);

            var usernameResult = await _userManager.SetUserNameAsync(identityUser!, email);
            if (!usernameResult.Succeeded)
                return await RollbackFailureAsync(transaction, usernameResult.Errors, cancellationToken);

            var currentRoles = await _userManager.GetRolesAsync(identityUser!);
            var rolesToRemove = currentRoles
                .Where(currentRole => SupportedRoles.Contains(currentRole, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            if (rolesToRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(identityUser!, rolesToRemove);
                if (!removeResult.Succeeded)
                    return await RollbackFailureAsync(transaction, removeResult.Errors, cancellationToken);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(identityUser!, role);
            if (!addRoleResult.Succeeded)
                return await RollbackFailureAsync(transaction, addRoleResult.Errors, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            _db.AuditEvents.Add(new AuditEvent
            {
                Action = "STAFF_UPDATED",
                ActingUserId = actingUserId!,
                EmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return EmployeeUpdateResult.Success(employee.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Employee update failed because of a database constraint.");
            await transaction.RollbackAsync(cancellationToken);
            return EmployeeUpdateResult.Failure("The employee could not be saved because a database constraint was violated. Check the employee number and email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating employee {EmployeeId}.", request.EmployeeId);
            await transaction.RollbackAsync(cancellationToken);
            return EmployeeUpdateResult.Failure("The employee could not be updated. No changes were saved.");
        }
    }

    private async Task<bool> CreatesReportingCycleAsync(
        int employeeId,
        int managerId,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<int>();
        var currentId = managerId;
        while (visited.Add(currentId))
        {
            if (currentId == employeeId)
                return true;

            var nextManagerId = await _db.Employees
                .Where(e => e.Id == currentId)
                .Select(e => e.ManagerId)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextManagerId is null)
                return false;
            currentId = nextManagerId.Value;
        }

        return false;
    }

    private async Task<int> CountAdministratorsAsync(CancellationToken cancellationToken)
    {
        var adminRole = await _roleManager.FindByNameAsync("ADMIN");
        if (adminRole is null)
            return 0;

        return await _db.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id, cancellationToken);
    }

    private static async Task<EmployeeUpdateResult> RollbackFailureAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        IEnumerable<IdentityError> identityErrors,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        var errors = identityErrors.Select(error => error.Description).ToArray();
        return errors.Length == 0
            ? EmployeeUpdateResult.Failure("The Identity account could not be updated. No changes were saved.")
            : EmployeeUpdateResult.Failure(errors);
    }
}
