using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForumWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class CommentContentNotRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "comments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "chk_comment_has_content",
                table: "comments",
                sql: "content IS NOT NULL OR image_url IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_comment_has_content",
                table: "comments");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "comments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
