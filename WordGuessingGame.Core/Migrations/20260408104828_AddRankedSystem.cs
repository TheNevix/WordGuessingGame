using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordGuessingGame.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRankedSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Seasons",
                schema: "wgg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRankedStats",
                schema: "wgg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false),
                    RP = table.Column<int>(type: "int", nullable: false),
                    PeakRP = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRankedStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRankedStats_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalSchema: "wgg",
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRankedStats_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "wgg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "wgg",
                table: "Seasons",
                columns: new[] { "Id", "EndDate", "Name", "StartDate" },
                values: new object[] { 1, new DateTime(2026, 5, 8, 23, 59, 59, 0, DateTimeKind.Utc), "Season 1", new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_UserRankedStats_SeasonId",
                schema: "wgg",
                table: "UserRankedStats",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRankedStats_UserId_SeasonId",
                schema: "wgg",
                table: "UserRankedStats",
                columns: new[] { "UserId", "SeasonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRankedStats",
                schema: "wgg");

            migrationBuilder.DropTable(
                name: "Seasons",
                schema: "wgg");
        }
    }
}
