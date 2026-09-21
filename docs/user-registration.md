# Cadastro público
A rota /registro usa POST /api/v1/autenticacao/registrar com nome, email e senha.
Cada cadastro cria um tenant próprio e uma sessão autenticada. O Tenant ID fica
visível na confirmação e no dashboard; guarde-o para entrar novamente.
A confirmação de senha não sai do navegador. Não são enviados e-mails e a conta
não é apresentada como e-mail verificado.

O mesmo e-mail pode pertencer a tenants diferentes. Não há convites, recuperação
de senha ou login social nesta entrega. O frontend não repete automaticamente
o POST: uma falha de rede pode ocorrer depois de a API persistir a conta.

Antes de divulgar amplamente o cadastro público, implementar limites antiabuso
no servidor e limites de envio por tenant. A interface não substitui controles
de segurança na API.

Validação: build Angular de produção, testes Angular e RegistrationTests com
PostgreSQL/Redis isolados via Testcontainers. Nunca usar produção para criar
contas durante verificações automatizadas.

