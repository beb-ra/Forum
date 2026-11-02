using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForumWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserImageAndDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "users");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "users");
        }
    }
}
