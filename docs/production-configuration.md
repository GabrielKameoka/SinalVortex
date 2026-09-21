# Configuração de produção

## Login e CORS

O Angular usa `https://sinalvortex-production.up.railway.app/api/v1`, sem a porta
interna 8080. Essa URL é pública e não é uma chave secreta.

Com `ASPNETCORE_ENVIRONMENT=Production` (ou sem sobrescrever o ambiente padrão),
a API carrega `appsettings.Production.json`, autorizando somente a origem
`https://sinal-vortex.vercel.app`. Desenvolvimento mantém `http://localhost:4200`.
Não usar `AllowAnyOrigin` com credenciais nem liberar todos os previews da Vercel.

Variáveis de ambiente têm precedência sobre JSON: no Railway, se existir
`Cors__AllowedOrigins__0`, seu valor deve ser `https://sinal-vortex.vercel.app`.
Revise também índices adicionais para não manter origens antigas autorizadas.
O nome da variável leva dois underscores. Não incluir `/api/v1` nem barra final.

Após publicar API e frontend, verificar o preflight:

```sh
curl -i -X OPTIONS https://sinalvortex-production.up.railway.app/api/v1/autenticacao/token \
  -H 'Origin: https://sinal-vortex.vercel.app' \
  -H 'Access-Control-Request-Method: POST' \
  -H 'Access-Control-Request-Headers: content-type'
```

A resposta deve incluir `Access-Control-Allow-Origin: https://sinal-vortex.vercel.app`.
HTTP 204 sozinho não confirma que CORS está autorizado.

## Segredos

Use `.env.example` como referência, sem preencher ou versionar segredos nele.
Não é necessário adicionar biblioteca dotenv: o .NET já lê variáveis do processo,
mas **não carrega arquivos `.env` automaticamente**. O Docker Compose atual contém
apenas infraestrutura local; criar `.env` não injeta variáveis na API/Worker.
Em desenvolvimento, use variáveis exportadas ou .NET user-secrets; em produção,
configure as variáveis diretamente nos serviços Railway.

- API: `Jwt__SigningKey` aleatória, com pelo menos 32 bytes. Nunca no Angular.
- API e Worker: `ConnectionStrings__DefaultConnection` e
  `ConnectionStrings__RedisConnection`, com credenciais de produção. Ambos devem
  usar o mesmo banco/Redis. As credenciais postgres/postgres do repositório são locais.
- Worker: credenciais SMTP (`EmailSettings__Username` e `EmailSettings__Password`),
  host, porta, TLS e remetente para envio real. Consulte `real-delivery.md`.
- Webhooks de saída: lista de destinos HTTPS permitidos não é segredo.
- Webhooks de entrada: há um risco existente no controller: validação de assinatura
  fica desligada sem `Webhooks__ValidarAssinatura=true` e há fallback de segredo
  conhecido. Configure segredo exclusivo em `Webhooks__<provedor>__Secret` para
  cada provedor antes de habilitar entrada pública. Isso não foi alterado nesta
  correção de login; a implementação também precisa de endurecimento para falhar
  de forma segura quando faltar configuração.

Angular não precisa de chave de API, chave JWT ou senha SMTP. Qualquer valor
incluído em seu bundle é público, mesmo vindo de variável de ambiente de build.
SMS, WhatsApp e Push ainda não têm provedores implementados; apenas adicionar
chaves não habilita esses canais. Não há necessidade de chave de IA.

Arquivos `.env` e `.env.*` são ignorados pelo Git, exceto `.env.example`.
Essa proteção não remove segredos já versionados: nesses casos, revogue/rotacione.
