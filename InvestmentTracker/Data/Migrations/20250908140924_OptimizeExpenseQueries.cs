using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeExpenseQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_Date",
                table: "IrregularExpenses",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_DateCategoryAmount",
                table: "IrregularExpenses",
                columns: new[] { "Date", "ExpenseCategoryId", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_CoveringQuery",
                table: "ExpenseSchedules",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth", "RegularExpenseId", "Amount", "Frequency" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_TemporalLookup",
                table: "ExpenseSchedules",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_CoveringQuery",
                table: "CategoryBudgets",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth", "ExpenseCategoryId", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_TemporalLookup",
                table: "CategoryBudgets",
                columns: new[] { "ExpenseCategoryId", "StartYear", "StartMonth", "EndYear", "EndMonth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IrregularExpenses_Date",
                table: "IrregularExpenses");

            migrationBuilder.DropIndex(
                name: "IX_IrregularExpenses_DateCategoryAmount",
                table: "IrregularExpenses");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseSchedules_CoveringQuery",
                table: "ExpenseSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseSchedules_TemporalLookup",
                table: "ExpenseSchedules");

            migrationBuilder.DropIndex(
                name: "IX_CategoryBudgets_CoveringQuery",
                table: "CategoryBudgets");

            migrationBuilder.DropIndex(
                name: "IX_CategoryBudgets_TemporalLookup",
                table: "CategoryBudgets");
        }
    }
}
