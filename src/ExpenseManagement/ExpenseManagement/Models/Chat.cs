namespace ExpenseManagement.Models
{
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage> History { get; set; } = new();
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }
    }
}
