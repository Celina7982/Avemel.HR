
using System.Data;
using Microsoft.EntityFrameworkCore;
using Avemel.HR.Infrastructure.Persistence;

namespace Avemel.HR.Infrastructure.Services;

public class EmployeeNumberService
{
    private readonly ApplicationDbContext _db;

    public EmployeeNumberService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateNextAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = _db.Database.GetDbConnection();

        bool shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.CommandText = "SELECT pg_advisory_xact_lock(749381);";
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var reconcileCommand = connection.CreateCommand())
            {
                reconcileCommand.CommandText = """
                    SELECT setval(
                        'public.employee_number_seq',
                        GREATEST(
                            (SELECT last_value FROM public.employee_number_seq),
                            (SELECT COALESCE(MAX(SUBSTRING(employee_number FROM 5)::bigint), 0)
                             FROM public.employees
                             WHERE employee_number ~ '^AVM-[0-9]+$')),
                        true);
                    """;
                await reconcileCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT nextval('public.employee_number_seq')";

            while (true)
            {
                var result = await command.ExecuteScalarAsync(cancellationToken);
                var candidate = $"AVM-{Convert.ToInt64(result):D3}";
                if (!await _db.Employees.AnyAsync(e => e.EmployeeNumber == candidate, cancellationToken))
                    return candidate;
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
