using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "project",
                table: "tasks",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "project",
                table: "tasks");
        }
    }
}
