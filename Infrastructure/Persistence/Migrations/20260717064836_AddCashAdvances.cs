using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashAdvances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cash_advance_id",
                table: "expense_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "expense_reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "EmployeePaid");

            migrationBuilder.CreateTable(
                name: "cash_advances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payee_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purpose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    advanced_at = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_advances", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_advances");

            migrationBuilder.DropColumn(
                name: "cash_advance_id",
                table: "expense_reports");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "expense_reports");
        }
    }
}
