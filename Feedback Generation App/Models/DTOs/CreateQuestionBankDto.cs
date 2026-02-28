using Feedback_Generation_App.Models;

public class CreateQuestionBankDto
{
    public string Text { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public List<string>? Options { get; set; }
}