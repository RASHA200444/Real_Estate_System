using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace otherServices.Migrations
{
    /// <inheritdoc />
    public partial class Merge_SubscriptionPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AlterColumn<long>(
                name: "PostId",
                table: "Transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AdminUserId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetToLandlord",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "PaymentCardId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "PaymentScheduleId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "InstallmentDurationMonths",
                table: "Proposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentIntervalMonths",
                table: "Proposals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BankCards",
                columns: table => new
                {
                    BankCardId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardNumberEncrypted = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CvvHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    ExpiryMonth = table.Column<int>(type: "int", nullable: false),
                    ExpiryYear = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FailedChargeCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCards", x => x.BankCardId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentCards",
                columns: table => new
                {
                    PaymentCardId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CardTokenEncrypted = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MaskedCardNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    ExpiryMonth = table.Column<int>(type: "int", nullable: false),
                    ExpiryYear = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId1 = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCards", x => x.PaymentCardId);
                    table.ForeignKey(
                        name: "FK_PaymentCards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentCards_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "BankTokenMaps",
                columns: table => new
                {
                    BankTokenMapId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankCardId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTokenMaps", x => x.BankTokenMapId);
                    table.ForeignKey(
                        name: "FK_BankTokenMaps_BankCards_BankCardId",
                        column: x => x.BankCardId,
                        principalTable: "BankCards",
                        principalColumn: "BankCardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    PaymentPlanId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    PayerUserId = table.Column<long>(type: "bigint", nullable: false),
                    PayeeUserId = table.Column<long>(type: "bigint", nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    IsInstallment = table.Column<int>(type: "int", nullable: false),
                    PaymentCardId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: true),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    DurationMonths = table.Column<int>(type: "int", nullable: true),
                    IntervalMonths = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PeriodicAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformFeePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.PaymentPlanId);
                    table.ForeignKey(
                        name: "FK_PaymentPlans_PaymentCards_PaymentCardId",
                        column: x => x.PaymentCardId,
                        principalTable: "PaymentCards",
                        principalColumn: "PaymentCardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentPlans_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentPlans_Users_PayeeUserId",
                        column: x => x.PayeeUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentPlans_Users_PayerUserId",
                        column: x => x.PayerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSchedules",
                columns: table => new
                {
                    PaymentScheduleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentPlanId = table.Column<long>(type: "bigint", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LastFailureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EscalatedToAdmin = table.Column<bool>(type: "bit", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSchedules", x => x.PaymentScheduleId);
                    table.ForeignKey(
                        name: "FK_PaymentSchedules_PaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "PaymentPlans",
                        principalColumn: "PaymentPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExternalRef",
                table: "Transactions",
                column: "ExternalRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentScheduleId",
                table: "Transactions",
                column: "PaymentScheduleId",
                unique: true,
                filter: "[PaymentScheduleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BankTokenMaps_BankCardId",
                table: "BankTokenMaps",
                column: "BankCardId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCards_UserId_CardTokenEncrypted",
                table: "PaymentCards",
                columns: new[] { "UserId", "CardTokenEncrypted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCards_UserId1",
                table: "PaymentCards",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PayeeUserId",
                table: "PaymentPlans",
                column: "PayeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PayerUserId",
                table: "PaymentPlans",
                column: "PayerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PaymentCardId",
                table: "PaymentPlans",
                column: "PaymentCardId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_PostId",
                table: "PaymentPlans",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSchedules_PaymentPlanId_DueDate",
                table: "PaymentSchedules",
                columns: new[] { "PaymentPlanId", "DueDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentSchedules_PaymentScheduleId",
                table: "Transactions",
                column: "PaymentScheduleId",
                principalTable: "PaymentSchedules",
                principalColumn: "PaymentScheduleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentSchedules_PaymentScheduleId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "BankTokenMaps");

            migrationBuilder.DropTable(
                name: "PaymentSchedules");

            migrationBuilder.DropTable(
                name: "BankCards");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropTable(
                name: "PaymentCards");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExternalRef",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentScheduleId",
                table: "Transactions");

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
                name: "Amount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ContractHash",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalRef",
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
                name: "LastError",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NetToLandlord",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentCardId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentScheduleId",
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
                name: "InstallmentDurationMonths",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "InstallmentIntervalMonths",
                table: "Proposals");

            migrationBuilder.AlterColumn<long>(
                name: "PostId",
                table: "Transactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
