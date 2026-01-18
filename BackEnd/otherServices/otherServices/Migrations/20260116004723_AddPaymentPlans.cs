using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace otherServices.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentCards_PaymentCardId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentCardId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProviderRef",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Transactions");

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentScheduleId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

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
                name: "PaymentSchedules");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExternalRef",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentScheduleId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalRef",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentScheduleId",
                table: "Transactions");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Transactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderRef",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentCardId",
                table: "Transactions",
                column: "PaymentCardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentCards_PaymentCardId",
                table: "Transactions",
                column: "PaymentCardId",
                principalTable: "PaymentCards",
                principalColumn: "PaymentCardId");
        }
    }
}
