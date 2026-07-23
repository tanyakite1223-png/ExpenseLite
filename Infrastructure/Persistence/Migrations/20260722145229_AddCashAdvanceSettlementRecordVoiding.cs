using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashAdvanceSettlementRecordVoiding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_voided",
                table: "cash_advance_settlement_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "cash_advance_settlement_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                table: "cash_advance_settlement_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "voided_at",
                table: "cash_advance_settlement_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voided_by",
                table: "cash_advance_settlement_records",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_voided",
                table: "cash_advance_settlement_records");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "cash_advance_settlement_records");

            migrationBuilder.DropColumn(
                name: "void_reason",
                table: "cash_advance_settlement_records");

            migrationBuilder.DropColumn(
                name: "voided_at",
                table: "cash_advance_settlement_records");

            migrationBuilder.DropColumn(
                name: "voided_by",
                table: "cash_advance_settlement_records");
        }
    }
}
