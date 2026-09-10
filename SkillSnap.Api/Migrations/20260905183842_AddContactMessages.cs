using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillSnap.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                    table.CheckConstraint("CK_ContactMessages_Email_Format", "instr(\"Email\", '@') > 1");
                    table.CheckConstraint("CK_ContactMessages_Email_Length", "length(trim(\"Email\")) BETWEEN 3 AND 254");
                    table.CheckConstraint("CK_ContactMessages_Message_Length", "length(trim(\"Message\")) BETWEEN 10 AND 2000");
                    table.CheckConstraint("CK_ContactMessages_Name_Length", "length(trim(\"Name\")) BETWEEN 2 AND 100");
                    table.CheckConstraint("CK_ContactMessages_Name_NoDigits", "\"Name\" NOT GLOB '*[0-9]*'");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages");
        }
    }
}
