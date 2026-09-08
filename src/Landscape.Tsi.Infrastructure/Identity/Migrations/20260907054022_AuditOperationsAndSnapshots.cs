using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Landscape.Tsi.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AuditOperationsAndSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "Operation",
                schema: "audit",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PhysicalTableName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RootRecordId = table.Column<long>(type: "bigint", nullable: true),
                    RootDisplayName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmpresaSubsidiariaId = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AffectedRecordCount = table.Column<int>(type: "int", nullable: false),
                    ReversesOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operation", x => x.OperationId);
                });

            migrationBuilder.CreateTable(
                name: "RecordKeyMap",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhysicalTableName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OldPrimaryKeyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewPrimaryKeyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordKeyMap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordKeyMap_Operation_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "audit",
                        principalTable: "Operation",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecordSnapshot",
                schema: "audit",
                columns: table => new
                {
                    SnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PhysicalTableName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PrimaryKeyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ForeignKeysJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteOrder = table.Column<int>(type: "int", nullable: false),
                    RestoreOrder = table.Column<int>(type: "int", nullable: false),
                    IsRoot = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordSnapshot", x => x.SnapshotId);
                    table.ForeignKey(
                        name: "FK_RecordSnapshot_Operation_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "audit",
                        principalTable: "Operation",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Operation_ActionType",
                schema: "audit",
                table: "Operation",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_Operation_ActorUserId",
                schema: "audit",
                table: "Operation",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Operation_CorrelationId",
                schema: "audit",
                table: "Operation",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Operation_EmpresaSubsidiariaId",
                schema: "audit",
                table: "Operation",
                column: "EmpresaSubsidiariaId");

            migrationBuilder.CreateIndex(
                name: "IX_Operation_EntityCode",
                schema: "audit",
                table: "Operation",
                column: "EntityCode");

            migrationBuilder.CreateIndex(
                name: "IX_Operation_OccurredAtUtc",
                schema: "audit",
                table: "Operation",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Operation_ReversesOperationId",
                schema: "audit",
                table: "Operation",
                column: "ReversesOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordKeyMap_OperationId_PhysicalTableName",
                schema: "audit",
                table: "RecordKeyMap",
                columns: new[] { "OperationId", "PhysicalTableName" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordSnapshot_OperationId_PhysicalTableName_DeleteOrder",
                schema: "audit",
                table: "RecordSnapshot",
                columns: new[] { "OperationId", "PhysicalTableName", "DeleteOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecordKeyMap",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "RecordSnapshot",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "Operation",
                schema: "audit");
        }
    }
}
