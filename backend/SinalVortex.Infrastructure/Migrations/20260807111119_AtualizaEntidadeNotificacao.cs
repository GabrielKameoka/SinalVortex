using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinalVortex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AtualizaEntidadeNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PayloadJson",
                table: "Notificacoes",
                newName: "Conteudo");

            migrationBuilder.AddColumn<string>(
                name: "Assunto",
                table: "Notificacoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "Notificacoes",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assunto",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Notificacoes");

            migrationBuilder.RenameColumn(
                name: "Conteudo",
                table: "Notificacoes",
                newName: "PayloadJson");
        }
    }
}
