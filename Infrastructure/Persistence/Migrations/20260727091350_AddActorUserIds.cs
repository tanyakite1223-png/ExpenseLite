using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActorUserIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reviewer_user_id",
                table: "expense_review_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applicant_user_id",
                table: "expense_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "handled_by_user_id",
                table: "cash_advance_settlement_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "voided_by_user_id",
                table: "cash_advance_settlement_records",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reviewer_user_id",
                table: "expense_review_records");

            migrationBuilder.DropColumn(
                name: "applicant_user_id",
                table: "expense_reports");

            migrationBuilder.DropColumn(
                name: "handled_by_user_id",
                table: "cash_advance_settlement_records");

            migrationBuilder.DropColumn(
                name: "voided_by_user_id",
                table: "cash_advance_settlement_records");
        }
    }
}
