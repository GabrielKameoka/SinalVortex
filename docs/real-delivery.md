# Dados e envios reais

A Inbox usa GET /api/v1/notificacoes, com paginação e atualização a cada 5 segundos. Não contém conversas, contatos, tags, respostas ou contadores não lidos de demonstração. O dashboard já usa a API. Registros históricos podem ter sido marcados como enviados pelos drivers simulados antigos: não foram apagados nem reclassificados.

O monitor lê os tamanhos reais das quatro listas compartilhadas do Redis. Os contadores são globais; os eventos são separados por tenant. A API publica eventos ao enfileirar, e o Worker publica consumo e resultado. O cliente consulta novamente o snapshot a cada 3 segundos e tenta reconectar. Eventos recebidos durante a conexão ficam limitados a 50; não existe histórico persistido desses logs.

## E-mail SMTP

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
