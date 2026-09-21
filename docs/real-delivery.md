# Dados e envios reais

A Inbox usa GET /api/v1/notificacoes, com paginação e atualização a cada 5 segundos. Não contém conversas, contatos, tags, respostas ou contadores não lidos de demonstração. O dashboard já usa a API. Registros históricos podem ter sido marcados como enviados pelos drivers simulados antigos: não foram apagados nem reclassificados.

O monitor lê os tamanhos reais das quatro listas compartilhadas do Redis. Os contadores são globais; os eventos são separados por tenant. A API publica eventos ao enfileirar, e o Worker publica consumo e resultado. O cliente consulta novamente o snapshot a cada 3 segundos e tenta reconectar. Eventos recebidos durante a conexão ficam limitados a 50; não existe histórico persistido desses logs.

## E-mail SMTP

### Desenvolvimento local com Mailpit

Pré-requisitos: .NET 10, Docker com Compose e `curl`. Execute os comandos a partir da raiz do repositório. O Mailpit recebe SMTP localmente e permite conferir o conteúdo em uma interface web; não entrega a mensagem a uma caixa externa. Sua imagem está fixada por digest para reproduzir o ambiente validado.

1. Inicie a infraestrutura. Neste exemplo, PostgreSQL usa a porta **5433** para evitar conflito com outros projetos:

   ```sh
   SINALVORTEX_POSTGRES_PORT=5433 docker compose up -d postgres redis mailpit
   ```

2. Em um terminal, inicie a API com o banco correto e uma chave JWT temporária:

   ```sh
   export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=5433;Database=sinalvortex;Username=postgres;Password=postgres'
   export ConnectionStrings__RedisConnection='127.0.0.1:6379'
   export Jwt__SigningKey="$(openssl rand -hex 32)"
   dotnet run --project backend/SinalVortex.API --launch-profile http
   ```

3. Em outro terminal, configure as mesmas conexões e habilite explicitamente `Development` no Worker:

   ```sh
   export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=5433;Database=sinalvortex;Username=postgres;Password=postgres'
   export ConnectionStrings__RedisConnection='127.0.0.1:6379'
   dotnet run --project backend/SinalVortex.Worker --launch-profile Development
   ```

   O perfil `Development` do Worker define `DOTNET_ENVIRONMENT` e `ASPNETCORE_ENVIRONMENT` explicitamente. Na IDE, selecione esse perfil ou defina as duas variáveis na configuração de execução, além das conexões. O ambiente da API não se propaga para outro processo. O Worker carrega `appsettings.Development.json` com host `127.0.0.1`, porta `1025`, `EnableSsl=false` e remetente `sinalvortex@example.test`. Confirme no log a linha `Hosting environment: Development`. Variáveis `EmailSettings__*` e user-secrets existentes podem sobrescrever esses valores; remova overrides antigos da configuração de execução local. Um arquivo `.env` não é carregado automaticamente pelo .NET.

4. Cadastre um usuário de teste por `POST /api/v1/autenticacao/registrar`, com `nome`, `email` e `senha`, conforme [cadastro e autenticação](user-registration.md). Guarde o `accessToken` e use-o no POST abaixo. Exemplo em zsh, sem imprimir o token:

   ```sh
   read -rs 'SINALVORTEX_TOKEN?Access token: '
   curl --fail-with-body http://localhost:5287/api/v1/notificacoes \
     -H "Authorization: Bearer $SINALVORTEX_TOKEN" \
     -H 'Content-Type: application/json' \
     -d '{"aplicacaoId":"22222222-2222-2222-2222-222222222222","destinatario":"demo@example.test","canal":1,"prioridade":2,"assunto":"Validacao SMTP local","conteudo":"Mensagem processada pelo Worker e capturada no Mailpit."}'
   ```

5. Consulte `GET /api/v1/notificacoes/{id}` com o mesmo Bearer token até obter `status: 3` (`Enviado`), normalmente com `tentativas: 1`. Abra <http://localhost:8025> e confira destinatário, assunto e conteúdo. O monitor deve registrar consumo e sucesso, sem DLQ para esse ID.

O status `Enviado` significa aceitação pelo servidor SMTP. Neste ambiente, comprova captura no Mailpit, não entrega externa ou leitura. Use mensagens novas para validar; não mova manualmente itens entre listas do Redis. O reprocessamento manual possui uma incompatibilidade de payload documentada na [investigação](investigations/notificacoes-dlq.md).

Para encerrar, pare os processos com Ctrl+C. `docker compose stop mailpit postgres` interrompe apenas esses serviços; não apaga volumes. O teste automatizado não exige que esta infraestrutura esteja ativa: utiliza PostgreSQL/Redis próprios e SMTP em porta efêmera.

Referências: [Docker do Mailpit](https://mailpit.axllent.org/docs/install/docker/) e [SMTP sem TLS/autenticação por padrão](https://mailpit.axllent.org/docs/configuration/smtp/).

### Provedor SMTP externo

Configure no ambiente do Worker (não commite senhas):

- `EmailSettings__SmtpHost`
- `EmailSettings__SmtpPort` (padrão 587, SMTP com STARTTLS)
- `EmailSettings__EnableSsl` (padrão true)
- `EmailSettings__From`
- `EmailSettings__Username`
- `EmailSettings__Password`

Somente a aceitação do envio pelo servidor SMTP retorna sucesso. Isso não comprova entrega nem leitura. Não há mais fallback que simule sucesso. Porta 465/SSL implícito não é suportada por este transporte; use STARTTLS.

## Webhook

Configure `WebhookSettings__AllowedOrigins__0` (e índices seguintes) com origens HTTPS controladas, por exemplo `https://seu-receptor.example`. Sem origem autorizada, não há envio. O Worker faz POST com notificacaoId, assunto e conteudo; usa o ID como Idempotency-Key. O receptor deve deduplicar por esse ID. Redirecionamentos não são seguidos. Respostas 2xx indicam aceitação; 408, 429 e 5xx são falhas transitórias; outros códigos são falhas permanentes. Não configure origens não confiáveis ou que resolvam para serviços internos.

## Integrações pendentes

SMS, WhatsApp e Push ainda precisam da escolha e das credenciais dos provedores. Até a integração estar configurada, geram uma falha explícita e seguem para DLQ, sem registrar envio fictício. Reinicie API e Worker após atualizar o código. Não reprocessamos mensagens antigas automaticamente.

Os testes substituem os provedores externos apenas dentro do host de teste. Há um teste SMTP com servidor TCP local e testes HTTP dos Webhooks. Nenhum teste envia mensagens a contatos reais. Referência do transporte HTTP: https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
