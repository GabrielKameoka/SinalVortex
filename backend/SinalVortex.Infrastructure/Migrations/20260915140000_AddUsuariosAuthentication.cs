using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SinalVortex.Infrastructure.Persistence;

#nullable disable

namespace SinalVortex.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260915140000_AddUsuariosAuthentication")]
public partial class AddUsuariosAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Usuarios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                Ativo = table.Column<bool>(type: "boolean", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Usuarios", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Usuarios_TenantId_Email",
            table: "Usuarios",
            columns: new[] { "TenantId", "Email" },
            unique: true);

        migrationBuilder.AddColumn<Guid>(name: "TenantId", table: "Templates", type: "uuid", nullable: true);
        migrationBuilder.Sql("""
            UPDATE "Templates" AS t SET "TenantId" = a."TenantId"
            FROM "Aplicacoes" AS a WHERE t."AplicacaoId" = a."Id";
            UPDATE "Templates" SET "TenantId" = '00000000-0000-0000-0000-000000000000' WHERE "TenantId" IS NULL;
            """);
        migrationBuilder.AlterColumn<Guid>(name: "TenantId", table: "Templates", type: "uuid", nullable: false,
            oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TenantId", table: "Templates");
        migrationBuilder.DropTable(name: "Usuarios");
    }
}
