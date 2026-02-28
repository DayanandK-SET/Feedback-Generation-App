using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feedback_Generation_App.Migrations
{
    /// <inheritdoc />
    public partial class PreventingDuplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseToken",
                table: "Responses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseToken",
                table: "Responses");
        }
    }
}
