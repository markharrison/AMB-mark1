using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public UsersController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        /// <summary>
        /// Gets all users
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            var (users, error) = await _expenseService.GetAllUsersAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(users);
        }

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var (user, error) = await _expenseService.GetUserByIdAsync(id);
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        /// <summary>
        /// Gets all managers
        /// </summary>
        [HttpGet("managers")]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<User>>> GetManagers()
        {
            var (managers, error) = await _expenseService.GetManagersAsync();
            if (error != null)
            {
                Response.Headers.Append("X-Error-Message", error);
            }
            return Ok(managers);
        }
    }
}
