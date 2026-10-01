# ADR-001: Monólito modular com Clean Architecture em 3 projetos

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** o GazetaMarketplace é um único site (um processo, uma publicação) dividido em três projetos — Web, Core e Infrastructure — e, dentro deles, em 8 módulos com fronteiras claras. É o formato padrão do kit e o único que cabe na hospedagem compartilhada.

## Context
- O SPEC (v1.1) descreve um site público e um painel da equipe sobre os mesmos dados (US-001 a US-015), com escala pequena: ~200 anúncios ativos e ~1.000 visitas por dia (NFR-04).
- A produção é um IIS de hospedagem compartilhada (SmarterASP.NET), publicado por WebDeploy: um site por publicação, sem contêineres nem serviços separados (`ARCHITECTURE.md` §2).
- `rules/principles-and-practices.md` §3.1 define o monólito modular com Clean Architecture como padrão; qualquer desvio exige ADR.
- O repositório já tem `src/GazetaMarketplace.Web` e os projetos de teste, criados a partir do template.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Monólito modular: `Web` + `Core` + `Infrastructure`** | Uma publicação só; regras testáveis sem banco nem HTTP; dependências apontam para o Core; é o padrão do kit | Três projetos para manter; exige disciplina para um módulo não ler as tabelas de outro |
| B. Um único projeto `Web` com pastas | Menos arquivos de projeto | As regras ficam presas ao ASP.NET e ao EF Core; testes de unidade mais difíceis; fronteiras só por convenção |
| C. Site e API separados (dois processos) | Separa leitura pública e painel | Dois sites para publicar e configurar na hospedagem compartilhada; latência extra; nenhuma NFR pede isso |
| D. Microsserviços | Escala e deploy independentes | Custo operacional sem justificativa na escala da v1 (MonolithFirst); inviável na hospedagem |

## Decision
Adopt **Option A** because atende a NFR-04 com folga num processo só, é o único formato compatível com a publicação por WebDeploy num site compartilhado e segue o padrão de `principles-and-practices.md` §3.

## Consequences
**Positive**: uma publicação e uma configuração; regras de negócio no Core, testáveis com MSTest sem infraestrutura; integrações atrás de interfaces (troca de fotos, CEP, e-mail e catálogo sem mexer nas regras).
**Negative**: site público e painel competem pelo mesmo processo; os módulos compartilham o mesmo `AppDbContext`.
**Risks**: um módulo acessar tabelas de outro e criar acoplamento. Mitigação: cada módulo expõe serviços no Core; revisão de código (`/review`) confere as referências entre módulos.

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- Dois dos gatilhos de `principles-and-practices.md` §3.2 ocorrerem (por exemplo, equipe de desenvolvimento acima de 15 pessoas ou um módulo precisar de escala 10 vezes maior que os demais).
- O painel e o site público precisarem de janelas de publicação independentes.
- A hospedagem sair do IIS compartilhado para uma plataforma com contêineres.

## Implementation Notes
- Criar `src/GazetaMarketplace.Core` e `src/GazetaMarketplace.Infrastructure`; `Web` referencia os dois; `Infrastructure` referencia só o `Core`; o `Core` não referencia pacotes de ASP.NET nem de EF Core.
- Módulos como pastas e namespaces dentro do Core (`GazetaMarketplace.Core.Anuncios`, `…Vitrine`, `…Fotos`, `…Categorias`, `…Equipe`, `…Configuracoes`, `…Localizacao`, `…CatalogoVeiculos`); o painel como área MVC `Painel` no `Web`.
- Registro de dependências por métodos de extensão (`AddCore()`, `AddInfrastructure(IConfiguration)`), chamados no `Program.cs`.
- Projetos de exemplo do template (`ClaudeStack.*`, `Example.*`) saem da solução do produto: tarefa de limpeza no `/plan`.
- Respeitar `Directory.Build.props` (`Nullable` e `ImplicitUsings` desligados, `TreatWarningsAsErrors`) e Central Package Management.
