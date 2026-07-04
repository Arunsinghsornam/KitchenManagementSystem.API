using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenManagementSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingAndLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_OutletId",
                table: "Purchases",
                column: "OutletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Outlets_OutletId",
                table: "Purchases",
                column: "OutletId",
                principalTable: "Outlets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Outlets_OutletId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_OutletId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Organizations");
        }
    }
}
