using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStartDayToExpenseSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EndDay",
                table: "ExpenseSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartDay",
                table: "ExpenseSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDay",
                table: "ExpenseSchedules");

            migrationBuilder.DropColumn(
                name: "StartDay",
                table: "ExpenseSchedules");
        }
    }
}
