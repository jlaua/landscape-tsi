using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Landscape.Tsi.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IamEventoAuditoriaAutorizacion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeneficiaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmpresaSubsidiariaId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamEventoAuditoriaAutorizacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IamEventoAutenticacion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Mechanism = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamEventoAutenticacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IamPermiso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamPermiso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IamRol",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamRol", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IamUsuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BootstrapManaged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamUsuario", x => x.Id);
                    table.CheckConstraint("CK_IamUsuario_Vigencia", "[ValidUntilUtc] IS NULL OR [ValidFromUtc] IS NULL OR [ValidUntilUtc] > [ValidFromUtc]");
                });

            migrationBuilder.CreateTable(
                name: "IamRolPermiso",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamRolPermiso", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_IamRolPermiso_IamPermiso_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "IamPermiso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IamRolPermiso_IamRol_RoleId",
                        column: x => x.RoleId,
                        principalTable: "IamRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IamAccesoEmergencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaSubsidiariaId = table.Column<int>(type: "int", nullable: true),
                    IncidentReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamAccesoEmergencia", x => x.Id);
                    table.CheckConstraint("CK_IamAccesoEmergencia_Vigencia", "[ExpiresAtUtc] > [StartsAtUtc]");
                    table.ForeignKey(
                        name: "FK_IamAccesoEmergencia_IamUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "IamUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IamAccesoEmergencia_TEmpresaSubsidiaria_EmpresaSubsidiariaId",
                        column: x => x.EmpresaSubsidiariaId,
                        principalTable: "TEmpresaSubsidiaria",
                        principalColumn: "idEmpresaSubsidiaria",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IamUsuarioClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamUsuarioClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IamUsuarioClaim_IamUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "IamUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IamUsuarioLoginExterno",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamUsuarioLoginExterno", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_IamUsuarioLoginExterno_IamUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "IamUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IamUsuarioOrganizacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaSubsidiariaId = table.Column<int>(type: "int", nullable: true),
                    IsCorporateScope = table.Column<bool>(type: "bit", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamUsuarioOrganizacion", x => x.Id);
                    table.CheckConstraint("CK_IamUsuarioOrganizacion_Alcance", "([IsCorporateScope] = 1 AND [EmpresaSubsidiariaId] IS NULL) OR ([IsCorporateScope] = 0 AND [EmpresaSubsidiariaId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_IamUsuarioOrganizacion_IamUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "IamUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IamUsuarioOrganizacion_TEmpresaSubsidiaria_EmpresaSubsidiariaId",
                        column: x => x.EmpresaSubsidiariaId,
                        principalTable: "TEmpresaSubsidiaria",
                        principalColumn: "idEmpresaSubsidiaria",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IamUsuarioRol",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamUsuarioRol", x => new { x.UserId, x.RoleId });
                    table.CheckConstraint("CK_IamUsuarioRol_Vigencia", "[ValidUntilUtc] IS NULL OR [ValidUntilUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_IamUsuarioRol_IamRol_RoleId",
                        column: x => x.RoleId,
                        principalTable: "IamRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IamUsuarioRol_IamUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "IamUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IamUsuarioToken",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamUsuarioToken", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_IamUsuarioToken_IamUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "IamUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IamAccesoEmergencia_EmpresaSubsidiariaId",
                table: "IamAccesoEmergencia",
                column: "EmpresaSubsidiariaId");

            migrationBuilder.CreateIndex(
                name: "IX_IamAccesoEmergencia_ExpiresAtUtc",
                table: "IamAccesoEmergencia",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IamAccesoEmergencia_UserId",
                table: "IamAccesoEmergencia",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IamEventoAuditoriaAutorizacion_ActorUserId",
                table: "IamEventoAuditoriaAutorizacion",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IamEventoAuditoriaAutorizacion_BeneficiaryUserId",
                table: "IamEventoAuditoriaAutorizacion",
                column: "BeneficiaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IamEventoAuditoriaAutorizacion_EmpresaSubsidiariaId",
                table: "IamEventoAuditoriaAutorizacion",
                column: "EmpresaSubsidiariaId");

            migrationBuilder.CreateIndex(
                name: "IX_IamEventoAuditoriaAutorizacion_OccurredAtUtc",
                table: "IamEventoAuditoriaAutorizacion",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IamEventoAutenticacion_OccurredAtUtc",
                table: "IamEventoAutenticacion",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IamPermiso_Code",
                table: "IamPermiso",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IamRol_Code",
                table: "IamRol",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IamRolPermiso_PermissionId",
                table: "IamRolPermiso",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "IamUsuario",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "IamUsuario",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioClaim_UserId",
                table: "IamUsuarioClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioLoginExterno_Issuer_Subject",
                table: "IamUsuarioLoginExterno",
                columns: new[] { "Issuer", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioLoginExterno_UserId",
                table: "IamUsuarioLoginExterno",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioOrganizacion_EmpresaSubsidiariaId",
                table: "IamUsuarioOrganizacion",
                column: "EmpresaSubsidiariaId");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioOrganizacion_UserId",
                table: "IamUsuarioOrganizacion",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioOrganizacion_UserId_EmpresaSubsidiariaId_IsCorporateScope_ValidFromUtc",
                table: "IamUsuarioOrganizacion",
                columns: new[] { "UserId", "EmpresaSubsidiariaId", "IsCorporateScope", "ValidFromUtc" },
                unique: true,
                filter: "[EmpresaSubsidiariaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioRol_RoleId",
                table: "IamUsuarioRol",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_IamUsuarioRol_UserId",
                table: "IamUsuarioRol",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IamAccesoEmergencia");

            migrationBuilder.DropTable(
                name: "IamEventoAuditoriaAutorizacion");

            migrationBuilder.DropTable(
                name: "IamEventoAutenticacion");

            migrationBuilder.DropTable(
                name: "IamRolPermiso");

            migrationBuilder.DropTable(
                name: "IamUsuarioClaim");

            migrationBuilder.DropTable(
                name: "IamUsuarioLoginExterno");

            migrationBuilder.DropTable(
                name: "IamUsuarioOrganizacion");

            migrationBuilder.DropTable(
                name: "IamUsuarioRol");

            migrationBuilder.DropTable(
                name: "IamUsuarioToken");

            migrationBuilder.DropTable(
                name: "IamPermiso");

            migrationBuilder.DropTable(
                name: "IamRol");

            migrationBuilder.DropTable(
                name: "IamUsuario");
        }
    }
}
