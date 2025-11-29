using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMemberToInvestment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyMemberId",
                table: "Investments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Investments_FamilyMemberId",
                table: "Investments",
                column: "FamilyMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_FamilyMember_FamilyMemberId",
                table: "Investments",
                column: "FamilyMemberId",
                principalTable: "FamilyMember",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Investments_FamilyMember_FamilyMemberId",
                table: "Investments");

            migrationBuilder.DropIndex(
                name: "IX_Investments_FamilyMemberId",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "FamilyMemberId",
                table: "Investments");
        }
    }
}
