# TODO: GazetaMarketplace v1

> Projeção de `plans/plan.md`: cada linha repete o id e o título da tarefa. O `/build` marca `- [x]` ao concluir; nada de escopo novo entra aqui.

## Fase 0 — Fundação (concluída em 2026-10-03)
- [x] Task 0.1: Estruturar a solução em Web, Core e Infrastructure
- [x] Task 0.2: Configuração tipada, segredos fora do repositório e ambiente de desenvolvimento
- [x] Task 0.3: Logs estruturados, correlação e contrato de erros
- [x] Task 0.3b: Migrar pacotes Microsoft de rc.2/preview para GA 10.0.12
- [x] Task 0.4: Segurança HTTP: cabeçalhos, antiforgery, limites de requisição e HTTPS
- [x] Task 0.5: Verificações de saúde, cultura pt-BR e fuso
- [x] Task 0.6: Persistência base: DbContext, Dapper, auditoria, concorrência e script de migrations
- [x] Task 0.7: Layout base, tokens do design system e componentes de estado de página

## Checkpoint 0 — Fundação completa (fechado em 2026-10-03)

## Fase 1 — Equipe e acesso
- [x] Task 1.1: Entrar e sair do painel, com bloqueio e sessão
- [x] Task 1.2: Primeiro acesso e Administrador inicial
- [x] Task 1.3: Gerenciar usuários da equipe
- [x] Task 1.4: Recuperar senha esquecida por e-mail

## Checkpoint 1 — Equipe completa

## Fase 2 — Categorias, campos e catálogo
- [x] Task 2.1: Árvore de categorias e carga inicial
- [x] Task 2.2: Grupos de campos: framework e grupos Serviços, Vagas, Produtos em geral e Imóveis
- [x] Task 2.3: Grupos de campos de veículos e peças
- [x] Task 2.4: Grupos de campos de aluguel, telefonia, eletro, eletrônicos, roupas e máquinas
- [x] Task 2.5: Catálogo de veículos: tabelas, consulta encadeada e ferramenta de exportação
- [x] Task 2.6: Gerenciar categorias
- [x] Task 2.7: Telefone/WhatsApp do site

## Checkpoint 2 — Categorias e catálogo completos

## Fase 3 — Anúncios (equipe)
- [x] Task 3.1: Modelo do anúncio, situações e autorização por autoria
- [x] Task 3.2: Consulta de CEP no servidor, com cache e lista de municípios
- [x] Task 3.3: Criar e editar rascunho do anúncio
- [x] Task 3.4: Processamento e armazenamento de fotos
- [x] Task 3.5: Enviar, reordenar e remover fotos do anúncio
- [x] Task 3.6: Limpeza diária dos originais de foto
- [x] Task 3.7: Enviar anúncio para revisão
- [x] Task 3.8: Componentes de apresentação do anúncio: card, valor e bloco sem foto

## Checkpoint 3 — Anúncios completos (fechado em 2026-10-05)
- [x] Rascunho com fotos e envio para revisão funcionam para Carros, Serviços e Vagas
- [x] Fotos: HEIC convertido, GPS removido, `_originals/` sem rota
- [x] Teste diferencial das colunas calculadas escrito e passando

## Fase 4 — Revisão e ciclo de vida
- [x] Task 4.1: Fila de revisão e pré-visualização
- [x] Task 4.2: Publicar e rejeitar anúncios
- [x] Task 4.3: Despublicar e arquivar anúncios
- [ ] Task 4.4: Lista de anúncios do painel

## Checkpoint 4 — Revisão completa

## Fase 5 — Site público
- [ ] Task 5.1: Página inicial e páginas de categoria
- [ ] Task 5.2: Detalhe do anúncio e galeria de fotos
- [ ] Task 5.3: Contato por telefone e WhatsApp
- [ ] Task 5.4: Busca e filtros
- [ ] Task 5.5: Favoritos no navegador
- [ ] Task 5.6: SEO básico das páginas públicas

## Checkpoint 5 — Site público completo

## Fase 6 — Verificações transversais
- [ ] Task 6.1: Verificação transversal de acesso e de texto digitado
- [ ] Task 6.2: Base de verificação de acessibilidade e responsividade
- [ ] Task 6.3: Orçamentos de desempenho

## Checkpoint 6 — Verificações transversais completas

