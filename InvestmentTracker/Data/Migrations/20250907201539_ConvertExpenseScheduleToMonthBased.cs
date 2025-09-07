using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertExpenseScheduleToMonthBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseSchedules_RegularExpenseId_StartDate",
                table: "ExpenseSchedules");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ExpenseSchedules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "ExpenseSchedules");

            migrationBuilder.RenameColumn(
                name: "DayOfMonth",
                table: "ExpenseSchedules",
                newName: "EndYear");

            migrationBuilder.AddColumn<int>(
                name: "EndMonth",
                table: "ExpenseSchedules",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartMonth",
                table: "ExpenseSchedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartYear",
                table: "ExpenseSchedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_RegularExpenseId_StartYear_StartMonth",
                table: "ExpenseSchedules",
                columns: new[] { "RegularExpenseId", "StartYear", "StartMonth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseSchedules_RegularExpenseId_StartYear_StartMonth",
                table: "ExpenseSchedules");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                table: "ExpenseSchedules");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                table: "ExpenseSchedules");

            migrationBuilder.DropColumn(
                name: "StartYear",
                table: "ExpenseSchedules");

            migrationBuilder.RenameColumn(
                name: "EndYear",
                table: "ExpenseSchedules",
                newName: "DayOfMonth");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ExpenseSchedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "ExpenseSchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_RegularExpenseId_StartDate",
                table: "ExpenseSchedules",
                columns: new[] { "RegularExpenseId", "StartDate" });
        }
    }
}
