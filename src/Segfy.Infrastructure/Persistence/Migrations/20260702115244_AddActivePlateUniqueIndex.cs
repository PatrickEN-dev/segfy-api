using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivePlateUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Policies_LicensePlate_ActiveUnique",
                table: "Policies",
                column: "LicensePlate",
                unique: true,
                filter: "Status = 'Ativa'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_LicensePlate_ActiveUnique",
                table: "Policies");
        }
    }
}
