using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeBudgetIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add composite index for temporal budget queries (most common query pattern)
            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_ExpenseCategoryId_StartYear_StartMonth_EndYear_EndMonth",
                table: "CategoryBudgets",
                columns: new[] { "ExpenseCategoryId", "StartYear", "StartMonth", "EndYear", "EndMonth" });

            // Add index for budget amount queries (for filtering large budgets)
            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_Amount",
                table: "CategoryBudgets",
                column: "Amount");

            // Add covering index for the most common budget lookup query
            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_StartYear_StartMonth_EndYear_EndMonth_ExpenseCategoryId_Amount",
                table: "CategoryBudgets",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth", "ExpenseCategoryId", "Amount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryBudgets_ExpenseCategoryId_StartYear_StartMonth_EndYear_EndMonth",
                table: "CategoryBudgets");

            migrationBuilder.DropIndex(
                name: "IX_CategoryBudgets_Amount",
                table: "CategoryBudgets");

            migrationBuilder.DropIndex(
                name: "IX_CategoryBudgets_StartYear_StartMonth_EndYear_EndMonth_ExpenseCategoryId_Amount",
                table: "CategoryBudgets");
        }
    }
}
