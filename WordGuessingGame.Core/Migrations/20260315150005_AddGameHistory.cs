using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordGuessingGame.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddGameHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameHistories",
                schema: "wgg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Word = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WinnerUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WinnerUserId = table.Column<int>(type: "int", nullable: true),
                    OpponentUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OpponentUserId = table.Column<int>(type: "int", nullable: true),
                    TotalGuesses = table.Column<int>(type: "int", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameHistories_Users_OpponentUserId",
                        column: x => x.OpponentUserId,
                        principalSchema: "wgg",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameHistories_Users_WinnerUserId",
                        column: x => x.WinnerUserId,
                        principalSchema: "wgg",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameHistories_OpponentUserId",
                schema: "wgg",
                table: "GameHistories",
                column: "OpponentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameHistories_WinnerUserId",
                schema: "wgg",
                table: "GameHistories",
                column: "WinnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameHistories",
                schema: "wgg");
        }
    }
}
