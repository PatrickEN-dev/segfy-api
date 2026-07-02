using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusHistoryAndUtcTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyStatusHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyStatusHistory_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyStatusHistory_PolicyId",
                table: "PolicyStatusHistory",
                column: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyStatusHistory");
        }
    }
}
