using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages
{
    public class ExpensesModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly ILogger<ExpensesModel> _logger;

        public ExpensesModel(IExpenseService expenseService, ILogger<ExpensesModel> logger)
        {
            _expenseService = expenseService;
            _logger = logger;
        }

        public List<Expense> Expenses { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<ExpenseStatus> Statuses { get; set; } = new();
        public string? ErrorMessage { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? StatusId { get; set; }

        public async Task OnGetAsync()
        {
            var (categories, _) = await _expenseService.GetAllCategoriesAsync();
            Categories = categories;
            
            var (statuses, _) = await _expenseService.GetAllStatusesAsync();
            Statuses = statuses;
            
            if (!string.IsNullOrEmpty(SearchTerm) || CategoryId.HasValue || StatusId.HasValue)
            {
                var search = new ExpenseSearchModel
                {
                    SearchTerm = SearchTerm,
                    CategoryId = CategoryId,
                    StatusId = StatusId
                };
                var (expenses, error) = await _expenseService.SearchExpensesAsync(search);
                Expenses = expenses;
                ErrorMessage = error;
            }
            else
            {
                var (expenses, error) = await _expenseService.GetAllExpensesAsync();
                Expenses = expenses;
                ErrorMessage = error;
            }
        }

        public async Task<IActionResult> OnPostSubmitAsync(int id)
        {
            await _expenseService.SubmitExpenseAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _expenseService.DeleteExpenseAsync(id);
            return RedirectToPage();
        }
    }
}
