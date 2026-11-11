using System.Security.Claims;

namespace Avemel.HR.Application.Employees;

public interface IEmployeeCreationService
{
    Task<EmployeeCreationResult> CreateAsync(
        EmployeeCreationRequest request,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeeCreationResult(
    bool Succeeded,
    int? EmployeeId,
    IReadOnlyList<string> Errors)
{
    public static EmployeeCreationResult Success(int employeeId) => new(true, employeeId, Array.Empty<string>());
    public static EmployeeCreationResult Failure(params string[] errors) => new(false, null, errors);
}
