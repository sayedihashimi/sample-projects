using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunctionApp1.Migrations
{
    /// <inheritdoc />
    public partial class InitialHealthCheckSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CheckedUrl = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Results_TimestampUtc",
                table: "Results",
                column: "TimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");
        }
    }
}
