using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages
{
    public class NewExpenseModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly ILogger<NewExpenseModel> _logger;

        public NewExpenseModel(IExpenseService expenseService, ILogger<NewExpenseModel> logger)
        {
            _expenseService = expenseService;
            _logger = logger;
        }

        [BindProperty]
        public ExpenseCreateModel Expense { get; set; } = new()
        {
            UserId = 1,
            ExpenseDate = DateTime.Today
        };

        public List<Category> Categories { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var (expenseId, error) = await _expenseService.CreateExpenseAsync(Expense);
            
            if (error != null)
            {
                ErrorMessage = error;
                await LoadDataAsync();
                return Page();
            }

            SuccessMessage = $"Expense created successfully! (ID: {expenseId})";
            Expense = new ExpenseCreateModel
            {
                UserId = 1,
                ExpenseDate = DateTime.Today
            };
            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            var (categories, _) = await _expenseService.GetAllCategoriesAsync();
            Categories = categories;
            
            var (users, _) = await _expenseService.GetAllUsersAsync();
            Users = users;
        }
    }
}
