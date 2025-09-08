using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyExpenseCategorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpenseType",
                table: "RegularExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyMemberId",
                table: "RegularExpenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpenseType",
                table: "IrregularExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FamilyMemberId",
                table: "IrregularExpenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FamilyMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMember", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegularExpenses_FamilyMemberId",
                table: "RegularExpenses",
                column: "FamilyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_FamilyMemberId",
                table: "IrregularExpenses",
                column: "FamilyMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_IrregularExpenses_FamilyMember_FamilyMemberId",
                table: "IrregularExpenses",
                column: "FamilyMemberId",
                principalTable: "FamilyMember",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegularExpenses_FamilyMember_FamilyMemberId",
                table: "RegularExpenses",
                column: "FamilyMemberId",
                principalTable: "FamilyMember",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IrregularExpenses_FamilyMember_FamilyMemberId",
                table: "IrregularExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_RegularExpenses_FamilyMember_FamilyMemberId",
                table: "RegularExpenses");

            migrationBuilder.DropTable(
                name: "FamilyMember");

            migrationBuilder.DropIndex(
                name: "IX_RegularExpenses_FamilyMemberId",
                table: "RegularExpenses");

            migrationBuilder.DropIndex(
                name: "IX_IrregularExpenses_FamilyMemberId",
                table: "IrregularExpenses");

            migrationBuilder.DropColumn(
                name: "ExpenseType",
                table: "RegularExpenses");

            migrationBuilder.DropColumn(
                name: "FamilyMemberId",
                table: "RegularExpenses");

            migrationBuilder.DropColumn(
                name: "ExpenseType",
                table: "IrregularExpenses");

            migrationBuilder.DropColumn(
                name: "FamilyMemberId",
                table: "IrregularExpenses");
        }
    }
}
