using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services
{
    public interface IExpenseService
    {
        Task<(List<Expense> Expenses, string? Error)> GetAllExpensesAsync();
        Task<(Expense? Expense, string? Error)> GetExpenseByIdAsync(int expenseId);
        Task<(List<Expense> Expenses, string? Error)> GetExpensesByStatusAsync(string statusName);
        Task<(List<Expense> Expenses, string? Error)> GetExpensesByUserAsync(int userId);
        Task<(List<Expense> Expenses, string? Error)> GetPendingExpensesAsync();
        Task<(int ExpenseId, string? Error)> CreateExpenseAsync(ExpenseCreateModel expense);
        Task<(bool Success, string? Error)> UpdateExpenseAsync(ExpenseUpdateModel expense);
        Task<(bool Success, string? Error)> SubmitExpenseAsync(int expenseId);
        Task<(bool Success, string? Error)> ApproveExpenseAsync(int expenseId, int reviewerId);
        Task<(bool Success, string? Error)> RejectExpenseAsync(int expenseId, int reviewerId);
        Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId);
        Task<(List<Category> Categories, string? Error)> GetAllCategoriesAsync();
        Task<(List<ExpenseStatus> Statuses, string? Error)> GetAllStatusesAsync();
        Task<(List<User> Users, string? Error)> GetAllUsersAsync();
        Task<(User? User, string? Error)> GetUserByIdAsync(int userId);
        Task<(List<User> Managers, string? Error)> GetManagersAsync();
        Task<(ExpenseSummary Summary, string? Error)> GetExpenseSummaryAsync();
        Task<(List<Expense> Expenses, string? Error)> SearchExpensesAsync(ExpenseSearchModel search);
    }

    public class ExpenseService : IExpenseService
    {
        private readonly string _connectionString;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        private async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private string GetDetailedError(Exception ex, string operation, string file, int line)
        {
            var message = $"Error in {operation} at {file}:{line}. ";
            
            if (ex is SqlException sqlEx)
            {
                message += $"SQL Error {sqlEx.Number}: {sqlEx.Message}. ";
                
                if (sqlEx.Message.Contains("Login failed") || sqlEx.Message.Contains("managed identity"))
                {
                    message += "MANAGED IDENTITY FIX: Ensure the managed identity has been granted access to the database. " +
                              "Run the script.sql file to create the user and assign roles. " +
                              "Verify AZURE_CLIENT_ID environment variable is set to the managed identity client ID.";
                }
            }
            else
            {
                message += ex.Message;
            }
            
            return message;
        }

        public async Task<(List<Expense> Expenses, string? Error)> GetAllExpensesAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetAllExpenses", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var expenses = new List<Expense>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(MapExpenseFromReader(reader));
                }
                return (expenses, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all expenses");
                var error = GetDetailedError(ex, "GetAllExpensesAsync", "ExpenseService.cs", 85);
                return (DummyData.GetDummyExpenses(), error);
            }
        }

        public async Task<(Expense? Expense, string? Error)> GetExpenseByIdAsync(int expenseId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetExpenseById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@ExpenseId", expenseId);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return (MapExpenseFromReader(reader), null);
                }
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense by ID {ExpenseId}", expenseId);
                var error = GetDetailedError(ex, "GetExpenseByIdAsync", "ExpenseService.cs", 109);
                var dummyExpense = DummyData.GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
                return (dummyExpense, error);
            }
        }

        public async Task<(List<Expense> Expenses, string? Error)> GetExpensesByStatusAsync(string statusName)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetExpensesByStatus", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@StatusName", statusName);

                var expenses = new List<Expense>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(MapExpenseFromReader(reader));
                }
                return (expenses, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expenses by status {StatusName}", statusName);
                var error = GetDetailedError(ex, "GetExpensesByStatusAsync", "ExpenseService.cs", 137);
                var dummyExpenses = DummyData.GetDummyExpenses().Where(e => e.StatusName == statusName).ToList();
                return (dummyExpenses, error);
            }
        }

        public async Task<(List<Expense> Expenses, string? Error)> GetExpensesByUserAsync(int userId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetExpensesByUser", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserId", userId);

                var expenses = new List<Expense>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(MapExpenseFromReader(reader));
                }
                return (expenses, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expenses by user {UserId}", userId);
                var error = GetDetailedError(ex, "GetExpensesByUserAsync", "ExpenseService.cs", 165);
                var dummyExpenses = DummyData.GetDummyExpenses().Where(e => e.UserId == userId).ToList();
                return (dummyExpenses, error);
            }
        }

        public async Task<(List<Expense> Expenses, string? Error)> GetPendingExpensesAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetPendingExpenses", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var expenses = new List<Expense>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(MapExpenseFromReader(reader));
                }
                return (expenses, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending expenses");
                var error = GetDetailedError(ex, "GetPendingExpensesAsync", "ExpenseService.cs", 191);
                var dummyExpenses = DummyData.GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();
                return (dummyExpenses, error);
            }
        }

        public async Task<(int ExpenseId, string? Error)> CreateExpenseAsync(ExpenseCreateModel expense)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_CreateExpense", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserId", expense.UserId);
                command.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
                command.Parameters.AddWithValue("@AmountMinor", (int)(expense.Amount * 100));
                command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                command.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);

                var result = await command.ExecuteScalarAsync();
                return (Convert.ToInt32(result), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                var error = GetDetailedError(ex, "CreateExpenseAsync", "ExpenseService.cs", 218);
                return (0, error);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateExpenseAsync(ExpenseUpdateModel expense)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_UpdateExpense", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@ExpenseId", expense.ExpenseId);
                command.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
                command.Parameters.AddWithValue("@AmountMinor", (int)(expense.Amount * 100));
                command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                command.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);

                var result = await command.ExecuteScalarAsync();
                return (Convert.ToInt32(result) > 0, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense {ExpenseId}", expense.ExpenseId);
                var error = GetDetailedError(ex, "UpdateExpenseAsync", "ExpenseService.cs", 244);
                return (false, error);
            }
        }

        public async Task<(bool Success, string? Error)> SubmitExpenseAsync(int expenseId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_SubmitExpense", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@ExpenseId", expenseId);

                var result = await command.ExecuteScalarAsync();
                return (Convert.ToInt32(result) > 0, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
                var error = GetDetailedError(ex, "SubmitExpenseAsync", "ExpenseService.cs", 266);
                return (false, error);
            }
        }

        public async Task<(bool Success, string? Error)> ApproveExpenseAsync(int expenseId, int reviewerId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_ApproveExpense", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@ExpenseId", expenseId);
                command.Parameters.AddWithValue("@ReviewerId", reviewerId);

                var result = await command.ExecuteScalarAsync();
                return (Convert.ToInt32(result) > 0, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
                var error = GetDetailedError(ex, "ApproveExpenseAsync", "ExpenseService.cs", 289);
                return (false, error);
            }
        }

        public async Task<(bool Success, string? Error)> RejectExpenseAsync(int expenseId, int reviewerId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_RejectExpense", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@ExpenseId", expenseId);
                command.Parameters.AddWithValue("@ReviewerId", reviewerId);

                var result = await command.ExecuteScalarAsync();
                return (Convert.ToInt32(result) > 0, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
                var error = GetDetailedError(ex, "RejectExpenseAsync", "ExpenseService.cs", 312);
                return (false, error);
            }
        }

        public async Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_DeleteExpense", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@ExpenseId", expenseId);

                var result = await command.ExecuteScalarAsync();
                return (Convert.ToInt32(result) > 0, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
                var error = GetDetailedError(ex, "DeleteExpenseAsync", "ExpenseService.cs", 334);
                return (false, error);
            }
        }

        public async Task<(List<Category> Categories, string? Error)> GetAllCategoriesAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetAllCategories", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var categories = new List<Category>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new Category
                    {
                        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                        CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    });
                }
                return (categories, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                var error = GetDetailedError(ex, "GetAllCategoriesAsync", "ExpenseService.cs", 364);
                return (DummyData.GetDummyCategories(), error);
            }
        }

        public async Task<(List<ExpenseStatus> Statuses, string? Error)> GetAllStatusesAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetAllStatuses", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var statuses = new List<ExpenseStatus>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    statuses.Add(new ExpenseStatus
                    {
                        StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                        StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                    });
                }
                return (statuses, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all statuses");
                var error = GetDetailedError(ex, "GetAllStatusesAsync", "ExpenseService.cs", 394);
                return (DummyData.GetDummyStatuses(), error);
            }
        }

        public async Task<(List<User> Users, string? Error)> GetAllUsersAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetAllUsers", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var users = new List<User>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(MapUserFromReader(reader));
                }
                return (users, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                var error = GetDetailedError(ex, "GetAllUsersAsync", "ExpenseService.cs", 420);
                return (DummyData.GetDummyUsers(), error);
            }
        }

        public async Task<(User? User, string? Error)> GetUserByIdAsync(int userId)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetUserById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return (MapUserFromReader(reader), null);
                }
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
                var error = GetDetailedError(ex, "GetUserByIdAsync", "ExpenseService.cs", 446);
                var dummyUser = DummyData.GetDummyUsers().FirstOrDefault(u => u.UserId == userId);
                return (dummyUser, error);
            }
        }

        public async Task<(List<User> Managers, string? Error)> GetManagersAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetManagers", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var managers = new List<User>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    managers.Add(new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                        Email = reader.GetString(reader.GetOrdinal("Email"))
                    });
                }
                return (managers, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting managers");
                var error = GetDetailedError(ex, "GetManagersAsync", "ExpenseService.cs", 478);
                var dummyManagers = DummyData.GetDummyUsers().Where(u => u.RoleName == "Manager").ToList();
                return (dummyManagers, error);
            }
        }

        public async Task<(ExpenseSummary Summary, string? Error)> GetExpenseSummaryAsync()
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_GetExpenseSummary", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var approvedAmountMinor = reader.GetInt32(reader.GetOrdinal("ApprovedAmountMinor"));
                    return (new ExpenseSummary
                    {
                        TotalExpenses = reader.GetInt32(reader.GetOrdinal("TotalExpenses")),
                        PendingApprovals = reader.GetInt32(reader.GetOrdinal("PendingApprovals")),
                        ApprovedAmountGBP = approvedAmountMinor / 100.0m,
                        ApprovedCount = reader.GetInt32(reader.GetOrdinal("ApprovedCount"))
                    }, null);
                }
                return (DummyData.GetDummySummary(), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense summary");
                var error = GetDetailedError(ex, "GetExpenseSummaryAsync", "ExpenseService.cs", 511);
                return (DummyData.GetDummySummary(), error);
            }
        }

        public async Task<(List<Expense> Expenses, string? Error)> SearchExpensesAsync(ExpenseSearchModel search)
        {
            try
            {
                await using var connection = await GetConnectionAsync();
                await using var command = new SqlCommand("usp_SearchExpenses", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@SearchTerm", (object?)search.SearchTerm ?? DBNull.Value);
                command.Parameters.AddWithValue("@CategoryId", (object?)search.CategoryId ?? DBNull.Value);
                command.Parameters.AddWithValue("@StatusId", (object?)search.StatusId ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartDate", (object?)search.StartDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@EndDate", (object?)search.EndDate ?? DBNull.Value);

                var expenses = new List<Expense>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(MapExpenseFromReader(reader));
                }
                return (expenses, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching expenses");
                var error = GetDetailedError(ex, "SearchExpensesAsync", "ExpenseService.cs", 542);
                return (DummyData.GetDummyExpenses(), error);
            }
        }

        private static Expense MapExpenseFromReader(SqlDataReader reader)
        {
            return new Expense
            {
                ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                UserEmail = reader.GetString(reader.GetOrdinal("UserEmail")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
                AmountGBP = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
                Currency = reader.GetString(reader.GetOrdinal("Currency")),
                ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
                SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
                ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
                ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        private static User MapUserFromReader(SqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
