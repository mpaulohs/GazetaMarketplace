# Consolidação: campos de anúncio por categoria (Etapa 6)

> ⚠️ **Superado pelo SPEC (2026-09-30):** o conteúdo foi aplicado a `specs/SPEC.md` (S3, S4, S12, S16, US-003, US-008, US-009, NFR-12, Out of Scope, Apêndice B e A1 a A5), com as definições de Serviços e Vagas de emprego que substituem as propostas daqui. Em caso de diferença, vale o SPEC. Mantido como registro da descoberta.

> **Em resumo:** fecha a descoberta das Etapas 1 a 5. Diz quais categorias entram na primeira versão, quais campos cada uma terá, o que ainda depende de decisão do Product Owner e o texto exato que será aplicado ao `specs/SPEC.md` depois dessas decisões. **Nada aqui altera o SPEC**: as recomendações estão marcadas como recomendação, e o SPEC só muda com a aprovação do Product Owner.

## Documentos da descoberta

| Etapa | Documento | O que traz |
|---|---|---|
| 1 | `categories-active.md` | 129 categorias ativas (IsPostable = 1) em 22 categorias-pai; achados da árvore |
| 2 | `gazetaonline-fields.md` | Campos, tipos e obrigatoriedade das 29 categorias que tinham formulário no GazetaOnline |
| 3 | `field-mapping.md` | Matriz das 129 categorias: 29 herdadas, 100 com proposta em 7 grupos; perguntas Q1 a Q8 |
| 4 | `gazetaonline-lookups.md` | 67 listas de opções (666 opções) com ids |
| 5 | `gazetaonline-form-labels.md` | Rótulos, textos de exemplo e de ajuda das 29 telas antigas |

## Decisões do Product Owner (2026-09-30)

| ID | Decisão tomada | Diferença em relação à recomendação |
|---|---|---|
| Q1 | **Animais vivos saem da v1** (79, 80, 82, 83, 91). "Acessórios para pets" (81) fica | Igual à recomendação |
| Q2 | **Serviços (66) e Vagas de emprego (96) ficam na v1** | Diferente: recomendação era retirar |
| Q3 | **Aluguel fica na v1**: "Aluguel de quartos" (28), "Temporada" (29) e a opção "Vender ou alugar" nos imóveis | Diferente: recomendação era só venda |
| Q4 | **Catálogo de veículos do GazetaOnline** (marca, modelo, ano e versão de carros e motos, em listas encadeadas) | Diferente: recomendação era texto livre. ⚠️ O risco jurídico do catálogo coletado da API da OLX continua registrado; recomenda-se validação jurídica antes do lançamento |
| Q5 | **Sem vídeo do YouTube na v1** | Igual |
| Q6 | **Barcos e aeronaves: "Horas de uso" no lugar de "Quilometragem"**, uma categoria só | Igual |
| Q7 | **Fotos: JPG, PNG ou WebP, até 10 MB, de 1 a 20** | Igual |
| Q8 | **Aprovados os grupos** Produtos em geral, Eletrônicos e informática, Roupas e calçados, Máquinas | Igual. Os grupos de Serviços e Vagas **não foram aprovados nem recusados** (ver "Ainda em aberto") |

**Efeito:** saem 5 categorias; a v1 fica com **124 categorias ativas**.

### Ainda em aberto (consequência das decisões)

| ID | Pergunta | Por que surgiu |
|---|---|---|
| A1 | Quais campos para **Serviços (66)** e **Vagas de emprego (96)**? A proposta está em `field-mapping.md` (`P-SERVICO`, `P-VAGA`); Vaga não tem "preço" (seria salário) | Q2 manteve as duas categorias |
| A2 | Como fica a **comissão** em serviço, vaga e aluguel? Hoje o SPEC diz: receita = comissão sobre a **venda**, paga pelo vendedor (D-02) e o objetivo descreve venda intermediada | Q2 e Q3 trouxeram negócios que não são venda |
| A3 | **Migração do catálogo de veículos**: as tabelas estão no banco do GazetaOnline (não lido nesta descoberta). Definir em `/arch` como copiar e como atualizar | Q4 |

## Recomendações originais do analista (histórico)

A coluna **Recomendação** é a opinião do analista, com o motivo. Nenhuma foi aplicada.

| ID | Decisão | Recomendação | Motivo |
|---|---|---|---|
| Q1 | Animais vivos (79 Cachorros, 80 Gatos, 82 Roedores, 83 Outros animais, 91 Animais para agropecuária) | **Fora da v1** | Venda de animal vivo tem regras legais e de bem-estar próprias e não combina com a intermediação com comissão; "Acessórios para pets" (81) continua |
| Q2 | Serviços (66) e Vagas de emprego (96) | **Fora da v1** | Não são bens vendidos por um dono; não há comissão sobre venda a cobrar |
| Q3 | Aluguel: "Aluguel de quartos" (28), "Temporada" (29) e a opção "Vender ou alugar" nos imóveis | **Fora da v1; imóveis só para venda** | O SPEC descreve venda intermediada; aluguel exige outro fluxo (contrato, período) |
| Q4 | Catálogo de marca/modelo/ano/versão de carros e motos | **Não reaproveitar o catálogo do GazetaOnline** (coletado da API da OLX). Na v1: Marca e Modelo em texto, Ano em lista (1951 até o ano seguinte, mais "1950 ou anterior"); avaliar a tabela FIPE numa versão seguinte | Risco jurídico do catálogo coletado; texto livre é suficiente para a escala pequena da v1 |
| Q5 | Vídeo do YouTube nos veículos | **Fora da v1** | Não está no SPEC; incorporar vídeo externo exige abrir a política de segurança do site (CSP) |
| Q6 | Barcos e aeronaves (37) | **Trocar "Quilometragem" por "Horas de uso"**; manter uma categoria só | Embarcação e aeronave medem uso em horas; separar não se justifica na escala da v1 |
| Q7 | Fotos | **JPG, PNG ou WebP, até 10 MB, de 1 a 20** (como a suposição S16) | WebP é o formato mais leve para o site; GIF animado não é foto de produto |
| Q8 | Os 7 grupos de proposta para as 100 categorias sem formulário | **Aprovar** `P-CONDICAO`, `P-ELETRONICO`, `P-VESTUARIO` e `P-MAQUINA`; `P-ANIMAL`, `P-SERVICO` e `P-VAGA` caem junto com Q1 e Q2 | São os padrões que o GazetaOnline já usava (Condição, Marca, Tipo) |

*(Com as recomendações, sairiam 9 categorias e ficariam 120; as decisões acima mantêm 124.)*

## Modelo de campos da v1 (conforme as decisões do Product Owner)

### Campos comuns (todas as categorias)

Título · Descrição · Preço (R$, em centavos) · Categoria · CEP (preenche Cidade e UF) · Fotos (1 a 20). Regras já decididas nos Ajustes 1 a 3 e na S26.

### Campos específicos

| Grupo | Categorias | Campos (✅ obrigatório para enviar à revisão) | Origem |
|---|---|---|---|
| Carros | 33 | Marca → Modelo → Ano → Versão ✅ (listas encadeadas do catálogo do GazetaOnline) · Quilometragem ✅ · Câmbio · Portas · Combustível · Direção · Tipo · Potência · Cor · Opcionais · Informações adicionais | GazetaOnline, sem vídeo (Q5) |
| Motos | 36 | Marca → Modelo → Ano → Versão ✅ (catálogo) · Quilometragem ✅ · Cilindrada ✅ · Cor · Opcionais · Informações adicionais | GazetaOnline, sem vídeo |
| Caminhões, Ônibus | 34, 35 | Ano do modelo ✅ · Quilometragem ✅ · Câmbio · Combustível · Direção · Tipo · Opcionais · Informações adicionais | GazetaOnline, sem vídeo |
| Barcos e aeronaves | 37 | Ano do modelo ✅ · **Horas de uso ✅** · Tipo ✅ · Combustível · Comprimento/Largura/Altura (m) · Informações adicionais | GazetaOnline, com Q6 |
| Peças | 38–42 | Condição ✅ · Tipo de peça · Cor | GazetaOnline |
| Imóveis | 26, 27, 30, 31 | Tipo ✅ · Vender ou alugar ✅ · Quartos ✅ (26, 27) · Banheiros (26, 27) · Área m² · Vagas (26, 27, 31) · Condomínio · IPTU · Características · Características do condomínio (26, 27) | GazetaOnline |
| Aluguel de quartos | 28 | Características | GazetaOnline |
| Temporada | 29 | Tipo ✅ · Quartos ✅ · Acomoda quantas pessoas ✅ · Banheiros · Vagas · Forma de pagamento · Características | GazetaOnline |
| Celulares | 43 | Marca ✅ · Modelo ✅ · Condição ✅ · Armazenamento · Cor · Saúde da bateria | GazetaOnline |
| Smartwatches | 46 | Marca ✅ · Condição ✅ | GazetaOnline |
| Produtos de telefonia | 44, 45, 47, 48 | Tipo ✅ · Condição ✅ · Marcas compatíveis (44, 45) | GazetaOnline |
| Eletro | 128–134 | Tipo ✅ · Marca ✅ · Voltagem ✅ · Condição ✅ · Capacidade (128) | GazetaOnline |
| Eletrônicos e informática | 102–127 (26) | Condição ✅ · Marca ✅ (lista por categoria) · Modelo | Proposta aprovada (Q8) |
| Roupas e calçados | 64, 65, 68, 69, 72, 75, 76, 77 | Condição ✅ · Tamanho ✅ · Gênero | Proposta aprovada (Q8) |
| Máquinas | 89, 92, 93, 97 | Condição ✅ · Marca · Ano de fabricação · Horas de uso | Proposta aprovada (Q8) |
| Produtos em geral | as demais (55) | Condição ✅ · Tipo de produto | Proposta aprovada (Q8) |
| ❓ Serviços, Vagas de emprego | 66, 96 | A definir (A1) | Em aberto |

**Listas de opções:** reaproveitar as de `gazetaonline-lookups.md` com os mesmos ids; o catálogo de veículos vem do banco do GazetaOnline (A3). Os grupos de proposta precisam de listas novas (Tamanho, Gênero, Marca por categoria de eletrônicos, Tipo por categoria), a definir em `/arch`.

**Rótulos:** partir de `gazetaonline-form-labels.md`, corrigindo "Titulo", o texto em inglês, o "CEP da localização do veículo" em telas que não são de veículo, e usando sempre "Descrição" (não "Informações adicionais") para o texto livre.

## Texto proposto para o SPEC (a aplicar com a autorização do Product Owner)

| Onde | Hoje | Passa a ser |
|---|---|---|
| S3 (árvore de categorias) | Proposta própria de 2 níveis, sem referência confirmada | **Resolvida:** árvore de `specs/categories.md` (até 3 níveis, por causa de "Autopeças"); na v1, as 124 categorias ativas (as 129 de `categories-active.md` menos os 5 de animais vivos, Q1) |
| S4 (campos por categoria) | Campos específicos só para Veículos e Terrenos | **Resolvida:** tabela "Modelo de campos da v1" deste documento |
| S16 (fotos) | Valores propostos pelo analista | Formatos e tamanho conforme Q7 |
| US-008-S09 (cenário "Campos mudam conforme a categoria") | Usa "Terrenos e lotes" e "Serviços" (sem campos) | Trocar os exemplos por categorias da árvore nova: "Terrenos, sítios e fazendas" mostra "Área (m²)"; uma categoria de "Produtos em geral" mostra só "Condição" e "Tipo de produto" |
| Apêndice (árvore de categorias) | Lista proposta pelo analista | Substituída pela referência a `specs/categories.md` |
| Out of Scope | — | Acrescentar: anúncio de animais vivos (Q1) e vídeo do YouTube (Q5) |
| D-02, Objective e Executive Summary | Receita = comissão sobre a **venda** | Revisar depois de A2 (serviço, vaga e aluguel não são venda) |
| Suposições novas | — | A1 (campos de Serviços e Vagas), A2 (comissão fora de venda), A3 (migração do catálogo) como Open |
| Revision History | v1.0 Baseline (Draft) | Sem nova linha enquanto o SPEC estiver em Draft; o conteúdo entra na v1.0 que for aprovada |

## Achados críticos fora do escopo de campos

1. **Senha do banco de produção no código do GazetaOnline** (`src/GazetaOnline.Import/Program.cs`). Trocar a senha, retirar do código e do histórico do git, usar cofre de segredos. Não reproduzida em nenhum documento.
2. **Catálogo de veículos coletado da API interna da OLX** (`valet.olx.com.br`). Não reaproveitar sem validação jurídica.
3. **Slugs:** 100 das 129 categorias ativas não têm slug, e a 33 usa `cars` (inglês). Gerar slugs em português antes do lançamento (NFR-21, SEO).
4. **IDs 24, 25 e 32 ausentes** em `specs/categories.md`: confirmar se foram excluídos de propósito.
