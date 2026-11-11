using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Avemel.HR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    division_id = table.Column<int>(type: "integer", nullable: true),
                    pay_basis = table.Column<string>(type: "text", nullable: false, defaultValue: "MONTHLY"),
                    work_days_per_week = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    ordinary_hours_per_day = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ordinary_hours_per_week = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    rest_days = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    works_roster = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    annual_leave_days = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0m),
                    job_title = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    department = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    manager_id = table.Column<int>(type: "integer", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    id_number = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    emergency_contact_name = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    emergency_contact_phone = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    basic_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    probation_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    employment_type = table.Column<string>(type: "text", nullable: false, defaultValue: "PERMANENT"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    identity_user_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.CheckConstraint("CK_employees_employment_type", "employment_type IN ('PERMANENT', 'FIXED_TERM', 'PART_TIME', 'CONTRACTOR', 'INTERN')");
                    table.CheckConstraint("CK_employees_pay_basis", "pay_basis IN ('MONTHLY', 'WEEKLY', 'HOURLY')");
                    table.CheckConstraint("CK_employees_status", "status IN ('ACTIVE', 'ON_NOTICE', 'TERMINATED')");
                    table.CheckConstraint("CK_employees_work_days", "work_days_per_week IN (5, 6)");
                    table.ForeignKey(
                        name: "FK_employees_AspNetUsers_identity_user_id",
                        column: x => x.identity_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_employees_divisions_division_id",
                        column: x => x.division_id,
                        principalTable: "divisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_employees_employees_manager_id",
                        column: x => x.manager_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_division_id",
                table: "employees",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_number",
                table: "employees",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_identity_user_id",
                table: "employees",
                column: "identity_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_manager_id",
                table: "employees",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_status",
                table: "employees",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
