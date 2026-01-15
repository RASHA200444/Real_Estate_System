using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace otherServices.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProjectsTemplates2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitTemplates_ProjectId",
                table: "UnitTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_UnitTemplates_ProjectId_UnitCode",
                table: "UnitTemplates",
                columns: new[] { "ProjectId", "UnitCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitTemplates_ProjectId_UnitCode",
                table: "UnitTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_UnitTemplates_ProjectId",
                table: "UnitTemplates",
                column: "ProjectId");
        }
    }
}
