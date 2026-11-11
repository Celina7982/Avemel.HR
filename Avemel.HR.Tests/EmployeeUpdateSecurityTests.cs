using System.Security.Claims;
using Avemel.HR.Application.Employees;
using Avemel.HR.Domain.Entities;
using Avemel.HR.Infrastructure.Identity;
using Avemel.HR.Infrastructure.Persistence;
using Avemel.HR.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit.Sdk;

namespace Avemel.HR.Tests;

public sealed class EmployeeUpdateSecurityTests : IClassFixture<PostgresSecurityFixture>
{
    private readonly PostgresSecurityFixture _fixture;

    public EmployeeUpdateSecurityTests(PostgresSecurityFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Authenticated_staff_user_cannot_update_employee_records()
    {
        var service = new EmployeeUpdateService(null!, null!, null!, NullLogger<EmployeeUpdateService>.Instance);

        var result = await service.UpdateAsync(ValidRequest(), Principal("staff-user", "STAFF"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("administrator", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Authenticated_manager_user_cannot_update_employee_records()
    {
        var service = new EmployeeUpdateService(null!, null!, null!, NullLogger<EmployeeUpdateService>.Instance);

        var result = await service.UpdateAsync(ValidRequest(), Principal("manager-user", "MANAGER"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("administrator", StringComparison.OrdinalIgnoreCase));
    }

    [PostgreSqlFact]
    public async Task Administrator_cannot_remove_their_own_admin_role()
    {
        await _fixture.ResetAsync();
        await using var scope = _fixture.CreateScope();
        var admin = await _fixture.AddUserAsync(scope, "admin@example.com", "ADMIN");
        var employee = await _fixture.AddEmployeeAsync(scope, admin, "AVM-001", "admin@example.com");
        var service = scope.ServiceProvider.GetRequiredService<EmployeeUpdateService>();

        var result = await service.UpdateAsync(ValidRequest(employee.Id, "admin@example.com") with { Role = "STAFF" }, Principal(admin.Id, "ADMIN"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("own administrator", StringComparison.OrdinalIgnoreCase));
    }

    [PostgreSqlFact]
    public async Task Final_administrator_cannot_be_demoted()
    {
        await _fixture.ResetAsync();
        await using var scope = _fixture.CreateScope();
        var actingUser = await _fixture.AddUserAsync(scope, "acting@example.com");
        var finalAdmin = await _fixture.AddUserAsync(scope, "final-admin@example.com", "ADMIN");
        var employee = await _fixture.AddEmployeeAsync(scope, finalAdmin, "AVM-001", "final-admin@example.com");
        var service = scope.ServiceProvider.GetRequiredService<EmployeeUpdateService>();

        var result = await service.UpdateAsync(ValidRequest(employee.Id, "final-admin@example.com") with { Role = "STAFF" }, Principal(actingUser.Id, "ADMIN"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("last administrator", StringComparison.OrdinalIgnoreCase));
    }

    [PostgreSqlFact]
    public async Task Employee_cannot_be_assigned_as_their_own_reporting_manager()
    {
        await _fixture.ResetAsync();
        await using var scope = _fixture.CreateScope();
        var admin = await _fixture.AddUserAsync(scope, "admin@example.com", "ADMIN");
        var employeeUser = await _fixture.AddUserAsync(scope, "employee@example.com");
        var employee = await _fixture.AddEmployeeAsync(scope, employeeUser, "AVM-001", "employee@example.com");
        var service = scope.ServiceProvider.GetRequiredService<EmployeeUpdateService>();

        var result = await service.UpdateAsync(ValidRequest(employee.Id, "employee@example.com") with { ManagerId = employee.Id }, Principal(admin.Id, "ADMIN"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("themselves", StringComparison.OrdinalIgnoreCase));
    }

    [PostgreSqlFact]
    public async Task Circular_reporting_manager_relationships_are_rejected()
    {
        await _fixture.ResetAsync();
        await using var scope = _fixture.CreateScope();
        var admin = await _fixture.AddUserAsync(scope, "admin@example.com", "ADMIN");
        var employeeUser = await _fixture.AddUserAsync(scope, "employee@example.com");
        var managerUser = await _fixture.AddUserAsync(scope, "manager@example.com", "MANAGER");
        var employee = await _fixture.AddEmployeeAsync(scope, employeeUser, "AVM-001", "employee@example.com");
        var manager = await _fixture.AddEmployeeAsync(scope, managerUser, "AVM-002", "manager@example.com", employee.Id);
        var service = scope.ServiceProvider.GetRequiredService<EmployeeUpdateService>();

        var result = await service.UpdateAsync(ValidRequest(employee.Id, "employee@example.com") with { ManagerId = manager.Id }, Principal(admin.Id, "ADMIN"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("circular", StringComparison.OrdinalIgnoreCase));
    }

    [PostgreSqlFact]
    public async Task Duplicate_employee_emails_and_numbers_are_rejected()
    {
        await _fixture.ResetAsync();
        await using var scope = _fixture.CreateScope();
        var admin = await _fixture.AddUserAsync(scope, "admin@example.com", "ADMIN");
        var targetUser = await _fixture.AddUserAsync(scope, "target@example.com");
        var duplicateUser = await _fixture.AddUserAsync(scope, "duplicate@example.com");
        var target = await _fixture.AddEmployeeAsync(scope, targetUser, "AVM-001", "target@example.com");
        await _fixture.AddEmployeeAsync(scope, duplicateUser, "AVM-002", "duplicate@example.com");
        var service = scope.ServiceProvider.GetRequiredService<EmployeeUpdateService>();

        var duplicateEmail = await service.UpdateAsync(ValidRequest(target.Id, "duplicate@example.com"), Principal(admin.Id, "ADMIN"));
        var duplicateNumber = await service.UpdateAsync(ValidRequest(target.Id, "target@example.com") with { EmployeeNumber = "AVM-002" }, Principal(admin.Id, "ADMIN"));

        Assert.False(duplicateEmail.Succeeded);
        Assert.Contains(duplicateEmail.Errors, error => error.Contains("email", StringComparison.OrdinalIgnoreCase));
        Assert.False(duplicateNumber.Succeeded);
        Assert.Contains(duplicateNumber.Errors, error => error.Contains("employee number", StringComparison.OrdinalIgnoreCase));
    }

    [PostgreSqlFact]
    public async Task Failed_identity_update_rolls_back_employee_changes_and_audit_event()
    {
        await _fixture.ResetAsync();
        await using var scope = _fixture.CreateScope();
        var admin = await _fixture.AddUserAsync(scope, "admin@example.com", "ADMIN");
        var targetUser = await _fixture.AddUserAsync(scope, "target@example.com");
        var target = await _fixture.AddEmployeeAsync(scope, targetUser, "AVM-001", "target@example.com");
        var service = scope.ServiceProvider.GetRequiredService<EmployeeUpdateService>();
        var invalidIdentityEmail = new string('a', 260) + "@example.com";

        var result = await service.UpdateAsync(
            ValidRequest(target.Id, invalidIdentityEmail) with { FirstName = "Changed" },
            Principal(admin.Id, "ADMIN"));

        var savedEmployee = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Employees.AsNoTracking().SingleAsync(employee => employee.Id == target.Id);
        var auditEvents = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .AuditEvents.AsNoTracking().Where(audit => audit.EmployeeId == target.Id).ToListAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("Original", savedEmployee.FirstName);
        Assert.Equal("target@example.com", savedEmployee.Email);
        Assert.Empty(auditEvents);
    }

    private static ClaimsPrincipal Principal(string userId, string role) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        ], "Test"));

    private static EmployeeUpdateRequest ValidRequest(int employeeId = 42, string email = "target@example.com") => new()
    {
        EmployeeId = employeeId,
        EmployeeNumber = "AVM-099",
        FirstName = "Original",
        LastName = "Employee",
        Email = email,
        Role = "STAFF",
        StartDate = new DateOnly(2026, 1, 1),
        WorkDaysPerWeek = 5,
        OrdinaryHoursPerDay = 9,
        OrdinaryHoursPerWeek = 45,
        RestDays = ["SAT", "SUN"]
    };
}

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AVEMEL_HR_TEST_CONNECTION_STRING")))
            Skip = "Set AVEMEL_HR_TEST_CONNECTION_STRING to run the disposable PostgreSQL integration test.";
    }
}

public sealed class PostgresSecurityFixture : IAsyncLifetime
{
    private ServiceProvider? _services;
    private string? _databaseName;
    private string? _connectionString;
    private string? _unavailableReason;

    public async Task InitializeAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("AVEMEL_HR_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            _unavailableReason = "Set AVEMEL_HR_TEST_CONNECTION_STRING to a disposable PostgreSQL server connection string.";
            return;
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
            _databaseName = $"avemel_hr_tests_{Guid.NewGuid():N}";
            var adminBuilder = new NpgsqlConnectionStringBuilder(builder.ConnectionString) { Database = "postgres" };
            await using var adminConnection = new NpgsqlConnection(adminBuilder.ConnectionString);
            await adminConnection.OpenAsync();
            await using var createCommand = adminConnection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await createCommand.ExecuteNonQueryAsync();

            builder.Database = _databaseName;
            _connectionString = builder.ConnectionString;
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddDataProtection();

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseNpgsql(_connectionString));
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<EmployeeUpdateService>();
            _services = services.BuildServiceProvider();
            await ResetAsync();
        }
        catch (Exception exception)
        {
            _unavailableReason = $"PostgreSQL test database could not be created: {exception.Message}";
        }
    }

    public async Task ResetAsync()
    {
        EnsureAvailable();
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "ADMIN", "MANAGER", "STAFF" })
            Assert.True((await roleManager.CreateAsync(new IdentityRole(role))).Succeeded);
    }

    public AsyncServiceScope CreateScope()
    {
        EnsureAvailable();
        return _services!.CreateAsyncScope();
    }

    public async Task<ApplicationUser> AddUserAsync(AsyncServiceScope scope, string email, string? role = null)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "P@ssword123!");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        if (role is not null)
            Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    public async Task<Employee> AddEmployeeAsync(AsyncServiceScope scope, ApplicationUser user, string number, string email, int? managerId = null)
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employee = new Employee
        {
            EmployeeNumber = number,
            FirstName = "Original",
            LastName = "Employee",
            Email = email,
            ManagerId = managerId,
            StartDate = new DateOnly(2026, 1, 1),
            IdentityUserId = user.Id
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }

    public Task DisposeAsync()
    {
        _services?.Dispose();
        return DropDatabaseAsync();
    }

    private async Task DropDatabaseAsync()
    {
        if (_databaseName is null || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AVEMEL_HR_TEST_CONNECTION_STRING")))
            return;
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AVEMEL_HR_TEST_CONNECTION_STRING")!) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }

    private void EnsureAvailable()
    {
        if (_services is null)
            throw SkipException.ForSkip(_unavailableReason ?? "PostgreSQL test database is unavailable.");
    }
}