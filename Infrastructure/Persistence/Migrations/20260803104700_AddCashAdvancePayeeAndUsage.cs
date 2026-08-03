using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashAdvancePayeeAndUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 舊的預支款領款人只是手打字串，沒有帳號可對應，所以留 null——
            // 沒人是領款人，就沒人拿到「領款人可見」的權限。
            // 這跟舊報銷單 applicant_user_id 為 null 是同一個處理方式，不另外發明規則。
            migrationBuilder.AddColumn<Guid>(
                name: "payee_user_id",
                table: "cash_advances",
                type: "uuid",
                nullable: true);

            // 用途類型分三步加：先允許 null、把舊資料補成零用金、再改成必填。
            // EF 預設會塞空字串當 default，那不是合法的 enum 值，所以手動改成明確的 backfill。
            migrationBuilder.AddColumn<string>(
                name: "usage",
                table: "cash_advances",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            // backfill 成零用金：舊資料的用途欄位字面上就寫「零用金」，符合事實；
            // 它們也是核對邏輯的測試 fixture，維持全員可引用才不會擋掉既有報銷單。
            // 注意這跟「新建表單預設個人預支」是兩個獨立的決定，方向剛好相反：
            // backfill 要符合舊資料的事實，新建預設要選限制緊的一方。
            migrationBuilder.Sql("UPDATE cash_advances SET usage = 'PettyCash' WHERE usage IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "usage",
                table: "cash_advances",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payee_user_id",
                table: "cash_advances");

            migrationBuilder.DropColumn(
                name: "usage",
                table: "cash_advances");
        }
    }
}
