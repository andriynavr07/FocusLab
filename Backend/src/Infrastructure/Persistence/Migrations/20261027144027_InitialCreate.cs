using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_tags_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<int>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    deadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tasks_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "focus_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    task_id = table.Column<int>(type: "INTEGER", nullable: false),
                    start_date_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    estimated_minutes = table.Column<int>(type: "INTEGER", nullable: true),
                    duration_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    is_completed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_focus_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_focus_sessions_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_resources",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    task_id = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_resources", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_resources_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_tags",
                columns: table => new
                {
                    task_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tag_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_tags", x => new { x.task_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_task_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_tags_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "id", "created_at", "password_hash", "username" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "", "local" });

            migrationBuilder.CreateIndex(
                name: "ux_accounts_username",
                table: "accounts",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_focus_sessions_start_date_time",
                table: "focus_sessions",
                column: "start_date_time");

            migrationBuilder.CreateIndex(
                name: "ix_focus_sessions_task_id",
                table: "focus_sessions",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_account_id",
                table: "tags",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_resources_task_id",
                table: "task_resources",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_tags_tag_id",
                table: "task_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_account_id",
                table: "tasks",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status",
                table: "tasks",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "focus_sessions");

            migrationBuilder.DropTable(
                name: "task_resources");

            migrationBuilder.DropTable(
                name: "task_tags");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
