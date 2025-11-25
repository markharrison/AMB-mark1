using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using OpenAI.Chat;
using System.Text.Json;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace ExpenseManagement.Services
{
    public interface IChatService
    {
        Task<ChatResponse> SendMessageAsync(ChatRequest request);
        bool IsEnabled { get; }
    }

    public class ChatService : IChatService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;
        private readonly IExpenseService _expenseService;
        private readonly bool _isEnabled;
        private readonly string? _endpoint;
        private readonly string? _deploymentName;

        public bool IsEnabled => _isEnabled;

        public ChatService(
            IConfiguration configuration, 
            ILogger<ChatService> logger,
            IExpenseService expenseService)
        {
            _configuration = configuration;
            _logger = logger;
            _expenseService = expenseService;
            
            _endpoint = configuration["OpenAI:Endpoint"];
            _deploymentName = configuration["OpenAI:DeploymentName"];
            _isEnabled = configuration.GetValue<bool>("GenAI:Enabled") && 
                         !string.IsNullOrEmpty(_endpoint) && 
                         !string.IsNullOrEmpty(_deploymentName);

            if (!_isEnabled)
            {
                _logger.LogWarning("GenAI services are not enabled. Chat will return dummy responses.");
            }
        }

        public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
        {
            if (!_isEnabled)
            {
                return new ChatResponse
                {
                    Message = "GenAI services are not deployed. To enable the AI chat functionality, please run the deploy-with-chat.sh script instead of deploy.sh. " +
                             "This will deploy the Azure OpenAI and AI Search resources needed for the chat feature. " +
                             "Until then, you can still use all the expense management features through the UI.",
                    IsError = false
                };
            }

            try
            {
                var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
                Azure.Core.TokenCredential credential;

                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                var client = new AzureOpenAIClient(new Uri(_endpoint!), credential);
                var chatClient = client.GetChatClient(_deploymentName!);

                var messages = new List<OpenAIChatMessage>
                {
                    new SystemChatMessage(GetSystemPrompt())
                };

                foreach (var historyMessage in request.History)
                {
                    if (historyMessage.Role == "user")
                        messages.Add(new UserChatMessage(historyMessage.Content));
                    else if (historyMessage.Role == "assistant")
                        messages.Add(new AssistantChatMessage(historyMessage.Content));
                }

                messages.Add(new UserChatMessage(request.Message));

                var tools = GetChatTools();
                var options = new ChatCompletionOptions();
                foreach (var tool in tools)
                {
                    options.Tools.Add(tool);
                }

                var response = await chatClient.CompleteChatAsync(messages, options);
                var assistantMessage = response.Value;

                while (assistantMessage.FinishReason == ChatFinishReason.ToolCalls)
                {
                    messages.Add(new AssistantChatMessage(assistantMessage));

                    foreach (var toolCall in assistantMessage.ToolCalls)
                    {
                        var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                    }

                    response = await chatClient.CompleteChatAsync(messages, options);
                    assistantMessage = response.Value;
                }

                return new ChatResponse
                {
                    Message = assistantMessage.Content[0].Text,
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat service");
                return new ChatResponse
                {
                    Message = $"An error occurred: {ex.Message}",
                    IsError = true
                };
            }
        }

        private string GetSystemPrompt()
        {
            return @"You are an AI assistant for the Expense Management System. You can help users with:
- Viewing expenses and their status
- Creating new expenses
- Submitting expenses for approval
- Approving or rejecting expenses (for managers)
- Getting expense summaries and statistics

You have access to the following functions to interact with the expense system:
- get_all_expenses: Retrieves all expenses
- get_expenses_by_status: Gets expenses filtered by status (Draft, Submitted, Approved, Rejected)
- get_pending_expenses: Gets expenses waiting for approval
- get_expense_summary: Gets summary statistics
- create_expense: Creates a new expense
- submit_expense: Submits an expense for approval
- approve_expense: Approves an expense (requires manager role)
- reject_expense: Rejects an expense (requires manager role)

When listing expenses or data, format the response nicely with:
- Use numbered lists (1., 2., etc.) for listing items
- Use bullet points (- or *) for properties
- Use **bold** for emphasis on important values like amounts and status
- Include relevant details like date, category, amount, and status

Always be helpful and provide clear responses. If you need to perform an action, use the appropriate function.";
        }

        private List<ChatTool> GetChatTools()
        {
            return new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_all_expenses",
                    "Retrieves all expenses from the system with details including amount, category, status, and description"
                ),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_status",
                    "Gets expenses filtered by status",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                description = "The status to filter by: Draft, Submitted, Approved, or Rejected",
                                @enum = new[] { "Draft", "Submitted", "Approved", "Rejected" }
                            }
                        },
                        required = new[] { "status" }
                    })
                ),
                ChatTool.CreateFunctionTool(
                    "get_pending_expenses",
                    "Gets all expenses that are waiting for approval (status = Submitted)"
                ),
                ChatTool.CreateFunctionTool(
                    "get_expense_summary",
                    "Gets summary statistics including total expenses, pending approvals, and approved amounts"
                ),
                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Creates a new expense",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            userId = new { type = "integer", description = "The user ID creating the expense (default 1)" },
                            categoryId = new { type = "integer", description = "Category ID: 1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other" },
                            amount = new { type = "number", description = "Amount in GBP (e.g., 25.50)" },
                            expenseDate = new { type = "string", description = "Date of expense in YYYY-MM-DD format" },
                            description = new { type = "string", description = "Description of the expense" }
                        },
                        required = new[] { "categoryId", "amount", "expenseDate" }
                    })
                ),
                ChatTool.CreateFunctionTool(
                    "submit_expense",
                    "Submits an expense for approval",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "The ID of the expense to submit" }
                        },
                        required = new[] { "expenseId" }
                    })
                ),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves an expense (manager action)",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "The ID of the expense to approve" },
                            reviewerId = new { type = "integer", description = "The manager's user ID (default 2)" }
                        },
                        required = new[] { "expenseId" }
                    })
                ),
                ChatTool.CreateFunctionTool(
                    "reject_expense",
                    "Rejects an expense (manager action)",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "The ID of the expense to reject" },
                            reviewerId = new { type = "integer", description = "The manager's user ID (default 2)" }
                        },
                        required = new[] { "expenseId" }
                    })
                )
            };
        }

        private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
        {
            try
            {
                var args = JsonDocument.Parse(arguments);
                
                switch (functionName)
                {
                    case "get_all_expenses":
                        var (expenses, _) = await _expenseService.GetAllExpensesAsync();
                        return JsonSerializer.Serialize(expenses.Select(e => new
                        {
                            e.ExpenseId,
                            e.UserName,
                            e.CategoryName,
                            AmountGBP = $"£{e.AmountGBP:F2}",
                            e.ExpenseDate,
                            e.StatusName,
                            e.Description
                        }));

                    case "get_expenses_by_status":
                        var status = args.RootElement.GetProperty("status").GetString()!;
                        var (statusExpenses, _) = await _expenseService.GetExpensesByStatusAsync(status);
                        return JsonSerializer.Serialize(statusExpenses.Select(e => new
                        {
                            e.ExpenseId,
                            e.UserName,
                            e.CategoryName,
                            AmountGBP = $"£{e.AmountGBP:F2}",
                            e.ExpenseDate,
                            e.StatusName,
                            e.Description
                        }));

                    case "get_pending_expenses":
                        var (pendingExpenses, _) = await _expenseService.GetPendingExpensesAsync();
                        return JsonSerializer.Serialize(pendingExpenses.Select(e => new
                        {
                            e.ExpenseId,
                            e.UserName,
                            e.CategoryName,
                            AmountGBP = $"£{e.AmountGBP:F2}",
                            e.ExpenseDate,
                            e.Description
                        }));

                    case "get_expense_summary":
                        var (summary, _) = await _expenseService.GetExpenseSummaryAsync();
                        return JsonSerializer.Serialize(new
                        {
                            summary.TotalExpenses,
                            summary.PendingApprovals,
                            ApprovedAmount = $"£{summary.ApprovedAmountGBP:F2}",
                            summary.ApprovedCount
                        });

                    case "create_expense":
                        var createModel = new ExpenseCreateModel
                        {
                            UserId = args.RootElement.TryGetProperty("userId", out var userId) ? userId.GetInt32() : 1,
                            CategoryId = args.RootElement.GetProperty("categoryId").GetInt32(),
                            Amount = args.RootElement.GetProperty("amount").GetDecimal(),
                            ExpenseDate = DateTime.Parse(args.RootElement.GetProperty("expenseDate").GetString()!),
                            Description = args.RootElement.TryGetProperty("description", out var desc) ? desc.GetString() : null
                        };
                        var (expenseId, createError) = await _expenseService.CreateExpenseAsync(createModel);
                        if (createError != null)
                            return JsonSerializer.Serialize(new { error = createError });
                        return JsonSerializer.Serialize(new { success = true, expenseId });

                    case "submit_expense":
                        var submitId = args.RootElement.GetProperty("expenseId").GetInt32();
                        var (submitSuccess, submitError) = await _expenseService.SubmitExpenseAsync(submitId);
                        if (submitError != null)
                            return JsonSerializer.Serialize(new { error = submitError });
                        return JsonSerializer.Serialize(new { success = submitSuccess });

                    case "approve_expense":
                        var approveId = args.RootElement.GetProperty("expenseId").GetInt32();
                        var approveReviewerId = args.RootElement.TryGetProperty("reviewerId", out var appRevId) ? appRevId.GetInt32() : 2;
                        var (approveSuccess, approveError) = await _expenseService.ApproveExpenseAsync(approveId, approveReviewerId);
                        if (approveError != null)
                            return JsonSerializer.Serialize(new { error = approveError });
                        return JsonSerializer.Serialize(new { success = approveSuccess });

                    case "reject_expense":
                        var rejectId = args.RootElement.GetProperty("expenseId").GetInt32();
                        var rejectReviewerId = args.RootElement.TryGetProperty("reviewerId", out var rejRevId) ? rejRevId.GetInt32() : 2;
                        var (rejectSuccess, rejectError) = await _expenseService.RejectExpenseAsync(rejectId, rejectReviewerId);
                        if (rejectError != null)
                            return JsonSerializer.Serialize(new { error = rejectError });
                        return JsonSerializer.Serialize(new { success = rejectSuccess });

                    default:
                        return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }
    }
}
