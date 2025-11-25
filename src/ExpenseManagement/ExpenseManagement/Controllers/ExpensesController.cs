using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly ILogger<ExpensesController> _logger;

        public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
        {
            _expenseService = expenseService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all expenses
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Expense>>> GetAll()
        {
            var (expenses, error) = await _expenseService.GetAllExpensesAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(expenses);
        }

        /// <summary>
        /// Gets an expense by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Expense), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Expense>> GetById(int id)
        {
            var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            if (expense == null)
            {
                return NotFound();
            }
            return Ok(expense);
        }

        /// <summary>
        /// Gets expenses by status
        /// </summary>
        [HttpGet("status/{statusName}")]
        [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Expense>>> GetByStatus(string statusName)
        {
            var (expenses, error) = await _expenseService.GetExpensesByStatusAsync(statusName);
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(expenses);
        }

        /// <summary>
        /// Gets expenses by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Expense>>> GetByUser(int userId)
        {
            var (expenses, error) = await _expenseService.GetExpensesByUserAsync(userId);
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(expenses);
        }

        /// <summary>
        /// Gets pending expenses for approval
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Expense>>> GetPending()
        {
            var (expenses, error) = await _expenseService.GetPendingExpensesAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(expenses);
        }

        /// <summary>
        /// Gets expense summary statistics
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ExpenseSummary), StatusCodes.Status200OK)]
        public async Task<ActionResult<ExpenseSummary>> GetSummary()
        {
            var (summary, error) = await _expenseService.GetExpenseSummaryAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(summary);
        }

        /// <summary>
        /// Searches expenses
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Expense>>> Search([FromBody] ExpenseSearchModel search)
        {
            var (expenses, error) = await _expenseService.SearchExpensesAsync(search);
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(expenses);
        }

        /// <summary>
        /// Creates a new expense
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] ExpenseCreateModel expense)
        {
            var (expenseId, error) = await _expenseService.CreateExpenseAsync(expense);
            if (error != null)
            {
                return BadRequest(new { error });
            }
            return CreatedAtAction(nameof(GetById), new { id = expenseId }, new { expenseId });
        }

        /// <summary>
        /// Updates an expense
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Update(int id, [FromBody] ExpenseUpdateModel expense)
        {
            expense.ExpenseId = id;
            var (success, error) = await _expenseService.UpdateExpenseAsync(expense);
            if (error != null)
            {
                return BadRequest(new { error });
            }
            return NoContent();
        }

        /// <summary>
        /// Submits an expense for approval
        /// </summary>
        [HttpPost("{id}/submit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Submit(int id)
        {
            var (success, error) = await _expenseService.SubmitExpenseAsync(id);
            if (error != null)
            {
                return BadRequest(new { error });
            }
            return NoContent();
        }

        /// <summary>
        /// Approves an expense
        /// </summary>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Approve(int id, [FromQuery] int reviewerId = 2)
        {
            var (success, error) = await _expenseService.ApproveExpenseAsync(id, reviewerId);
            if (error != null)
            {
                return BadRequest(new { error });
            }
            return NoContent();
        }

        /// <summary>
        /// Rejects an expense
        /// </summary>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Reject(int id, [FromQuery] int reviewerId = 2)
        {
            var (success, error) = await _expenseService.RejectExpenseAsync(id, reviewerId);
            if (error != null)
            {
                return BadRequest(new { error });
            }
            return NoContent();
        }

        /// <summary>
        /// Deletes an expense
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Delete(int id)
        {
            var (success, error) = await _expenseService.DeleteExpenseAsync(id);
            if (error != null)
            {
                return BadRequest(new { error });
            }
            return NoContent();
        }
    }
}
