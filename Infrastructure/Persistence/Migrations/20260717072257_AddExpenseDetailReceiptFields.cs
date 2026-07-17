using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDetailReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "invoice_number",
                table: "expense_details",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Receipt");

            migrationBuilder.AddColumn<string>(
                name: "receipt_type",
                table: "expense_details",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "invoice_number",
                table: "expense_details");

            migrationBuilder.DropColumn(
                name: "receipt_type",
                table: "expense_details");
        }
    }
}
