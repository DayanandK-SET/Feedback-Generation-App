using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feedback_Generation_App.Migrations
{
    /// <inheritdoc />
    public partial class AddedSurveyExpiredate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpireAt",
                table: "Surveys",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "Surveys");
        }
    }
}
