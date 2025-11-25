using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
        {
            _expenseService = expenseService;
            _logger = logger;
        }

        public ExpenseSummary Summary { get; set; } = new();
        public List<Expense> RecentExpenses { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            var (summary, summaryError) = await _expenseService.GetExpenseSummaryAsync();
            Summary = summary;
            
            var (expenses, expensesError) = await _expenseService.GetAllExpensesAsync();
            RecentExpenses = expenses.Take(10).ToList();
            
            ErrorMessage = summaryError ?? expensesError;
        }
    }
}
