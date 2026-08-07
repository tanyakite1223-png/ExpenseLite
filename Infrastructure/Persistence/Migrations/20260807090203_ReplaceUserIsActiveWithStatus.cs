using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserIsActiveWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF 原本生出來的順序是「先 DropColumn is_active、再 AddColumn status」，
            // 那樣既有帳號的狀態會全部變成預設的空字串，等於把資料弄丟。
            // 改成先加欄位、把舊值換算過去、最後才砍舊欄位。
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql(
                """
                UPDATE users
                SET status = CASE WHEN is_active THEN 'Active' ELSE 'Disabled' END;
                """);

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 三態壓回 bool 一定會失真：Pending 與 Disabled 都只能回到 false。
            migrationBuilder.Sql(
                """
                UPDATE users
                SET is_active = (status = 'Active');
                """);

            migrationBuilder.DropColumn(
                name: "status",
                table: "users");
        }
    }
}
