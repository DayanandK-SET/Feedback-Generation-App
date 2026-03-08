using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feedback_Generation_App.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyResponseLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxResponses",
                table: "Surveys",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxResponses",
                table: "Surveys");
        }
    }
}
