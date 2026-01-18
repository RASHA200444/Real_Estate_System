using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace otherServices.Migrations
{
    /// <inheritdoc />
    public partial class PaymentChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCard");

            migrationBuilder.AddColumn<string>(
                name: "PrivateSignKeyEncrypted",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicSignKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AdminUserId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractHash",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContractId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LandlordUserId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetToLandlord",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "DownPayment",
                table: "Proposals",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InstallmentAmount",
                table: "Proposals",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId1",
                table: "PaymentCards",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCards_UserId1",
                table: "PaymentCards",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentCards_Users_UserId1",
                table: "PaymentCards",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentCards_Users_UserId1",
                table: "PaymentCards");

            migrationBuilder.DropIndex(
                name: "IX_PaymentCards_UserId1",
                table: "PaymentCards");

            migrationBuilder.DropColumn(
                name: "PrivateSignKeyEncrypted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PublicSignKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ContractHash",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LandlordUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NetToLandlord",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DownPayment",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "InstallmentAmount",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "PaymentCards");

            migrationBuilder.CreateTable(
                name: "CreditCard",
                columns: table => new
                {
                    CreditCardId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CVV = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CardHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCard", x => x.CreditCardId);
                    table.ForeignKey(
                        name: "FK_CreditCard_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCard_UserId",
                table: "CreditCard",
                column: "UserId");
        }
    }
}
