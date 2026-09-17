using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinalVortex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FeatureCrmMultitenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LogsNotificacoes_Notificacoes_NotificacaoId",
                table: "LogsNotificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_AplicacaoId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_CriadoEm",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_Status",
                table: "Notificacoes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LogsNotificacoes",
                table: "LogsNotificacoes");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Aplicacoes");

            migrationBuilder.RenameTable(
                name: "LogsNotificacoes",
                newName: "LogNotificacao");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "Notificacoes",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_LogsNotificacoes_NotificacaoId",
                table: "LogNotificacao",
                newName: "IX_LogNotificacao_NotificacaoId");

            migrationBuilder.AlterColumn<string>(
                name: "Destinatario",
                table: "Notificacoes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Conteudo",
                table: "Notificacoes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Assunto",
                table: "Notificacoes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContatoId",
                table: "Notificacoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Notificacoes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Notificacoes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Aplicacoes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Aplicacoes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "MensagemErro",
                table: "LogNotificacao",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LogNotificacao",
                table: "LogNotificacao",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Contatos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    CanalPreferencial = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contatos", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_LogNotificacao_Notificacoes_NotificacaoId",
                table: "LogNotificacao",
                column: "NotificacaoId",
                principalTable: "Notificacoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LogNotificacao_Notificacoes_NotificacaoId",
                table: "LogNotificacao");

            migrationBuilder.DropTable(
                name: "Contatos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LogNotificacao",
                table: "LogNotificacao");

            migrationBuilder.DropColumn(
                name: "ContatoId",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Aplicacoes");

            migrationBuilder.RenameTable(
                name: "LogNotificacao",
                newName: "LogsNotificacoes");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Notificacoes",
                newName: "CriadoEm");

            migrationBuilder.RenameIndex(
                name: "IX_LogNotificacao_NotificacaoId",
                table: "LogsNotificacoes",
                newName: "IX_LogsNotificacoes_NotificacaoId");

            migrationBuilder.AlterColumn<string>(
                name: "Destinatario",
                table: "Notificacoes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Conteudo",
                table: "Notificacoes",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Assunto",
                table: "Notificacoes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Aplicacoes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Aplicacoes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "MensagemErro",
                table: "LogsNotificacoes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LogsNotificacoes",
                table: "LogsNotificacoes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_AplicacaoId",
                table: "Notificacoes",
                column: "AplicacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_CriadoEm",
                table: "Notificacoes",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_Status",
                table: "Notificacoes",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_LogsNotificacoes_Notificacoes_NotificacaoId",
                table: "LogsNotificacoes",
                column: "NotificacaoId",
                principalTable: "Notificacoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
