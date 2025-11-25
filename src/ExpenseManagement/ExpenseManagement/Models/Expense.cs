namespace ExpenseManagement.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int AmountMinor { get; set; }
        public decimal AmountGBP { get; set; }
        public string Currency { get; set; } = "GBP";
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
        public string? ReceiptFile { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExpenseCreateModel
    {
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
    }

    public class ExpenseUpdateModel
    {
        public int ExpenseId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ExpenseStatus
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExpenseSummary
    {
        public int TotalExpenses { get; set; }
        public int PendingApprovals { get; set; }
        public decimal ApprovedAmountGBP { get; set; }
        public int ApprovedCount { get; set; }
    }

    public class ExpenseSearchModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? StatusId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
