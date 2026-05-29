using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HazeClue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartwatchData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartwatchData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HeartRate = table.Column<double>(type: "float", nullable: true),
                    Hrv = table.Column<double>(type: "float", nullable: true),
                    SleepScore = table.Column<double>(type: "float", nullable: true),
                    Steps = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartwatchData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartwatchData_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartwatchData_UserId",
                table: "SmartwatchData",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartwatchData");
        }
    }
}
