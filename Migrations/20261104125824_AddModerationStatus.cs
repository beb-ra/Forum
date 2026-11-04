using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForumWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_posts_status",
                table: "posts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_comments_status",
                table: "comments",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_status",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_comments_status",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "status",
                table: "comments");
        }
    }
}
