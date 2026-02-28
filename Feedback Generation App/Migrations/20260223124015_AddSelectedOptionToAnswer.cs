using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feedback_Generation_App.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedOptionToAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "SelectedOptionId",
                table: "Answers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_SelectedOptionId",
                table: "Answers",
                column: "SelectedOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_QuestionOptions_SelectedOptionId",
                table: "Answers",
                column: "SelectedOptionId",
                principalTable: "QuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_QuestionOptions_SelectedOptionId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_SelectedOptionId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "SelectedOptionId",
                table: "Answers");

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
