using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenefitFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beneficiaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beneficiaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "benefit_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BenefitType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    available_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_benefit_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_benefit_balances_beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "benefit_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BenefitType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_benefit_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_benefit_reservations_beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_beneficiaries_cpf",
                table: "beneficiaries",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_benefit_balances_BeneficiaryId_BenefitType",
                table: "benefit_balances",
                columns: new[] { "BeneficiaryId", "BenefitType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_benefit_reservations_BeneficiaryId",
                table: "benefit_reservations",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_benefit_reservations_IdempotencyKey",
                table: "benefit_reservations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_benefit_reservations_Status_ExpiresAt",
                table: "benefit_reservations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_OccurredAt",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benefit_balances");

            migrationBuilder.DropTable(
                name: "benefit_reservations");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "beneficiaries");
        }
    }
}
