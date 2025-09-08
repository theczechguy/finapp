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
    public decimal ActualAmount { get; set; // The actual amount received this month
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

## 8. Budgets – Feature Design (UX & Behavior)

### 8.1 Goals
- Provide a clear, at-a-glance view per month: for each category, am I within budget or over?
- Make budget setup lightweight and fast; avoid complex accounting concepts.
- No carry-over: leftover budget does not roll into the next month.

### 8.2 Scope
- Monthly budget per expense category (e.g., Groceries: 500 in October 2025).
- Optionally a global monthly budget (sum target) is out-of-scope for the first iteration.
- Applies to Expenses only (Regular + Irregular) for the selected month.
- Display in the app’s base currency.

### 8.3 How It Works (User Perspective)
- For a selected month, each category can have an optional budget amount.
- The app shows spending-to-date vs. the budget with a visual progress bar per category.
- If no budget is set for a category, show a muted “No budget” label and a small inline “Add” action.
- Over-budget states are clearly indicated (color + icon + label).
- No carry-over: each month’s budget stands alone (leftover does not roll over).

### 8.4 Monthly Spending Definition
- Spending per category for a month = sum of:
    - Regular Expenses that apply to that month (as they are currently displayed in the monthly view)
    - Irregular Expenses with dates within that month
- Only actual recorded amounts count. No forecasting in the first iteration.

- ### 8.5 UI Placement & Patterns
- Placement on Expenses page (monthly view):
    - Dedicated Budgets card: shows progress for categories with budgets only. No inline editing.
    - Link to separate Manage Budgets page for setting/editing budgets.
- Manage Budgets page (/Expenses/Budgets):
    - Full table of all categories with inline editing for budgets.
    - Month/year picker to manage budgets for different months.
    - Delete budget option per category.
- Category row content (Expenses page):
    - Name, Budget amount, Spent amount, Progress bar, Status chip (Under/Near/Over).
- Category row content (Manage Budgets page):
    - Name, Current Budget, Spent, Progress, Status, Actions (edit/delete).
- Color coding (thresholds):
    - Under budget: neutral/brand color (e.g., primary)
    - Near budget: warning color when Spent/Budget ≥ 80% and < 100%
    - Over budget: danger color when Spent/Budget ≥ 100%, with "Over by X" label
- Quick inline edit (on Manage Budgets page):
    - Amount input, scope toggle (This month only | This and future months [default]), Save, Delete.
- Manage Budgets page:
    - Bulk set budgets for multiple categories for a chosen month.
    - "Copy previous month's values" shortcut (copies budget amounts, not leftovers).

### 8.6 Status & Messaging
- States per category for the month:
    - No budget set (show “No budget” + inline Add)
    - Under budget (show remaining amount)
    - Near budget (warning at ≥ 80%)
    - Over budget (show overage amount)
    - Status labels used: “No budget”, “Under”, “Near”, “Over”.
- Page-level summary (optional): total budgeted vs. total spent for categories that have budgets.

### 8.7 Alerts (In-App)
- Non-intrusive indicators:
    - Progress bar color changes by threshold.
    - Small warning icon + tooltip when near budget.
    - Clear danger state + “Over by X” when exceeded.
- No emails/SMS for the first iteration; in-app only.

### 8.8 Navigation & Month Handling
- Budgets are month-specific views. Switching month updates the displayed budgets and progress.
- If a category lacks a budget for the selected month, show “No budget” with an inline Add option.
- Convenience: “Copy previous month’s values” on the Manage Budgets page (no carry-over of leftover).

### 8.9 Accessibility & Responsiveness
- Ensure progress bars meet contrast guidelines.
- Use text labels (Spent/ Budget / Remaining) in addition to color.
- Collapse to stacked cards on narrow screens.

### 8.10 Out of Scope (MVP)
- Carry-over/roll-over balances (explicitly not desired).
- Forecasting or predictive alerts.
- Email/SMS notifications.
- Multi-currency budgets.

### 8.11 Future Extensions (Nice-to-Have)
- “Copy previous month’s budgets” action.
- Global monthly budget (overall cap) with optional distribution across categories.
- Budget presets by category or templates.
- Export of budget vs. actual.
- Simple forecast: show projected month-end based on mid-month pace (later).

### 8.12 Success Criteria
- At a glance, users can tell which categories are over/under budget for the current month.
- Editing/setting a budget per category takes ≤ 2 clicks.
- Visual states are unmistakable without relying solely on color.

### 8.13 Temporal Budgets (Effective‑Dated)
- Budgets are temporal per category: changes take effect from the selected month forward and do not alter past months.
- Each category can evolve over time (e.g., Groceries budget 400 until Apr 2025, then 500 from May 2025 onward).
- The app determines the effective budget for a month by picking the latest change on or before that month.
- Inline edit offers scope:
    - This month only: set/override for the current month (does not affect future months).
    - This and future months (default): set a new effective amount starting from the selected month forward.

### 8.14 Currency & Display
- Budgets, spending, and progress are shown in the app’s base currency.
- Use consistent number formatting and currency symbols across the Budgets card and related summaries.

### 8.15 Wireframe Reference (Textual)
- Budgets Card (placed above category breakdown on Expenses page):
    - Header: “Budgets — {Month Name YYYY}” with a subtle info tooltip linking to Manage Budgets.
    - Body: list/table of categories. Each row contains:
        - Left: Category name
        - Middle: Budget amount (inline editable), Spent amount, Remaining (if Under/Near), or Over by X (if Over)
        - Progress: full-width bar under the row metrics, colored by status (Under/Near/Over)
        - Right: Status chip (No budget / Under / Near / Over)
    - Row actions (shown when editing): amount input, scope toggle (This month only | This and future months [default]), Save, Cancel
- Mobile layout:
    - Each category row stacks: Name on top; Budget/Spent below; Progress bar full width; Status chip aligned right or below
- Empty state:
    - “No budgets set for this month” with a small “Set budgets” button

### 8.16 UI Copy & Microcopy
- Card header:
    - Title: “Budgets — {Month YYYY}”
    - Info tooltip: “Set monthly category budgets. Changes can apply to this month or to future months. Leftover does not carry over.”

- Table/list headers (if shown):
    - Category | Budget | Spent | Status

- Category row (examples):
    - No budget state: Status chip “No budget”; inline action button “Add”.
    - Under/Near: Inline text “Spent {spent} / {budget} ({percent}%) • Remaining {remaining}”.
    - Over: Inline text “Spent {spent} / {budget} ({percent}%) • Over by {over}”.

- Progress bar:
    - Visual color by status (primary/warning/danger).
    - Tooltip/aria-label text:
        - Under/Near: “Spent {spent} of {budget} ({percent}%). {remaining} remaining.”
        - Over: “Spent {spent} of {budget} ({percent}%). Over by {over}.”

- Status chips:
    - Labels: “No budget”, “Under”, “Near”, “Over”.
    - Near threshold: 80% (≥ 80% and < 100%).
    - Over threshold: ≥ 100%.

- Inline edit (quick edit):
    - Budget amount input placeholder: “Enter amount”.
    - Scope selector label: “Apply to”. Options:
        - Radio 1 (default): “This and future months”
        - Radio 2: “This month only”
    - Helper text (scope): “Applies starting {Month YYYY}. Past months remain unchanged.”
    - Actions: “Save”, “Cancel”.
    - Success toast/snackbar: “Budget updated.”
    - Validation message: “Please enter a positive amount.”
    - Error message (generic): “Could not update budget. Please try again.”

- Manage Budgets page (optional):
    - Header: “Manage Budgets — {Month YYYY}”.
    - Month picker label: “Month”.
    - Action: “Copy previous month’s values”.
    - Table columns: Category | Budget (editable) | Notes (optional).

- Empty state (Budgets card):
    - Title text: “No budgets set for this month”.
    - Action button: “Set budgets”. Secondary link: “Copy previous month’s values”.

- Tooltips (optional):
    - Near state: “Approaching budget (≥ 80%).”
    - Over state: “Over budget (≥ 100%).”
    - Add budget: “Set a budget for this category.”

- Accessibility:
    - Progress bars include aria-label as above and visible numeric text.
    - Status changes announce via aria-live “polite”: “Budget updated. Status: {status}. {remaining/over}.”
    - Inputs have associated labels, scope radios are grouped with a fieldset/legend.

- Formatting:
    - Currency: use app base currency, e.g., “$1,234.56”.
    - Percent: no decimals for > 10%; 1 decimal for < 10% (e.g., “9.5%”).
    - Large numbers: thousands separators.

### 8.17 ASCII Wireframe (Visual Mock)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Budgets — October 2025                                 (i) Info tooltip      │
├──────────────────────────────────────────────────────────────────────────────┤
│ Category            Budget        Spent            Status                    │
│                                                                              │
│ Groceries          $500.00      $420.00         [ Near ]                     │
│ Spent 420 / 500 (84%) • Remaining 80                                          │
│ [███████████████████████████████████████████────] 84% (warning color)        │
│                                                                              │
│ Utilities          $180.00      $90.00          [ Under ]                    │
│ Spent 90 / 180 (50%) • Remaining 90                                           │
│ [██████████████--------------------------------] 50% (primary color)         │
│                                                                              │
│ Dining Out         $150.00      $170.00         [ Over ]                     │
│ Spent 170 / 150 (113%) • Over by 20                                            │
│ [██████████████████████████████████████████████] 113% (danger color)         │
│                                                                              │
│ Transportation     —             $60.00          [ No budget ]               │
│ No budget set.                     (Add)                                        │
└──────────────────────────────────────────────────────────────────────────────┘
```

Inline Edit (Quick Edit) for a Category

```
Groceries  Budget: [ 500.00 ]  (Save) (Cancel)
Apply to:  (•) This and future months    ( ) This month only
Helper:    Applies starting October 2025. Past months remain unchanged.
```

Mobile Stacked Layout (One Row)

```
Groceries                                  [ Near ]
Budget: $500.00   Spent: $420.00   Remaining: 80
[███████████████████████████████████████████────] 84%
(Edit)
```

Empty State

```
No budgets set for this month
( Set budgets )         ( Copy previous month’s values )
```

### 8.18 Example Copy (Concrete Values)

- Under: “Spent $90 / $180 (50%) • Remaining $90”
- Near (≥80%): “Spent $420 / $500 (84%) • Remaining $80”
- Over (≥100%): “Spent $170 / $150 (113%) • Over by $20”
- No budget: “No budget set.”
- Tooltip (progress, Under/Near): “Spent $420 of $500 (84%). $80 remaining.”
- Tooltip (progress, Over): “Spent $170 of $150 (113%). Over by $20.”
