-- =====================================================================================================================
-- Repara os nomes de categoria gravados com acento corrompido (o "o" de Imoveis aparece como "A com til" + "3 sobrescrito").
-- (Este arquivo e so ASCII de proposito: nenhum acento aparece aqui, so NCHAR(codigo), para ele nao sofrer o mesmo problema.)
--
-- O QUE ACONTECEU
--   O script de carga (db/scripts/gazeta-idempotente.sql) e um arquivo UTF-8. O sqlcmd do Windows, quando nao recebe "-f 65001",
--   le o arquivo na pagina de codigo do sistema (Windows-1252) e grava no nvarchar o texto ja errado: o "o" com acento
--   (bytes C3 B3) vira "A com til" + "3 sobrescrito". A pagina web, que esta certa, so mostra o que esta no banco.
--   Afetou as 83 categorias com acento da carga inicial (tabela Categories, coluna Name).
--
-- O QUE ESTE SCRIPT FAZ
--   Para cada categoria da carga inicial que tem acento, grava o nome certo, procurando pelo Id.
--   So mexe na linha cujo Name ainda contem "A com til" (NCHAR(195)) ou "A com circunflexo" (NCHAR(194)), que e o sinal do
--   problema, e so quando o nome e diferente do certo. Por isso:
--     * pode rodar duas vezes: na segunda nao ha nada a corrigir e nada muda;
--     * nome que o Administrador ja corrigiu ou renomeou pelo painel nao e sobrescrito.
--   A tabela Categories tem as colunas de auditoria UpdatedAt e UpdatedBy, mas elas nao distinguem uma edicao de nome (o painel
--   regrava a linha inteira mesmo quando so a ordem muda). Por isso o criterio e o conteudo do nome, nao a auditoria.
--   Cada linha corrigida recebe UpdatedAt = agora (UTC); UpdatedBy fica como esta. Nao existe coluna de busca derivada do nome
--   em Categories (a busca de anuncios usa TitleSearch, da tabela Ads), entao nada mais precisa ser recalculado.
--
-- NAO TOCA em: AuditEntries (PreviousValue/NewValue guardam o nome como estava na hora da alteracao: sao historico) nem nos logs.
--
-- COMO RODAR (depois de um BACKUP do banco)
--   sqlcmd -S (servidor) -U (usuario) -P (senha) -d (banco) -b -I -f 65001 -i db/scripts/reparar-acentos-categorias.sql
--   Termina com duas linhas: quantas categorias foram corrigidas e quantas ainda tem A com til / A com circunflexo no nome.
--   Conferir depois:  SELECT Id, Name, LEN(Name) FROM Categories WHERE Id = 1;   -- deve ter 7 caracteres
-- =====================================================================================================================
-- Os mesmos SET do script de carga: a tabela Categories tem indices filtrados, e sem QUOTED_IDENTIFIER ON o UPDATE falha com o erro 1934
-- (a ferramenta de SQL do provedor ou o SSMS podem nao ligar isso sozinhos).
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Correcao TABLE (Id int NOT NULL PRIMARY KEY, Name nvarchar(100) NOT NULL);

INSERT INTO @Correcao (Id, Name) VALUES
    (1, N'Im' + NCHAR(243) + N'veis'),
    (2, N'Autom' + NCHAR(243) + N'veis, Pe' + NCHAR(231) + N'as e Acess' + NCHAR(243) + N'rios'),
    (3, N'Autope' + NCHAR(231) + N'as'),
    (5, N'Casa, Decora' + NCHAR(231) + NCHAR(227) + N'o e Utens' + NCHAR(237) + N'lios'),
    (7, N'Servi' + NCHAR(231) + N'os'),
    (10, N'Animais de estima' + NCHAR(231) + NCHAR(227) + N'o'),
    (11, N'M' + NCHAR(250) + N'sica e hobbies'),
    (12, N'Agro e ind' + NCHAR(250) + N'stria'),
    (14, N'Com' + NCHAR(233) + N'rcio'),
    (15, N'C' + NCHAR(226) + N'meras e Drones'),
    (18, NCHAR(193) + N'udio'),
    (19, N'Inform' + NCHAR(225) + N'tica'),
    (21, N'M' + NCHAR(243) + N'veis'),
    (22, N'Materiais de Constru' + NCHAR(231) + NCHAR(227) + N'o'),
    (23, N'Escrit' + NCHAR(243) + N'rio e Home Office'),
    (30, N'Terrenos, s' + NCHAR(237) + N'tios e fazendas'),
    (31, N'Com' + NCHAR(233) + N'rcio e ind' + NCHAR(250) + N'stria'),
    (33, N'Carros, vans e utilit' + NCHAR(225) + N'rios'),
    (34, N'Caminh' + NCHAR(245) + N'es'),
    (35, NCHAR(212) + N'nibus'),
    (38, N'Pe' + NCHAR(231) + N'as para carros, vans e utilit' + NCHAR(225) + N'rios'),
    (39, N'Pe' + NCHAR(231) + N'as para caminh' + NCHAR(245) + N'es'),
    (40, N'Pe' + NCHAR(231) + N'as para motos'),
    (41, N'Pe' + NCHAR(231) + N'as para barcos e aeronaves'),
    (42, N'Pe' + NCHAR(231) + N'as para ' + NCHAR(244) + N'nibus'),
    (44, N'Acess' + NCHAR(243) + N'rios de Celular'),
    (45, N'Pe' + NCHAR(231) + N'as de Celular'),
    (47, N'Acess' + NCHAR(243) + N'rios Para Smartwatch'),
    (50, N'Decora' + NCHAR(231) + NCHAR(245) + N'es Para Casa'),
    (52, N'Utens' + NCHAR(237) + N'lios Para Cozinha'),
    (53, N'Utens' + NCHAR(237) + N'lios Para Banheiro e Limpeza'),
    (54, N'Ilumina' + NCHAR(231) + NCHAR(227) + N'o'),
    (55, N'Seguran' + NCHAR(231) + N'a Residencial'),
    (57, NCHAR(193) + N'rea Externa'),
    (59, N'Academia e Exerc' + NCHAR(237) + N'cios'),
    (63, N'Esportes Aqu' + NCHAR(225) + N'ticos'),
    (65, N'Cal' + NCHAR(231) + N'ados Esportivos'),
    (66, N'Servi' + NCHAR(231) + N'os'),
    (69, N'Cal' + NCHAR(231) + N'ados'),
    (71, N'Acess' + NCHAR(243) + N'rios'),
    (74, N'Maternidade e Cuidados com o Beb' + NCHAR(234)),
    (75, N'Cal' + NCHAR(231) + N'ados Infantis'),
    (76, N'Roupas para Beb' + NCHAR(234) + N's'),
    (77, N'Cal' + NCHAR(231) + N'ados Para Beb' + NCHAR(234) + N's'),
    (78, N'M' + NCHAR(243) + N'veis Infantis'),
    (81, N'Acess' + NCHAR(243) + N'rios para pets'),
    (88, N'Hobbies e cole' + NCHAR(231) + NCHAR(245) + N'es'),
    (89, N'Tratores e m' + NCHAR(225) + N'quinas agr' + NCHAR(237) + N'colas'),
    (90, N'Pe' + NCHAR(231) + N'as para tratores e m' + NCHAR(225) + N'quinas'),
    (92, N'M' + NCHAR(225) + N'quinas pesadas para constru' + NCHAR(231) + NCHAR(227) + N'o'),
    (93, N'M' + NCHAR(225) + N'quinas para produ' + NCHAR(231) + NCHAR(227) + N'o industrial'),
    (94, N'Outros itens para agro e ind' + NCHAR(250) + N'stria'),
    (95, N'Produ' + NCHAR(231) + NCHAR(227) + N'o Rural'),
    (98, N'Equipamentos Para Com' + NCHAR(233) + N'rcio'),
    (100, N'Equipamentos M' + NCHAR(233) + N'dicos e Hospitalares'),
    (102, N'C' + NCHAR(226) + N'meras e Filmadoras'),
    (103, N'Acess' + NCHAR(243) + N'rios para C' + NCHAR(226) + N'meras e Filmadoras'),
    (105, N'Consoles de V' + NCHAR(237) + N'deo Game'),
    (106, N'Jogos de V' + NCHAR(237) + N'deo Game'),
    (107, N'Pe' + NCHAR(231) + N'as e Acess' + NCHAR(243) + N'rios de V' + NCHAR(237) + N'deo Game'),
    (109, N'Pe' + NCHAR(231) + N'as e Acess' + NCHAR(243) + N'rios para TV'),
    (110, N'Projetores e Telas de Proje' + NCHAR(231) + NCHAR(227) + N'o'),
    (111, N'DVD, Blu-Ray e V' + NCHAR(237) + N'deo Cassete'),
    (116, N'Equipamentos e Acess' + NCHAR(243) + N'rios de Som'),
    (120, N'Perif' + NCHAR(233) + N'ricos e Acess' + NCHAR(243) + N'rios de Computador'),
    (121, N'Pe' + NCHAR(231) + N'as de Hardware'),
    (123, N'Mem' + NCHAR(243) + N'ria RAM'),
    (125, N'Placas de V' + NCHAR(237) + N'deo'),
    (131, N'Fog' + NCHAR(245) + N'es e Fornos'),
    (132, N'M' + NCHAR(225) + N'quinas de Lavar e Secadoras'),
    (133, N'Eletroport' + NCHAR(225) + N'teis Para Cozinha e Limpeza'),
    (134, N'Eletroport' + NCHAR(225) + N'teis Para Cuidados Pessoais'),
    (135, N'Camas e Colch' + NCHAR(245) + N'es'),
    (136, N'Sof' + NCHAR(225) + N's e Poltronas'),
    (140, N'Racks e Pain' + NCHAR(233) + N'is'),
    (141, N'Arm' + NCHAR(225) + N'rios e Guarda-Roupas'),
    (142, N'M' + NCHAR(243) + N'veis Para Organiza' + NCHAR(231) + NCHAR(227) + N'o'),
    (143, N'Funda' + NCHAR(231) + NCHAR(227) + N'o e Estrutura'),
    (149, N'Instala' + NCHAR(231) + NCHAR(245) + N'es El' + NCHAR(233) + N'tricas e Hidr' + NCHAR(225) + N'ulicas'),
    (150, N'Ferramentas de Constru' + NCHAR(231) + NCHAR(227) + N'o'),
    (152, N'Itens Para Escrit' + NCHAR(243) + N'rio'),
    (153, N'Cadeiras de Escrit' + NCHAR(243) + N'rio e Gamer'),
    (154, N'M' + NCHAR(243) + N'veis de Escrit' + NCHAR(243) + N'rio')
;

UPDATE c
   SET c.Name = f.Name,
       c.UpdatedAt = SYSUTCDATETIME()
  FROM dbo.Categories AS c
  JOIN @Correcao AS f ON f.Id = c.Id
 WHERE (c.Name COLLATE Latin1_General_BIN2 LIKE N'%' + NCHAR(195) + N'%'
        OR c.Name COLLATE Latin1_General_BIN2 LIKE N'%' + NCHAR(194) + N'%')
   AND c.Name COLLATE Latin1_General_BIN2 <> f.Name COLLATE Latin1_General_BIN2;

DECLARE @Corrigidas int = @@ROWCOUNT;

DECLARE @Restantes int =
    (SELECT COUNT(*)
       FROM dbo.Categories
      WHERE Name COLLATE Latin1_General_BIN2 LIKE N'%' + NCHAR(195) + N'%'
         OR Name COLLATE Latin1_General_BIN2 LIKE N'%' + NCHAR(194) + N'%');

COMMIT TRANSACTION;

PRINT 'Categorias corrigidas: ' + CAST(@Corrigidas AS varchar(10));
PRINT 'Categorias que ainda tem A com til ou A com circunflexo no nome: ' + CAST(@Restantes AS varchar(10));
