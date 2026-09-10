using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillSnap.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LiveUrl",
                table: "Projects",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryUrl",
                table: "Projects",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_LiveUrl_Length",
                table: "Projects",
                sql: "\"LiveUrl\" IS NULL OR length(\"LiveUrl\") <= 2048");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_RepositoryUrl_Length",
                table: "Projects",
                sql: "\"RepositoryUrl\" IS NULL OR length(\"RepositoryUrl\") <= 2048");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_LiveUrl_Length",
                table: "Projects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_RepositoryUrl_Length",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LiveUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RepositoryUrl",
                table: "Projects");
        }
    }
}
