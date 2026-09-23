# Monitor de filas — Issue #09

## Fluxo

Angular conecta ao Hub autenticado `/hubs/queue-monitor` usando o JWT. O tenant é extraído do token, nunca de um grupo informado pelo cliente. A conexão expira com a autenticação.

API e Worker usam o mesmo Redis. O enfileiramento publica um evento, mesmo com o Worker parado. O Worker publica consumo, resultado, nova tentativa e DLQ. A API retransmite via SignalR apenas os logs do tenant correspondente. O cliente mantém os últimos 50 eventos recebidos desde a conexão; não existe replay histórico de Pub/Sub.

Os quatro contadores consultam as listas compartilhadas do Redis e são globais, como indicado na tela. Os snapshots são atualizados pelos eventos e também a cada três segundos. Uma fila vazia exibe zero; dados ainda não recebidos exibem um traço. Os valores são reais e recebem uma animação visual, desativada com a preferência de movimento reduzido.

## Executar e conferir

1. Disponibilize PostgreSQL e Redis; configure as conexões da API e do Worker.
2. Configure JWT na API e inclua a origem Angular em `Cors:AllowedOrigins`.
3. Reinicie a API e o Worker após recompilar. `ng serve` não atualiza os processos .NET.
4. Execute `npm ci` e `npm start` dentro de `frontend`.
5. Faça login, abra `/monitor-filas` e espere o estado conectado.
6. Faça um POST de notificação no mesmo tenant. Entrada, consumo e resultado aparecem sem recarregar.
7. No modo local, SMS e WhatsApp usam provedores simulados e geram sucesso sem contato externo. Um provedor realmente não configurado, como Push, deve gerar falha/DLQ, nunca envio fictício. Consulte `real-delivery.md` para SMTP e Webhook.

Mensagens consumidas rapidamente podem entrar e sair entre dois snapshots. O feed registra os eventos observados mesmo quando o contador volta a zero. Eventos perdidos durante desconexão não são recriados.

## Validação automatizada

- `dotnet test backend/SinalVortex.slnx --configuration Release`
- Em `frontend`: `npm run build` e `npm test -- --watch=false --browsers=ChromeHeadless`
- Em máquinas sem Chrome, defina `CHROME_BIN` para um navegador Chromium disponível.

Testes de integração usam PostgreSQL/Redis isolados e WebSocket real: POST com Worker, POST sem Worker, falha permanente, retentativas até DLQ e isolamento dos logs. Provedores externos são substituídos exclusivamente no host de testes. SMTP tem teste de protocolo em servidor TCP local. Nenhum teste envia mensagens a contatos reais.

## Limites desta entrega

Não adiciona IA nem chamadas pagas de IA. SMS, WhatsApp e Push dependem de integrações futuras; a branch não alega que esses canais enviam mensagens. Os registros antigos não foram apagados nem convertidos em comprovantes de entrega real. A confirmação do provedor não equivale à leitura pelo destinatário.
