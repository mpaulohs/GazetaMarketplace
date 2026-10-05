# Triagem do BACKLOG ao fechar a Fase 3 (Checkpoint 3)

> **Em resumo:** `plans/BACKLOG.md` tinha 112 itens abertos ao fechar a Fase 3. Esta lista só **classifica** (nenhum código mudou): o que precisa estar resolvido **antes do lançamento**, o que **entra nas Fases 4 e 5** e o que **depende de decisão do Product Owner**. Cada linha cita o item do BACKLOG pelo assunto. Itens de histórico que já foram atendidos mas ficaram sem `[x]` estão no fim, para conferir.

## 1. Antes do lançamento (bloqueadores)

| Item | Por que bloqueia |
|---|---|
| Carga real dos municípios do IBGE (`db/seed/cities.sql` via `tools/CitiesImport`) | Hoje só existe a amostra de 40 cidades |
| Exportação real do catálogo de veículos (A5) e o esquema presumido da origem (GazetaOnline) | Hoje só existe o catálogo reduzido de exemplo; depende de parecer jurídico e acesso somente leitura |
| AR-05: provar o Magick.NET (HEIC e WebP) na hospedagem Windows compartilhada | Só foi provado em Linux; sem o componente nativo o HEIC é recusado |
| HEIC de iPhone de verdade e JPEG com GPS real (orientação, perfil de cor, GPS) | Os arquivos de teste são sintéticos; a prova no site publicado (Checkpoint 3) usa esses |
| Publicar com `RuntimeIdentifier` fixo (`win-x64`) | A saída publicada tem 233 MB com os nativos de todas as plataformas |
| Runbook de implantação: `sqlcmd -I`, arquivos estáticos só da saída publicada, `Site__BaseUrl` obrigatório em Production | Sem `Site__BaseUrl` o site não sobe |
| Publicar as tarefas 1.1 e 1.2 juntas | Só a 1.2 cria o primeiro Administrador |
| Checklist de banco: `ARITHABORT ON` e `QUOTED_IDENTIFIER ON` no servidor de produção | As colunas calculadas persistidas dependem disso |
| Conferir que o site roda em um só processo | Os caches (árvore de categorias, catálogo, configurações) são por processo e demoram até 10 minutos para refletir mudanças |
| Verificação manual de CEP e municípios com a rede real | O ambiente de desenvolvimento bloqueia `viacep.com.br` e o IBGE |
| `?v=` na URL da foto antes de qualquer reprocessamento real | O cache `immutable` de 1 ano esconderia a foto nova (só vale se `IPhotoReprocessing` for usado) |
| Autocomplete de Marca e Tipo de produto (`/api/v1/brands/suggest` e `/api/v1/product-types/suggest`, tarefa 3.3b) | Os campos funcionam como texto livre, mas o contrato está no `openapi.yaml` e ainda não existe; decidir se entra antes do lançamento |

## 2. Vai para as Fases 4 e 5 (ou para uma tarefa técnica já prevista)

| Item | Onde entra |
|---|---|
| Telas provisórias "Meus anúncios" e "Fila de revisão" | 4.4 e 4.1 |
| E2E do reenvio de rejeitado (S04 da 3.7) | 4.2 (rejeitar) |
| Selo "Cidade/UF manual" na pré-visualização; `_AdBody` com galeria e bloco "Vaga de emprego" | 4.1 e 5.2 (US-003) |
| Coração, esqueleto de carregamento e `Href` do card; quem monta a lista usa `AdCardModel` e `AdCardCover.FromStored` | 5.1 e 5.2 |
| Filtros de faixa com decimais (`s-amin`, `s-amax`); ligação da cadeia marca → modelo → ano → versão aos filtros | 5.4 |
| Redirecionamento do slug antigo, se a busca/SEO precisar de slug novo | 5.1 |
| Lista de Tipo de peça por categoria quando o grupo Peças for exibido | 5.x |
| Diálogos de "Desativar" e "Redefinir senha" nas telas de usuários | melhoria progressiva futura |
| Diálogo Bootstrap na confirmação do envio (hoje é página própria) | opcional, sem mudança no servidor |
| Alinhar `architecture/design-system.md` §5.2 ("Serviço:") ao wireframe ("Tipo:") | próxima revisão do design-system |
| Renomear classes CSS e hooks `data-*` para inglês; texto neutro de gênero ("conta ativa") | refactor de front-end |
| Dívida de formatação: BOM em 14 arquivos antigos, `Test1.cs` do template, `MSTestSettings.cs` | commit só de formatação |
| Três cópias de "tirar acentos" (`Normalizer`, `SlugGenerator`, `CategoryRules`) | `/simplify` |
| `IPhotoReprocessing` sem chamador; log por arquivo apagado; limpeza lê a data do arquivo (backup) | revisar depois do lançamento |
| Cache de CEP com nome padronizado; `CitiesImport` sem `--prune`; dado vencido do ViaCEP; 429 do CEP tratado como indisponível | polimento do CEP, depois do lançamento |
| E2E: limites de pedidos do site de teste, ordem de rodada, segundo site em Development, duplicação de `FakeViaCep` nos E2E antigos | manutenção da suíte |
| Fila de e-mail em memória e limite diário do SendGrid | reavaliar se surgirem outros e-mails importantes |
| Sem JavaScript o envio de foto perde o que não foi salvo; fotos enviadas uma de cada vez; miniatura HEIC só depois do envio; galeria sem auditoria; troca de categoria sem trava de linha | limitações aceitas da v1 |

## 3. Decisão do Product Owner (não técnica)

| Item | Pergunta |
|---|---|
| Teto do preço: SPEC S28 diz R$ 9.999.999.999, ADR-004 diz R$ 99.999.999,99 | Confirmar o menor e alinhar o texto (o código já usa o menor) |
| Limites assumidos: Quartos/Banheiros/Vagas 0–20, km até 9.999.999, horas de uso, medidas, Temporada, Condomínio/IPTU, área, motivo de rejeição de 500 caracteres | Confirmar ou ajustar |
| SPEC v1.3 pendente: Redator vê o próprio anúncio Em revisão/Publicado/Arquivado em leitura; mensagem "Mova antes os anúncios desta categoria"; Motos na US-013-S08 | Emendar a SPEC |
| Texto "Este anúncio não pode ser editado" para Publicado e Arquivado | Confirmar o texto |
| Roupas e calçados com Marca; lista fechada de marcas no Eletro; AVIF | Hoje fora da v1 |
| IDs de categoria 24, 25 e 32 ausentes | Foi proposital? |
| Administrador reenviar anúncio Em revisão/Publicado | Hoje não pode (409) |
| Rastro (auditoria) de quem mexeu nas fotos; arrastar para reordenar | Hoje não existe |
| Excluir a última subcategoria não devolve o status de postável ao pai | Confirmar o comportamento |
| Parecer jurídico e acesso somente leitura ao GazetaOnline (A5) | Destrava o catálogo real |
| Amostras reais (HEIC de iPhone, JPEG com GPS) | Para a verificação do item 1 |
| Chave do Google Maps no histórico da branch antiga | Decidido: não reescrever o histórico (manter registrado) |

## Itens que parecem já atendidos mas seguem sem `[x]` (conferir e marcar)

Teto de preço aplicado na 3.3 · remoção de `Test1.cs` e renomeação das migrations · troca de `ICategoryUsage` pela implementação real (3.1) · ligação da cadeia do catálogo ao formulário (3.3) · pendências do envio (3.7, citadas como "da 3.7" nos itens da 3.3).
