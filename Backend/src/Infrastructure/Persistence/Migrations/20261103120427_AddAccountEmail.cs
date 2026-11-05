using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_accounts_username",
                table: "accounts");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "accounts",
                type: "TEXT",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "id",
                keyValue: 1,
                column: "email",
                value: "");

            migrationBuilder.CreateIndex(
                name: "ux_accounts_email",
                table: "accounts",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_accounts_email",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "email",
                table: "accounts");

            migrationBuilder.CreateIndex(
                name: "ux_accounts_username",
                table: "accounts",
                column: "username",
                unique: true);
        }
    }
}
