#nullable enable
using System;

namespace JiraTools.Configuration
{
    /// <summary>
    /// Represents a comment on a project
    /// </summary>
    public class ProjectComment
    {
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;

        public ProjectComment()
        {
        }

        public ProjectComment(string text, string author, DateTime? date = null)
        {
            Text = text;
            Author = author;
            Date = date ?? DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Date:yyyy-MM-dd}] {Text}";
        }
    }
}
