using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordGuessingGame.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRankedMatchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RankedMatchHistories",
                schema: "wgg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false),
                    OpponentName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Won = table.Column<bool>(type: "bit", nullable: false),
                    RPChange = table.Column<int>(type: "int", nullable: false),
                    NewRP = table.Column<int>(type: "int", nullable: false),
                    MySeriesWins = table.Column<int>(type: "int", nullable: false),
                    OpponentSeriesWins = table.Column<int>(type: "int", nullable: false),
                    WasForfeit = table.Column<bool>(type: "bit", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankedMatchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankedMatchHistories_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalSchema: "wgg",
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedMatchHistories_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "wgg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RankedMatchHistories_SeasonId",
                schema: "wgg",
                table: "RankedMatchHistories",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedMatchHistories_UserId_SeasonId",
                schema: "wgg",
                table: "RankedMatchHistories",
                columns: new[] { "UserId", "SeasonId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RankedMatchHistories",
                schema: "wgg");
        }
    }
}
