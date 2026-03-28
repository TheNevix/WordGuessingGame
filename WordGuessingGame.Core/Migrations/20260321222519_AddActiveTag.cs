using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordGuessingGame.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveTag",
                schema: "wgg",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveTag",
                schema: "wgg",
                table: "Users");
        }
    }
}
