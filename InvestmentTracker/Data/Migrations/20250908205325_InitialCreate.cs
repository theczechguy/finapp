using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMember", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncomeSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Investments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    ChargeAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpenseCategoryId = table.Column<int>(type: "integer", nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: false),
                    StartMonth = table.Column<int>(type: "integer", nullable: false),
                    EndYear = table.Column<int>(type: "integer", nullable: true),
                    EndMonth = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryBudgets_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IrregularExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    ExpenseCategoryId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpenseType = table.Column<int>(type: "integer", nullable: false),
                    FamilyMemberId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IrregularExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IrregularExpenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IrregularExpenses_FamilyMember_FamilyMemberId",
                        column: x => x.FamilyMemberId,
                        principalTable: "FamilyMember",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegularExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    ExpenseCategoryId = table.Column<int>(type: "integer", nullable: true),
                    ExpenseType = table.Column<int>(type: "integer", nullable: false),
                    FamilyMemberId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegularExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegularExpenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegularExpenses_FamilyMember_FamilyMemberId",
                        column: x => x.FamilyMemberId,
                        principalTable: "FamilyMember",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MonthlyIncomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IncomeSourceId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyIncomes_IncomeSources_IncomeSourceId",
                        column: x => x.IncomeSourceId,
                        principalTable: "IncomeSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OneTimeIncomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    IncomeSourceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeIncomes_IncomeSources_IncomeSourceId",
                        column: x => x.IncomeSourceId,
                        principalTable: "IncomeSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContributionSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestmentId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionSchedules_Investments_InvestmentId",
                        column: x => x.InvestmentId,
                        principalTable: "Investments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestmentId = table.Column<int>(type: "integer", nullable: false),
                    AsOf = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentValues_Investments_InvestmentId",
                        column: x => x.InvestmentId,
                        principalTable: "Investments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OneTimeContributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestmentId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeContributions_Investments_InvestmentId",
                        column: x => x.InvestmentId,
                        principalTable: "Investments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegularExpenseId = table.Column<int>(type: "integer", nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: false),
                    StartMonth = table.Column<int>(type: "integer", nullable: false),
                    EndYear = table.Column<int>(type: "integer", nullable: true),
                    EndMonth = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseSchedules_RegularExpenses_RegularExpenseId",
                        column: x => x.RegularExpenseId,
                        principalTable: "RegularExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_CoveringQuery",
                table: "CategoryBudgets",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth", "ExpenseCategoryId", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_ExpenseCategoryId_StartYear_StartMonth",
                table: "CategoryBudgets",
                columns: new[] { "ExpenseCategoryId", "StartYear", "StartMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_TemporalLookup",
                table: "CategoryBudgets",
                columns: new[] { "ExpenseCategoryId", "StartYear", "StartMonth", "EndYear", "EndMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionSchedules_InvestmentId_StartDate",
                table: "ContributionSchedules",
                columns: new[] { "InvestmentId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_CoveringQuery",
                table: "ExpenseSchedules",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth", "RegularExpenseId", "Amount", "Frequency" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_RegularExpenseId_StartYear_StartMonth",
                table: "ExpenseSchedules",
                columns: new[] { "RegularExpenseId", "StartYear", "StartMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSchedules_TemporalLookup",
                table: "ExpenseSchedules",
                columns: new[] { "StartYear", "StartMonth", "EndYear", "EndMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentValues_InvestmentId_AsOf",
                table: "InvestmentValues",
                columns: new[] { "InvestmentId", "AsOf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_Date",
                table: "IrregularExpenses",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_DateCategoryAmount",
                table: "IrregularExpenses",
                columns: new[] { "Date", "ExpenseCategoryId", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_ExpenseCategoryId",
                table: "IrregularExpenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IrregularExpenses_FamilyMemberId",
                table: "IrregularExpenses",
                column: "FamilyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyIncomes_IncomeSourceId_Month",
                table: "MonthlyIncomes",
                columns: new[] { "IncomeSourceId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeContributions_InvestmentId_Date",
                table: "OneTimeContributions",
                columns: new[] { "InvestmentId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeIncomes_Date",
                table: "OneTimeIncomes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeIncomes_IncomeSourceId",
                table: "OneTimeIncomes",
                column: "IncomeSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RegularExpenses_ExpenseCategoryId",
                table: "RegularExpenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RegularExpenses_FamilyMemberId",
                table: "RegularExpenses",
                column: "FamilyMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryBudgets");

            migrationBuilder.DropTable(
                name: "ContributionSchedules");

            migrationBuilder.DropTable(
                name: "ExpenseSchedules");

            migrationBuilder.DropTable(
                name: "InvestmentValues");

            migrationBuilder.DropTable(
                name: "IrregularExpenses");

            migrationBuilder.DropTable(
                name: "MonthlyIncomes");

            migrationBuilder.DropTable(
                name: "OneTimeContributions");

            migrationBuilder.DropTable(
                name: "OneTimeIncomes");

            migrationBuilder.DropTable(
                name: "RegularExpenses");

            migrationBuilder.DropTable(
                name: "Investments");

            migrationBuilder.DropTable(
                name: "IncomeSources");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "FamilyMember");
        }
    }
}
