using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinalVortex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PadronizaNomesAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE tabela text;
                BEGIN
                    FOREACH tabela IN ARRAY ARRAY['Usuarios', 'Notificacoes', 'Contatos', 'Aplicacoes'] LOOP
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'CreatedAt')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'CriadoEm') THEN
                            EXECUTE format('ALTER TABLE %I RENAME COLUMN "CreatedAt" TO "CriadoEm"', tabela);
                        END IF;
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'UpdatedAt')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'AtualizadoEm') THEN
                            EXECUTE format('ALTER TABLE %I RENAME COLUMN "UpdatedAt" TO "AtualizadoEm"', tabela);
                        END IF;
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE tabela text;
                BEGIN
                    FOREACH tabela IN ARRAY ARRAY['Usuarios', 'Notificacoes', 'Contatos', 'Aplicacoes'] LOOP
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'CriadoEm')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'CreatedAt') THEN
                            EXECUTE format('ALTER TABLE %I RENAME COLUMN "CriadoEm" TO "CreatedAt"', tabela);
                        END IF;
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'AtualizadoEm')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = tabela AND column_name = 'UpdatedAt') THEN
                            EXECUTE format('ALTER TABLE %I RENAME COLUMN "AtualizadoEm" TO "UpdatedAt"', tabela);
                        END IF;
                    END LOOP;
                END $$;
                """);
        }
    }
}
