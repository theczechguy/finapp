using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryBudgets_TemporalLookup",
                table: "CategoryBudgets");

            migrationBuilder.CreateTable(
                name: "InvestmentProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentProviders_Name",
                table: "InvestmentProviders",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentProviders");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_TemporalLookup",
                table: "CategoryBudgets",
                columns: new[] { "ExpenseCategoryId", "StartYear", "StartMonth", "EndYear", "EndMonth" });
        }
    }
}
