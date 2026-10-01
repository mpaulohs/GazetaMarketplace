# Fluxo: Equipe, do rascunho à publicação do anúncio — @US-008, @US-009, @US-010, @US-011, @US-012, @US-015

> **Em resumo:** o Redator monta o anúncio como rascunho e o envia para revisão; o Administrador confere e decide. Só depois de publicado o visitante vê o anúncio. Se o Administrador rejeitar, o Redator vê o motivo, corrige e reenvia. O Administrador pode ainda tirar do ar (despublicar) ou encerrar (arquivar) um anúncio. Nenhum anúncio é publicado sem a aprovação de um Administrador.

## Jornada — passo a passo (Mermaid)

```mermaid
flowchart TD
    Start(["Redator logado em Meus anúncios"]) --> Novo["Clica em Novo anúncio e preenche o formulário @US-008-S01"]
    Novo --> Foto["Envia fotos, escolhe a capa, remove se precisar @US-008-S02 @US-008-S03"]
    Foto -->|"foto inválida ou 21ª foto @US-008-S04 @US-008-S05"| ErroFoto["Mensagem por arquivo; anúncio não muda"]
    ErroFoto --> Foto
    Foto --> Salva["Salvar rascunho @US-008-S01"]
    Salva -->|"sem título @US-008-S08"| SemTitulo["Informe um título"]
    SemTitulo --> Novo
    Salva --> Envia["Enviar para revisão e confirmar @US-009-S01"]
    Envia -->|"faltam itens @US-009-S02 @US-009-S03"| Pend["Lista de pendências com link para cada campo"]
    Pend --> Novo
    Envia -->|"tudo certo"| EmRev["Situação: Em revisão. Anúncio somente leitura para o Redator @US-008-S12"]

    EmRev --> Fila["Administrador abre a Fila de revisão @US-010-S01"]
    Fila -->|"nenhum anúncio @US-010-S06"| FilaVazia["Nenhum anúncio aguardando revisão"]
    Fila --> Prev["Pré-visualização: ainda não publicado @US-010-S02"]
    Prev -->|"corrige antes de decidir"| Edita["Editar anúncio @US-010-S02"]
    Edita --> Prev
    Prev --> Decide{"Decisão do Administrador"}

    Decide -->|"Publicar @US-010-S03"| Tel{"Telefone do site configurado?"}
    Tel -->|"não @US-010-S08"| Config["Configure o telefone/WhatsApp antes de publicar, link para Configurações"]
    Config -->|"salva o número @US-015-S01"| Prev
    Tel -->|"sim"| Conflito{"Outro administrador decidiu antes?"}
    Conflito -->|"sim @US-010-S07"| Ja["Este anúncio já foi publicado por outro administrador"]
    Conflito -->|"não"| Pub["Situação: Publicado. Visitante vê na página inicial e na busca"]

    Decide -->|"Rejeitar com motivo @US-010-S04"| Rej["Situação: Rejeitado. Redator vê o motivo em Meus anúncios"]
    Decide -->|"Rejeitar sem motivo @US-010-S05"| SemMotivo["Informe o motivo da rejeição"]
    SemMotivo --> Decide
    Rej -->|"Redator corrige e salva @US-008-S11"| Reenvia["Enviar para revisão de novo @US-009-S04"]
    Reenvia --> EmRev

    Pub -->|"Despublicar @US-011-S01"| Rasc["Situação: Rascunho. Fora do site"]
    Rasc --> Envia
    Pub -->|"Arquivar e confirmar @US-011-S02"| Arq["Situação: Arquivado. Final, sem reativação na v1"]
    Rej -->|"Arquivar @US-011-S05"| Arq
    Rasc -->|"Arquivar"| Arq
    Pub -->|"Administrador corrige o preço @US-008-S13"| Pub
```

## Ciclo de vida do anúncio (Mermaid)

```mermaid
stateDiagram-v2
    state "Em revisão" as EmRevisao
    [*] --> Rascunho: Salvar rascunho (US-008)
    Rascunho --> EmRevisao: Enviar para revisão (US-009)
    Rejeitado --> EmRevisao: Enviar de novo após corrigir (US-009-S04)
    EmRevisao --> Publicado: Publicar (US-010-S03)
    EmRevisao --> Rejeitado: Rejeitar com motivo (US-010-S04)
    Publicado --> Rascunho: Despublicar (US-011-S01)
    Rascunho --> Arquivado: Arquivar (US-011)
    EmRevisao --> Arquivado: Arquivar (US-011)
    Rejeitado --> Arquivado: Arquivar (US-011-S05)
    Publicado --> Arquivado: Arquivar (US-011-S02)
    Arquivado --> [*]
```

## Notas

- **Quem faz o quê:** o Redator só mexe nos próprios anúncios em Rascunho ou Rejeitado (@US-008-S10, @US-012-S01); o Administrador decide, despublica, arquiva e edita qualquer anúncio não arquivado (@US-008-S13). O Administrador que cria um anúncio segue o mesmo caminho, sem atalho de publicação.
- **Efeito imediato no site:** publicar, despublicar, arquivar e a edição do Administrador em anúncio publicado valem na hora para o visitante (@US-010-S03, @US-011-S01, @US-011-S02, @US-008-S13).
- **Rejeitado só muda ao reenviar:** depois de corrigir, o anúncio continua "Rejeitado" até o Redator clicar em "Enviar para revisão" (@US-008-S11).
- **Clique duplo:** enviar para revisão duas vezes seguidas gera uma única entrada na fila (@US-009-S05).
- Arquivado não volta na versão 1; a decisão de permitir reativação é a suposição S5 da SPEC.
