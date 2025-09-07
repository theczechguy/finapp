# Expense Tracker Design Document

## 1. Overview

This document outlines the design and requirements for a new Expense Tracker feature within the FinApp application. The goal is to create a simple, single-page application for a family to track their monthly incomes and expenses, providing a clear overview of their financial situation for any given month.

## 2. Core Requirements

-   Track multiple sources of income.
-   Track both regular (recurring) and irregular (one-off) expenses.
-   Categorize all expenses to understand spending habits.
-   Provide a clear monthly summary of total income, total expenses, and the net balance.
-   Allow users to navigate through different months and years to review historical data.
-   Ensure the user interface is intuitive and allows for quick editing and addition of entries.

## 3. Architectural Approach

-   **Service-Oriented:** All business logic will be encapsulated within a dedicated service layer (`IExpenseService` and `ExpenseService`), following the pattern already established by the `InvestmentService`.
-   **Razor Pages:** The user interface will be a single Razor Page, which will interact directly with the `IExpenseService`.
-   **No API Layer (Initial Version):** For simplicity, a separate web API will not be created in the initial version. The Razor Page will handle all user interactions.
-   **Minimize Dependencies:** The project will prioritize using built-in .NET libraries (`System.Text.Json`, etc.) and avoid introducing third-party packages unless there is a clear and significant benefit.

## 4. Data Models

The following C# classes will be created in the `Models` directory to represent the data.

```csharp
// Represents a category for an expense, e.g., "Groceries", "Utilities"
public class ExpenseCategory 
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Represents a source of income, e.g., "Salary", "Bonus"
public class IncomeSource
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal ExpectedAmount { get; set; } // The baseline expected amount
}

// Represents an actual income entry for a specific month
public class MonthlyIncome
{
    public int Id { get; set; }
    public int IncomeSourceId { get; set; }
    public IncomeSource IncomeSource { get; set; }
    public DateTime Month { get; set; } // Represents the month (e.g., 2023-10-01)
    public decimal ActualAmount { get; set; } // The actual amount received this month
}

// Represents a recurring expense
public class RegularExpense
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory Category { get; set; }
    public Frequency Recurrence { get; set; } // e.g., Monthly, Quarterly, Annually
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// Represents a one-off, irregular expense
public class IrregularExpense
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory Category { get; set; }
    public DateTime Date { get; set; } // The specific date of the expense
}

public enum Frequency
{
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually
}
```

## 5. Service Layer (`IExpenseService`)

The service will manage all operations related to expenses and incomes.

```csharp
public interface IExpenseService
{
    // Get all data needed for a specific month's view
    Task<MonthlyExpenseViewModel> GetMonthlyDataAsync(int year, int month);

    // Manage Income Sources
    Task AddIncomeSourceAsync(IncomeSource incomeSource);
    Task UpdateIncomeSourceAsync(IncomeSource incomeSource);

    // Manage Monthly Income Entries
    Task LogOrUpdateMonthlyIncomeAsync(int incomeSourceId, int year, int month, decimal actualAmount);

    // Manage Expense Categories
    Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync();
    Task AddExpenseCategoryAsync(ExpenseCategory category);

    // Manage Regular Expenses
    Task AddRegularExpenseAsync(RegularExpense expense);
    Task UpdateRegularExpenseAsync(RegularExpense expense); // Note: Logic must prevent historical changes.

    // Manage Irregular Expenses
    Task AddIrregularExpenseAsync(IrregularExpense expense);
    Task DeleteIrregularExpenseAsync(int expenseId);
}
```

## 6. User Interface and Functionality

The feature will be implemented on a single Razor Page (`/Pages/Expenses/Index.cshtml`).

-   **Navigation:**
    -   Two dropdowns at the top of the page will allow the user to select the **Year** and **Month** to view.
    -   The page will default to the current month and year.

-   **Overview Section:**
    -   **Summary Cards:** Display `Total Income`, `Total Expenses`, and `Net Balance` for the selected month.
    -   **Expense Breakdown Chart:** A pie chart will show the percentage of total expenses for each category.

-   **Details Section:**
    -   **Incomes:**
        -   A list of all `IncomeSource` records.
        -   Each item will show the name and expected amount.
        -   An editable input field next to each will show the `ActualAmount` for the selected month, allowing for quick updates.
    -   **Regular Expenses:**
        -   A table listing all regular expenses applicable to the selected month.
        -   Columns: Name, Category, Amount.
        -   Should include an interface to add/edit regular expenses. When editing, the change must only apply from the selected month forward.
    -   **Irregular Expenses:**
        -   A table listing all irregular expenses recorded for the selected month.
        -   Columns: Date, Name, Category, Amount.
        -   A "Quick Add" form will be present to easily add a new irregular expense (Name, Amount, Category, Date).

## 7. Future Considerations

-   **API Layer:** A set of minimal API endpoints could be added later to allow for third-party integrations or a mobile app.
-   **Reporting:** A dedicated reporting page could be built to show trends over time (e.g., spending in a category over 12 months).
-   **Budgets:** A feature to set monthly budgets for each category and track performance against them.
