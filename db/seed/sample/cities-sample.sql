-- Municípios do IBGE (tarefa 3.2). GERADO por tools/CitiesImport: não edite à mão; gere de novo.
-- Origem: sample
-- AMOSTRA DE TESTE: poucos municípios, com códigos a conferir na carga real. NÃO aplique em produção.
-- Municípios: 40
-- Idempotente: pode ser aplicado mais de uma vez. Aplique com 'sqlcmd -I' ou na ferramenta de SQL do provedor, depois das migrations.
SET QUOTED_IDENTIFIER ON;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

MERGE [Cities] AS t
USING (VALUES
    (1302603, N'Manaus', 'AM', N'manaus'),
    (1501402, N'Belém', 'PA', N'belem'),
    (2304400, N'Fortaleza', 'CE', N'fortaleza'),
    (2408102, N'Natal', 'RN', N'natal'),
    (2611606, N'Recife', 'PE', N'recife'),
    (2704302, N'Maceió', 'AL', N'maceio'),
    (2927408, N'Salvador', 'BA', N'salvador'),
    (3106200, N'Belo Horizonte', 'MG', N'belo horizonte'),
    (3118601, N'Contagem', 'MG', N'contagem'),
    (3136702, N'Juiz de Fora', 'MG', N'juiz de fora'),
    (3170206, N'Uberlândia', 'MG', N'uberlandia'),
    (3205309, N'Vitória', 'ES', N'vitoria'),
    (3301009, N'Campos dos Goytacazes', 'RJ', N'campos dos goytacazes'),
    (3303302, N'Niterói', 'RJ', N'niteroi'),
    (3303906, N'Petrópolis', 'RJ', N'petropolis'),
    (3304557, N'Rio de Janeiro', 'RJ', N'rio de janeiro'),
    (3509502, N'Campinas', 'SP', N'campinas'),
    (3518800, N'Guarulhos', 'SP', N'guarulhos'),
    (3525904, N'Jundiaí', 'SP', N'jundiai'),
    (3534401, N'Osasco', 'SP', N'osasco'),
    (3543402, N'Ribeirão Preto', 'SP', N'ribeirao preto'),
    (3545803, N'Santa Bárbara d''Oeste', 'SP', N'santa barbara d''oeste'),
    (3548500, N'Santos', 'SP', N'santos'),
    (3549805, N'São José do Rio Preto', 'SP', N'sao jose do rio preto'),
    (3549904, N'São José dos Campos', 'SP', N'sao jose dos campos'),
    (3550308, N'São Paulo', 'SP', N'sao paulo'),
    (3552205, N'Sorocaba', 'SP', N'sorocaba'),
    (4106902, N'Curitiba', 'PR', N'curitiba'),
    (4113700, N'Londrina', 'PR', N'londrina'),
    (4115200, N'Maringá', 'PR', N'maringa'),
    (4202404, N'Blumenau', 'SC', N'blumenau'),
    (4205407, N'Florianópolis', 'SC', N'florianopolis'),
    (4209102, N'Joinville', 'SC', N'joinville'),
    (4305108, N'Caxias do Sul', 'RS', N'caxias do sul'),
    (4314407, N'Pelotas', 'RS', N'pelotas'),
    (4314902, N'Porto Alegre', 'RS', N'porto alegre'),
    (5002704, N'Campo Grande', 'MS', N'campo grande'),
    (5103403, N'Cuiabá', 'MT', N'cuiaba'),
    (5208707, N'Goiânia', 'GO', N'goiania'),
    (5300108, N'Brasília', 'DF', N'brasilia')
) AS s ([IbgeCode], [Name], [Uf], [NameSearch])
ON t.[IbgeCode] = s.[IbgeCode]
WHEN MATCHED AND (t.[Name] COLLATE Latin1_General_100_BIN2 <> s.[Name] COLLATE Latin1_General_100_BIN2 OR t.[Uf] <> s.[Uf] OR t.[NameSearch] COLLATE Latin1_General_100_BIN2 <> s.[NameSearch] COLLATE Latin1_General_100_BIN2) THEN UPDATE SET [Name] = s.[Name], [Uf] = s.[Uf], [NameSearch] = s.[NameSearch]
WHEN NOT MATCHED THEN INSERT ([IbgeCode], [Name], [Uf], [NameSearch]) VALUES (s.[IbgeCode], s.[Name], s.[Uf], s.[NameSearch]);

COMMIT TRANSACTION;
GO
