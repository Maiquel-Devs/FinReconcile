using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinReconcile.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankStatements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StatementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternalTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InternalTransactionId = table.Column<int>(type: "int", nullable: false),
                    BankStatementId = table.Column<int>(type: "int", nullable: false),
                    MatchType = table.Column<int>(type: "int", nullable: false),
                    DifferenceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReconciledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_InternalTransactions_InternalTransactionId",
                        column: x => x.InternalTransactionId,
                        principalTable: "InternalTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_BatchId",
                table: "BankStatements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_OriginalReference",
                table: "BankStatements",
                column: "OriginalReference");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransactions_ExternalReference",
                table: "InternalTransactions",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_BankStatementId",
                table: "ReconciliationMatches",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_InternalTransactionId",
                table: "ReconciliationMatches",
                column: "InternalTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationMatches");

            migrationBuilder.DropTable(
                name: "BankStatements");

            migrationBuilder.DropTable(
                name: "InternalTransactions");
        }
    }
}
