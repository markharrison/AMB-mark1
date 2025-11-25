namespace ExpenseManagement.Models
{
    public class DummyData
    {
        public static List<Expense> GetDummyExpenses()
        {
            return new List<Expense>
            {
                new Expense
                {
                    ExpenseId = 1,
                    UserId = 1,
                    UserName = "Alice Example",
                    UserEmail = "alice@example.co.uk",
                    CategoryId = 1,
                    CategoryName = "Travel",
                    StatusId = 2,
                    StatusName = "Submitted",
                    AmountMinor = 12000,
                    AmountGBP = 120.00m,
                    Currency = "GBP",
                    ExpenseDate = DateTime.Now.AddDays(-10),
                    Description = "Taxi from airport to client site",
                    SubmittedAt = DateTime.Now.AddDays(-9),
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new Expense
                {
                    ExpenseId = 2,
                    UserId = 1,
                    UserName = "Alice Example",
                    UserEmail = "alice@example.co.uk",
                    CategoryId = 2,
                    CategoryName = "Meals",
                    StatusId = 3,
                    StatusName = "Approved",
                    AmountMinor = 6900,
                    AmountGBP = 69.00m,
                    Currency = "GBP",
                    ExpenseDate = DateTime.Now.AddDays(-30),
                    Description = "Client lunch meeting",
                    SubmittedAt = DateTime.Now.AddDays(-29),
                    ReviewedBy = 2,
                    ReviewerName = "Bob Manager",
                    ReviewedAt = DateTime.Now.AddDays(-28),
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new Expense
                {
                    ExpenseId = 3,
                    UserId = 1,
                    UserName = "Alice Example",
                    UserEmail = "alice@example.co.uk",
                    CategoryId = 3,
                    CategoryName = "Supplies",
                    StatusId = 3,
                    StatusName = "Approved",
                    AmountMinor = 9950,
                    AmountGBP = 99.50m,
                    Currency = "GBP",
                    ExpenseDate = DateTime.Now.AddDays(-60),
                    Description = "Office stationery",
                    SubmittedAt = DateTime.Now.AddDays(-59),
                    ReviewedBy = 2,
                    ReviewerName = "Bob Manager",
                    ReviewedAt = DateTime.Now.AddDays(-58),
                    CreatedAt = DateTime.Now.AddDays(-60)
                },
                new Expense
                {
                    ExpenseId = 4,
                    UserId = 1,
                    UserName = "Alice Example",
                    UserEmail = "alice@example.co.uk",
                    CategoryId = 1,
                    CategoryName = "Travel",
                    StatusId = 3,
                    StatusName = "Approved",
                    AmountMinor = 1920,
                    AmountGBP = 19.20m,
                    Currency = "GBP",
                    ExpenseDate = DateTime.Now.AddDays(-90),
                    Description = "Transport to meeting",
                    SubmittedAt = DateTime.Now.AddDays(-89),
                    ReviewedBy = 2,
                    ReviewerName = "Bob Manager",
                    ReviewedAt = DateTime.Now.AddDays(-88),
                    CreatedAt = DateTime.Now.AddDays(-90)
                }
            };
        }

        public static List<Category> GetDummyCategories()
        {
            return new List<Category>
            {
                new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
                new Category { CategoryId = 2, CategoryName = "Meals", IsActive = true },
                new Category { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
                new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
                new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
            };
        }

        public static List<ExpenseStatus> GetDummyStatuses()
        {
            return new List<ExpenseStatus>
            {
                new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
                new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
                new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
                new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
            };
        }

        public static List<User> GetDummyUsers()
        {
            return new List<User>
            {
                new User
                {
                    UserId = 1,
                    UserName = "Alice Example",
                    Email = "alice@example.co.uk",
                    RoleId = 1,
                    RoleName = "Employee",
                    ManagerId = 2,
                    ManagerName = "Bob Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-6)
                },
                new User
                {
                    UserId = 2,
                    UserName = "Bob Manager",
                    Email = "bob.manager@example.co.uk",
                    RoleId = 2,
                    RoleName = "Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-12)
                }
            };
        }

        public static ExpenseSummary GetDummySummary()
        {
            return new ExpenseSummary
            {
                TotalExpenses = 4,
                PendingApprovals = 1,
                ApprovedAmountGBP = 187.70m,
                ApprovedCount = 3
            };
        }
    }
}
