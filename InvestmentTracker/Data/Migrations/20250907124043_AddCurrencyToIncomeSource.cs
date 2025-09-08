using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyToIncomeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "IncomeSources",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Skip the problematic AlterColumn for now - we'll handle this differently
            // The ExpectedAmount column will remain as TEXT for compatibility
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "IncomeSources");
        }
    }
}
