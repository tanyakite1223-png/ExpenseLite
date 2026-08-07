using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProtectedAccountAndLastSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_protected",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_signed_in_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_protected",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_signed_in_at",
                table: "users");
        }
    }
}
