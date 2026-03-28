using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordGuessingGame.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerColorAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerColor",
                schema: "wgg",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserTags",
                schema: "wgg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTags_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "wgg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTags_UserId",
                schema: "wgg",
                table: "UserTags",
                column: "UserId");

            // Backfill: give every existing user the OG tag
            migrationBuilder.Sql(@"
                INSERT INTO [wgg].[UserTags] (UserId, Name)
                SELECT Id, 'OG' FROM [wgg].[Users]
                WHERE Id NOT IN (SELECT UserId FROM [wgg].[UserTags] WHERE Name = 'OG')
            ");

            // Backfill: set default banner color for existing users
            migrationBuilder.Sql(@"
                UPDATE [wgg].[Users] SET BannerColor = '#5b21b6' WHERE BannerColor = ''
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTags",
                schema: "wgg");

            migrationBuilder.DropColumn(
                name: "BannerColor",
                schema: "wgg",
                table: "Users");
        }
    }
}
