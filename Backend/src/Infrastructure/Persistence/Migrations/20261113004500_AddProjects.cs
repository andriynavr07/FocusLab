using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusLab.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261113004500_AddProjects")]
public partial class AddProjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "projects",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                account_id = table.Column<int>(type: "INTEGER", nullable: false),
                name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects", x => x.id);
                table.ForeignKey(
                    name: "FK_projects_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_projects_account_name",
            table: "projects",
            columns: new[] { "account_id", "name" },
            unique: true);

        migrationBuilder.Sql("""
            INSERT INTO projects (account_id, name, color)
            SELECT account_id, project, '#3B6EF5'
            FROM tasks
            WHERE project IS NOT NULL AND length(trim(project)) > 0
            GROUP BY account_id, project;

            INSERT OR IGNORE INTO projects (account_id, name, color)
            VALUES
                (1, 'Launch Plan', '#3B6EF5'),
                (1, 'Backend V2', '#10B981'),
                (1, 'Billing Module', '#F59E0B'),
                (1, 'FocusFlow App', '#A855F7');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "projects");
}
