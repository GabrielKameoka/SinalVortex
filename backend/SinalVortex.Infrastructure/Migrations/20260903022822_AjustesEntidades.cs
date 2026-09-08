using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinalVortex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustesEntidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AlterColumn<string>(
                name: "MensagemErro",
                table: "LogsNotificacoes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_AplicacaoId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_CriadoEm",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_Status",
                table: "Notificacoes");

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

            migrationBuilder.AlterColumn<string>(
                name: "MensagemErro",
                table: "LogsNotificacoes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
