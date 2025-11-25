# Azure Services Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Resource Group (UK South)                              │
│                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────┐   │
│  │                         User-Assigned Managed Identity                    │   │
│  │                              (mid-expensemgmt-xxx)                        │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                │                    │                     │                      │
│                │                    │                     │                      │
│                ▼                    ▼                     ▼                      │
│  ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────────────┐     │
│  │   App Service    │   │    Azure SQL     │   │     Azure OpenAI         │     │
│  │ (asp-expensemgmt)│   │ (sql-expensemgmt)│   │   (Sweden Central)       │     │
│  │                  │   │                  │   │                          │     │
│  │ • ASP.NET 8.0    │   │ • Database:      │   │ • GPT-4o Model           │     │
│  │ • Razor Pages    │   │   expensedb      │   │ • Capacity: 8            │     │
│  │ • REST APIs      │   │ • Entra ID Auth  │   │ • S0 SKU                 │     │
│  │ • Chat UI        │   │ • Basic Tier     │   │                          │     │
│  │ • S1 SKU         │   │                  │   │ ┌──────────────────────┐ │     │
│  │                  │   │                  │   │ │    AI Search         │ │     │
│  └────────┬─────────┘   └────────┬─────────┘   │ │ (search-expensemgmt) │ │     │
│           │                      │             │ │ • Basic Tier         │ │     │
│           │                      │             │ └──────────────────────┘ │     │
│           │                      │             └──────────────────────────┘     │
│           │                      │                                              │
│           └──────────────────────┴──────────────────────────────────────────────│
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

                    Data Flow
                    =========

┌──────────┐     HTTPS     ┌──────────────────┐
│  User    │─────────────▶ │    App Service   │
│ Browser  │               │                  │
└──────────┘               └────────┬─────────┘
                                    │
                                    │ Managed Identity
                                    ▼
           ┌────────────────────────┼────────────────────────┐
           │                        │                        │
           ▼                        ▼                        ▼
    ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
    │  Azure SQL   │      │Azure OpenAI  │      │  AI Search   │
    │              │      │   (GPT-4o)   │      │              │
    └──────────────┘      └──────────────┘      └──────────────┘


                    Deployment Files
                    ================

    ┌─────────────────┐          ┌─────────────────────┐
    │   deploy.sh     │          │ deploy-with-chat.sh │
    │                 │          │                     │
    │ Deploys:        │          │ Deploys:            │
    │ • App Service   │          │ • App Service       │
    │ • SQL Database  │          │ • SQL Database      │
    │ • Managed ID    │          │ • Managed Identity  │
    │                 │          │ • Azure OpenAI      │
    │ No GenAI        │          │ • AI Search         │
    │ services        │          │                     │
    └─────────────────┘          └─────────────────────┘
```

## Components

### App Service (UK South)
- **SKU**: Standard S1 (to avoid cold starts)
- **Framework**: .NET 8.0
- **Features**: 
  - Razor Pages for UI
  - REST APIs with Swagger documentation
  - Chat UI with AI integration (when GenAI deployed)

### Azure SQL Database (UK South)
- **Tier**: Basic (for development)
- **Database**: expensedb
- **Authentication**: Entra ID Only (no SQL auth per MCAPS governance)
- **Security**: Managed Identity access

### Azure OpenAI (Sweden Central)
- **SKU**: S0
- **Model**: GPT-4o
- **Capacity**: 8 tokens/minute
- **Purpose**: Chat AI for expense management assistance

### AI Search (Sweden Central)
- **SKU**: Basic
- **Purpose**: RAG pattern for contextual AI responses

### User-Assigned Managed Identity
- Connects all services securely
- No passwords or connection strings with secrets
- Role assignments:
  - SQL Database Reader/Writer
  - Cognitive Services OpenAI User
  - Search Index Data Contributor

## Authentication Flow

1. User accesses App Service via HTTPS
2. App Service uses Managed Identity to authenticate to:
   - Azure SQL Database (Active Directory Managed Identity auth)
   - Azure OpenAI (DefaultAzureCredential)
   - AI Search (DefaultAzureCredential)

## Local Development

For local development, use `Authentication=Active Directory Default` in the connection string. This will use your Azure CLI login credentials (`az login`).
