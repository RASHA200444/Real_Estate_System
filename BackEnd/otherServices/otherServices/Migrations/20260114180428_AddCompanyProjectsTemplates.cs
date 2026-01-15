using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace otherServices.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProjectsTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CommercialRegisterPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommercialRegisterEvaluation = table.Column<int>(type: "int", nullable: false),
                    PendingStatus = table.Column<int>(type: "int", nullable: false),
                    LandlordId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Companies_Landlords_LandlordId",
                        column: x => x.LandlordId,
                        principalTable: "Landlords",
                        principalColumn: "LandlordId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Companies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectDocPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalFloors = table.Column<int>(type: "int", nullable: false),
                    HasElevator = table.Column<bool>(type: "bit", nullable: false),
                    UnitsPerFloor = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PendingStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitTemplates",
                columns: table => new
                {
                    UnitTemplateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    UnitCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NumberOfRooms = table.Column<int>(type: "int", nullable: false),
                    NumberOfBathrooms = table.Column<int>(type: "int", nullable: false),
                    Area = table.Column<double>(type: "float", nullable: false),
                    IsFurnished = table.Column<bool>(type: "bit", nullable: false),
                    HasGarage = table.Column<bool>(type: "bit", nullable: false),
                    BasePrice = table.Column<double>(type: "float", nullable: false),
                    PriceIncreasePerFloor = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTemplates", x => x.UnitTemplateId);
                    table.ForeignKey(
                        name: "FK_UnitTemplates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ProjectId",
                table: "Posts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_LandlordId",
                table: "Companies",
                column: "LandlordId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitTemplates_ProjectId",
                table: "UnitTemplates",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Projects_ProjectId",
                table: "Posts",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Projects_ProjectId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "UnitTemplates");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ProjectId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Posts");
        }
    }
}
