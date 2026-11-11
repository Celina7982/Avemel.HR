using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avemel.HR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeNumberSequence : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        CREATE SEQUENCE IF NOT EXISTS public.employee_number_seq
            AS bigint
            START WITH 1
            INCREMENT BY 1;
        """);

            migrationBuilder.Sql("""
        SELECT setval(
            'public.employee_number_seq',
            GREATEST(
                (SELECT last_value
                 FROM public.employee_number_seq),
                (SELECT COALESCE(
                    MAX(SUBSTRING(employee_number FROM 5)::bigint),
                    0
                )
                 FROM public.employees
                 WHERE employee_number ~ '^AVM-[0-9]+$')
            ),
            true
        );
        """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        DROP SEQUENCE IF EXISTS public.employee_number_seq;
        """);
        }

    }
}
