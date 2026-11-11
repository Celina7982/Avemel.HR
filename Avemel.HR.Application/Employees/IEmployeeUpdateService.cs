using System.Security.Claims;

namespace Avemel.HR.Application.Employees;

public interface IEmployeeUpdateService
{
    Task<EmployeeUpdateResult> UpdateAsync(
        EmployeeUpdateRequest request,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeeUpdateResult(
    bool Succeeded,
    int? EmployeeId,
    IReadOnlyList<string> Errors)
{
    public static EmployeeUpdateResult Success(int employeeId) => new(true, employeeId, Array.Empty<string>());
    public static EmployeeUpdateResult Failure(params string[] errors) => new(false, null, errors);
}
