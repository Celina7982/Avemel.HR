using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avemel.HR.Infrastructure.Persistence.Migrations;

public partial class AddEmployeeAuditAndCaseInsensitiveEmail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_employees_email",
            table: "employees");

        migrationBuilder.CreateIndex(
            name: "IX_employees_email",
            table: "employees",
            column: "email");

        migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_employees_email_lower\" ON employees (lower(email));");

        migrationBuilder.CreateTable(
            name: "audit_events",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                acting_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                employee_id = table.Column<int>(type: "integer", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_audit_events", x => x.id));

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_employee_id",
            table: "audit_events",
            column: "employee_id");

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_created_at",
            table: "audit_events",
            column: "created_at");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_events");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_employees_email_lower\";");
        migrationBuilder.DropIndex(name: "IX_employees_email", table: "employees");
        migrationBuilder.CreateIndex(name: "IX_employees_email", table: "employees", column: "email", unique: true);
    }
}
