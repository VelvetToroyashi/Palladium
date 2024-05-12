using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookmarker.Migrations
{
    /// <inheritdoc />
    public partial class ReaddIDProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ID",
                table: "Bookmarks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ID",
                table: "Bookmarks");
        }
    }
}
