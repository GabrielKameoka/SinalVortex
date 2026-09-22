# Investigação: notificações indo diretamente para a DLQ

## Problema e objetivo

Relato: novas notificações terminam na DLQ independentemente do canal, incluindo e-mail e WhatsApp. Objetivo desta correção: tornar o envio de e-mail local reproduzível e verificar API → Redis → Worker → SMTP, preservando falhas explícitas para canais indisponíveis.

## Evidências e diagnóstico

- `EmailNotificacaoService` lança `PermanentChannelException` quando `SmtpHost` ou `From` não estão preenchidos. O `appsettings.json` do Worker mantém ambos vazios; antes desta correção, `appsettings.Development.json` não fornecia configuração SMTP.
- `WhatsappNotificacaoService`, SMS e Push lançam falha permanente porque suas integrações ainda não foram implementadas. Não são falhas de roteamento da fila.
- `SignalProcessingWorker` envia falhas permanentes diretamente para `notificacoes:fila:dlq`. Falhas transitórias são reenfileiradas imediatamente, até o limite configurado na notificação, normalmente três tentativas. Não há backoff exponencial nesse fluxo.
- A suíte anterior passava com 8 testes unitários e 34 de integração. Entretanto, os três testes antigos de Worker copiavam sua lógica em um helper, enquanto a fixture HTTP substituía os provedores. Isso não comprovava envio SMTP pelo Worker real.
- A porta local 5432 pertence ao contêiner `repcortex-db`. A validação desta correção usa o PostgreSQL do SinalVortex em 5433 e Redis separado em 6380. O banco do outro projeto não foi acessado.
- Ao iniciar o processo real em `Development`, a validação de dependências impedia o Worker de subir: o registro MediatR varria todos os handlers da Application, incluindo autenticação, CRM e templates, sem os serviços exclusivos da API. A fixture HTTP não revelava isso porque usa o registro de serviços da própria API.

O código explica a DLQ imediata de e-mail sem SMTP e de WhatsApp indisponível. Não foram recuperados os logs do processo originalmente relatado; a reprodução controlada abaixo distingue evidência atual de hipótese sobre o incidente histórico.

## Decisão e alterações

- Usar Mailpit local, com SMTP em `127.0.0.1:1025` e interface em `127.0.0.1:8025`; fixar a imagem por digest.
- Configurar o SMTP apenas em `Development` no Worker. Produção continua exigindo configuração explícita; nenhum envio é simulado como sucesso.
- O Worker final consome somente as filas de notificações. O inbound webhook
  foi retirado da superfície executável da demo; o driver outbound continua
  disponível para evolução futura.
- Tornar a porta PostgreSQL do Compose configurável por `SINALVORTEX_POSTGRES_PORT`, mantendo 5432 como padrão.
- Substituir os testes que copiavam a lógica por testes HTTP que executam o Worker, dispatcher e serviços de e-mail/WhatsApp reais. Usar PostgreSQL/Redis isolados e servidor SMTP TCP local com respostas controladas.
- Preservar APIs, entidades, migrations, classificação de falhas e mensagens antigas. A captura local demonstra aceitação SMTP, não entrega externa.

## Como reproduzir e validar

Siga o [guia de envio local](../real-delivery.md#desenvolvimento-local-com-mailpit). Para reproduzir a falha de configuração, inicie o Worker com `EmailSettings__SmtpHost='' EmailSettings__From=''` junto das conexões isoladas. Publique uma nova notificação e verifique `Dlq`, uma tentativa e a mensagem `SMTP não configurado` no log do Worker. Reinicie sem esses overrides e publique outra mensagem: ela deve ser capturada no Mailpit e ficar como `Enviado`.

Execute a suíte:

```sh
dotnet test backend/SinalVortex.slnx --configuration Release
```

| Cenário automatizado | Resultado esperado |
| --- | --- |
| SMTP aceita e-mail nas prioridades baixa, normal e alta | Conteúdo capturado, `Enviado`, uma tentativa, ID ausente da DLQ |
| Configuração SMTP ausente | `Dlq`, uma tentativa, motivo persistido, nenhuma conexão SMTP |
| SMTP responde 550 | `Dlq` após uma tentativa, motivo permanente persistido |
| SMTP responde 451 | Três tentativas reais, depois `Dlq` com motivo persistido |
| WhatsApp sem integração | `Dlq`, uma tentativa, motivo explícito, nenhum envio SMTP |
| Registro do Worker | Container de DI válido, com processamento de notificações e sem endpoint inbound |

### Resultados desta execução

- Suíte Release: **8 testes unitários + 39 de integração aprovados**, sem falhas ou testes ignorados. Comando executado: `dotnet test backend/SinalVortex.slnx --configuration Release --no-restore`.
- Demonstração com API e Worker em processos separados, `Development`, PostgreSQL em 5433, Redis temporário em 6380 e Mailpit em 1025. O cadastro HTTP forneceu um JWT real para publicar e consultar as notificações.
- SMTP ausente: notificação `2faaad67-c9c3-4a13-ae6d-00ad73da5f57`, status `5` (`Dlq`), uma tentativa; log confirmou `SMTP não configurado`.
- SMTP de desenvolvimento: notificação `7345ee9b-66fe-4e27-9c3c-69a1240be2ef`, status `3` (`Enviado`), uma tentativa. Mailpit capturou destinatário, assunto e corpo esperados, mensagem `6flMMkAgs30bQB9FgWlTtv`.
- Consulta direta à lista Redis confirmou o ID da falha na DLQ e o ID enviado ausente. As filas preexistentes na porta 6379 não foram consumidas pela demonstração.
- API e Worker temporários foram encerrados após a validação. Os IDs acima são evidência desta execução local, não fixtures permanentes nem resultados de produção.
- O build ainda informa avisos preexistentes, incluindo `NU1903` em `Microsoft.OpenApi 2.0.0-preview5`. Atualização de dependências não foi incluída nesta correção.

## Limites e próximos problemas

- WhatsApp, SMS e Push continuam indisponíveis até a implementação de seus provedores. Webhook exige origem HTTPS autorizada.
- Mensagens antigas publicadas pelo reprocessamento anterior podem conter somente um GUID e não são compatíveis com o consumidor atual. A branch agora serializa `NotificacaoFilaItemDto` ao reprocessar; itens antigos precisam ser removidos ou tratados manualmente antes de uma validação limpa.
- Retentativas imediatas podem esgotar o limite rapidamente durante indisponibilidade. Backoff e garantias de entrega da fila ficam para uma evolução própria.
