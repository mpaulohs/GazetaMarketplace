# Checklist do dia da publicação (go-live)

> **Em resumo:** uma página, na ordem, para **você** (Product Owner) publicar o GazetaMarketplace no SmarterASP e só então divulgar o endereço. Marque `[x]` ao concluir cada item. **Quem escreveu o site não publica:** a publicação é sua. Os motivos e os comandos completos estão no [`DEPLOY-RUNBOOK.md`](DEPLOY-RUNBOOK.md); as conferências manuais, no [`VERIFY-CHECKLIST.md`](VERIFY-CHECKLIST.md).

**Regra de ouro:** se um item marcado **[PARE]** falhar, **não divulgue o endereço**. Anote o número do item, o que apareceu e uma captura de tela, e me avise. O site pode ficar no ar sem divulgação enquanto isso.

---

## A. Antes do dia (pode ser feito em dias anteriores)

- [ ] **A1. Certificado HTTPS grátis solicitado** na aba **SSL** do painel do SmarterASP (AR-09) e emitido. Sem ele, o item D2 não passa.
- [ ] **A2. E-mail do site:** domínio remetente validado no SendGrid, com SPF e DKIM (AR-12), para o e-mail de redefinição de senha não cair no spam.
- [ ] **A3. Google Maps:** no Google Cloud Console, a chave antiga aparece como **revogada** (SC-28). É uma conferência de 1 minuto.
- [ ] **A4. Três pastas criadas** dentro de `h:\root\home\mpaulohs-001\www\`, ao lado da pasta do site: `gazeta-fotos`, `gazeta-chaves`, `gazeta-logs` (RUNBOOK §3).
- [ ] **A5. Banco SQL Server 2022 criado** no painel; servidor, nome, usuário e senha anotados no cofre de senhas.
- [ ] **A6. `web.Production.config` preenchido** a partir do `web.Production.config.example` (fora do git): cadeia de conexão com `Encrypt=True`, `SendGrid__ApiKey`, `SendGrid__FromEmail`, `Site__BaseUrl` com `https://` e sem barra no fim, e **só desta vez** `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword`. Confirme que `DataProtection__ProtectWithDpapi` está **`false`**.
- [ ] **A7. Pacote gerado:** `dotnet publish src/GazetaMarketplace.Web -c Release -p:PublishProfile=IIS-win-x64` (cerca de 62 MB; RUNBOOK §2).

## B. Publicação (RUNBOOK §7)

- [ ] **B1. Backup do banco** (ou confirmação do backup automático do plano).
- [ ] **B2. Script do banco:** `sqlcmd -S (servidor) -U (usuario) -P (senha) -d (banco) -b -I -i db/scripts/gazeta-idempotente.sql`. Esperado: termina sem erro.
- [ ] **B3. Publicar o pacote** na hospedagem.
- [ ] **B4. `https://(site)/health/ready` responde `Healthy`.** [PARE] se não responder: veja o log em `gazeta-logs` (a mensagem diz qual variável falta).
- [ ] **B5. Primeiro acesso:** entre com o Administrador, **troque a senha provisória**, remova `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` do `web.Production.config` e reinicie o site.
- [ ] **B6. As três pastas ganharam arquivos** sem você ter dado permissão nenhuma: um log do dia em `gazeta-logs` e um `key-*.xml` em `gazeta-chaves`. [PARE] se não: o pool não tem escrita nelas.

## C. Teste prático do IP (RUNBOOK §7.1) — decide se há proxy a configurar

- [ ] **C1. Erre a senha de propósito** uma vez em `/painel/entrar`.
- [ ] **C2. Leia o log do dia** e ache `Falha de entrada de ... a partir de <IP>`.
- [ ] **C3. O IP é o seu?** Sim: nada a configurar. Não (sempre o mesmo, do provedor): grave-o em `ForwardedHeaders__KnownProxies__0`, reinicie e repita. [PARE] enquanto o IP visto não for o real: sem ele, a equipe inteira divide o limite de tentativas.

## D. Conferências que bloqueiam a divulgação (VERIFY-CHECKLIST)

Primeiro a preparação: **P1** criar um Redator, **P2** informar o telefone do site em `/painel/configuracoes`, **P3** cadastrar e publicar 3 anúncios reais com fotos de verdade.

- [ ] **D1. M6 — fotos reais:** HEIC de iPhone e JPEG grande de Android aceitos; orientação e cor certas; **nenhuma linha de GPS** (`exiftool foto.webp | grep -i gps`). [PARE] se aparecer GPS (privacidade) ou se a HEIC não converter (o componente de imagens não carregou no Windows do provedor, AR-05).
- [ ] **D2. M7 — HTTPS:** `http://` redireciona para `https://`; cadeado sem aviso; cabeçalhos `strict-transport-security`, `content-security-policy`, `x-content-type-options`, `x-frame-options`, `referrer-policy` presentes; um só `content-encoding`. [PARE] se o certificado não estiver válido.
- [ ] **D3. M10 — nunca página branca:** `/qualquer-coisa-que-nao-existe` mostra "Página não encontrada"; anúncio inexistente mostra "Este anúncio não está mais disponível"; formulário com aba velha mostra "Algo deu errado", nunca branco. [PARE] se aparecer página branca.

## E. Divulgar

- [ ] **E1. D1, D2 e D3 passaram.** Só então divulgue o endereço.
- [ ] **E2. Avise a equipe** sobre o limite de tentativas de entrada (5 erros por 15 minutos por IP) e sobre a troca da senha provisória.

## F. Depois de divulgar (não bloqueiam; viram correções se falharem)

- [ ] **F1. DPAPI (RUNBOOK §7.2):** com o site funcionando, ligar `DataProtection__ProtectWithDpapi=true`, reiniciar, entrar de novo, abrir um link de redefinição e conferir que o `key-*.xml` novo cita `dpapi`. Se der erro, volte a `false`.
- [ ] **F2. Itens M1 a M5, M8, M9 e M11** do `VERIFY-CHECKLIST.md` (celular, WhatsApp, leitor de tela, Firefox, Safari, CEP real, pré-visualização do link, e-mail real, sessão, métricas de velocidade na hospedagem).
- [ ] **F3. Toda semana:** olhar o tamanho de `gazeta-fotos` (SC-05); o disco é de 30 GB com limite flexível e não há alerta automático.
- [ ] **F4. Primeiros 7 dias:** olhar o LCP p75 e as visitas por dia (V-07) para decidir se vale pensar em CDN ou cache.

---

**Se algo der errado depois de publicado:** guarde sempre o pacote da versão anterior e volte a publicá-lo. Se o problema envolver o banco, restaure o backup do item B1 (o script do banco não tem "desfazer").
