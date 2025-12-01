using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace otherServices.Migrations
{
    /// <inheritdoc />
    public partial class edit_post_and_proposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RentalStatus",
                table: "Proposals");

            migrationBuilder.AddColumn<int>(
                name: "NIDEvaluation",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsInstallment",
                table: "Proposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Offeredprice",
                table: "Proposals",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ProposalStatus",
                table: "Proposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "date",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndRentalDate",
                table: "Posts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationPath",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PostDocPathEvaluation",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartRentalDate",
                table: "Posts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipDocPathEvaluation",
                table: "Landlords",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NIDEvaluation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsInstallment",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "Offeredprice",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "ProposalStatus",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "EndRentalDate",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LocationPath",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PostDocPathEvaluation",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "StartRentalDate",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OwnershipDocPathEvaluation",
                table: "Landlords");

            migrationBuilder.AddColumn<string>(
                name: "RentalStatus",
                table: "Proposals",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldDefaultValueSql: "GETDATE()");
        }
    }
}
