using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public CategoriesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        /// <summary>
        /// Gets all expense categories
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Category>>> GetAll()
        {
            var (categories, error) = await _expenseService.GetAllCategoriesAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(categories);
        }
    }
}
