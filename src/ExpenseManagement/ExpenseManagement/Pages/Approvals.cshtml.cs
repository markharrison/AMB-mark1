using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages
{
    public class ApprovalsModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly ILogger<ApprovalsModel> _logger;

        public ApprovalsModel(IExpenseService expenseService, ILogger<ApprovalsModel> logger)
        {
            _expenseService = expenseService;
            _logger = logger;
        }

        public List<Expense> PendingExpenses { get; set; } = new();
        public string? ErrorMessage { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var (expenses, error) = await _expenseService.GetPendingExpensesAsync();
            
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                expenses = expenses.Where(e => 
                    (e.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    e.UserName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    e.CategoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            
            PendingExpenses = expenses;
            ErrorMessage = error;
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            // Using default manager ID (2 = Bob Manager)
            await _expenseService.ApproveExpenseAsync(id, 2);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            await _expenseService.RejectExpenseAsync(id, 2);
            return RedirectToPage();
        }
    }
}
