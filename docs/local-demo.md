# Demonstração local

> Para apresentar a ideia sem subir a infraestrutura, use a rota pública
> `/demo`. Ela simula o inbox e o envio no navegador; este documento descreve
> o fluxo completo com serviços locais.

## Iniciar

Requer Docker Engine/Desktop com Compose e portas 4200, 5287 e 8025 livres.
O primeiro build baixa imagens e dependências; não requer contas de provedores,
chaves Resend/SendGrid, domínio, variáveis Railway ou plano pago de hospedagem.

```sh
docker compose -f docker-compose.local.yml config --quiet
docker compose -f docker-compose.local.yml up --build
```

O Compose usa um projeto próprio (`sinalvortex-local`) e volumes próprios.
Não execute simultaneamente com serviços manuais nas mesmas portas.
As únicas portas publicadas ficam em loopback: Angular 4200, API 5287 e Mailpit 8025.
PostgreSQL 5432, Redis 6379 e SMTP 1025 são acessíveis apenas na rede Docker.
API e Worker compartilham as conexões internas; o Worker aguarda a saúde da API
após suas migrations. A chave JWT e a senha do banco são exclusivas de demonstração.
Não utilize este Compose em produção.

## Roteiro para demonstração

1. Abra http://localhost:4200 e cadastre uma conta em **Criar conta**. Cada cadastro cria um tenant.
2. Entre no dashboard e clique em **Nova notificação**.
3. Mantenha E-mail, informe `recrutador@example.test`, assunto e conteúdo. A
   tela usa `POST /api/v1/notificacoes` e mostra o resultado da aceitação pela
   API.
4. Após a resposta `201`, a tela mostra o ID. No inbox, aguarde a atualização automática e confirme **Enviado**.
5. Abra http://localhost:8025 e confira a captura da mensagem. Isso comprova captura SMTP no sandbox, não entrega externa.
6. Mantenha o monitor de filas aberto e conectado. Em outra aba, faça login com o mesmo Tenant ID e crie notificações por WhatsApp e SMS usando `5511999999999`. Confirme **Enviado** no inbox e o evento de sucesso do provedor simulado local. Nenhum número real é contatado.
7. Confira que os três canais aparecem na mesma conversa do destinatário e que nenhum deles foi para a DLQ. O inbox e os eventos do monitor são isolados pelo tenant autenticado; os contadores das filas são globais.

O formulário expõe E-mail, WhatsApp, SMS e Push. Webhook permanece no contrato da API,
mas não faz parte deste roteiro, pois exigiria configurar um receptor.
O Mailpit deste Compose não tem relay SMTP configurado.

## Operação e persistência

```sh
docker compose -f docker-compose.local.yml ps
docker compose -f docker-compose.local.yml logs worker
docker compose -f docker-compose.local.yml restart
```

Após o reinício, faça login e confirme os registros no inbox. PostgreSQL mantém os
registros em volume e Redis usa AOF em volume. A caixa do Mailpit é temporária e pode
ser perdida quando o container for recriado. O Worker deve registrar `Hosting environment: Development`.

Para parar preservando os dados:

```sh
docker compose -f docker-compose.local.yml down
```

O uso de `down --volumes` apaga os dados locais. O `docker-compose.yml` original
continua disponível para desenvolvimento manual, e os arquivos Railway/produção
permanecem separados, sem participar do fluxo recomendado.

## Verificação de desenvolvimento

Com .NET 10, Node compatível com Angular 19, Chrome e Docker disponíveis no host:

```sh
dotnet test backend/SinalVortex.slnx --configuration Release
cd frontend
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Validação realizada em 21/09/2026

- Compose validado e os seis serviços construídos/iniciados; API saudável e Worker em Development.
- Cadastro pelo Angular do container: HTTP 201; login e dashboard verificados.
- E-mail criado pelo formulário: HTTP 201, status Enviado, uma tentativa e mensagem capturada no Mailpit.
- SMS e WhatsApp criados pelo formulário: HTTP 201, status Enviado pelo provedor simulado local e evento de sucesso no monitor conectado.
- Reinício dos seis containers: API saudável e as duas notificações preservadas no inbox.
- Backend: 8 testes unitários e 39 de integração aprovados.
- Frontend: `npm ci`, build de produção e 21 testes aprovados. ChromeHeadless usou o Brave/Chromium instalado via `CHROME_BIN`.
- Formulário inspecionado em viewport de 390 px, sem rolagem horizontal.

Nesta máquina havia um `ng serve` anterior em IPv6 na porta 4200. A verificação
foi direcionada ao IPv4 do Docker. Para repetir a demo com `localhost`, encerre
servidores anteriores nessas portas. O Mailpit anterior na porta 8025 foi pausado
somente durante a validação e restaurado ao final.
