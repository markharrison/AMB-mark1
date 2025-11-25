using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class StatusesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public StatusesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        /// <summary>
        /// Gets all expense statuses
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExpenseStatus>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ExpenseStatus>>> GetAll()
        {
            var (statuses, error) = await _expenseService.GetAllStatusesAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(statuses);
        }
    }
}
