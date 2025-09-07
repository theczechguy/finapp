using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyToExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IrregularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "IrregularExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_RegularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "RegularExpenses");

            migrationBuilder.AlterColumn<int>(
                name: "ExpenseCategoryId",
                table: "RegularExpenses",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "RegularExpenses",
                type: "decimal(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "RegularExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ExpenseCategoryId",
                table: "IrregularExpenses",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "IrregularExpenses",
                type: "decimal(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "IrregularExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_IrregularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "IrregularExpenses",
                column: "ExpenseCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "RegularExpenses",
                column: "ExpenseCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IrregularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "IrregularExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_RegularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "RegularExpenses");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "RegularExpenses");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "IrregularExpenses");

            migrationBuilder.AlterColumn<int>(
                name: "ExpenseCategoryId",
                table: "RegularExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "RegularExpenses",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 2)");

            migrationBuilder.AlterColumn<int>(
                name: "ExpenseCategoryId",
                table: "IrregularExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "IrregularExpenses",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_IrregularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "IrregularExpenses",
                column: "ExpenseCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegularExpenses_ExpenseCategories_ExpenseCategoryId",
                table: "RegularExpenses",
                column: "ExpenseCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
