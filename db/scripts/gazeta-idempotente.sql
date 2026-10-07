-- Script idempotente das migrations do GazetaMarketplace (gerado por db/scripts/gerar-script.sh; não edite à mão).
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CreateAuditEntries'
)
BEGIN
    CREATE TABLE [AuditEntries] (
        [Id] int NOT NULL IDENTITY,
        [OccurredAt] datetime2 NOT NULL,
        [ActorId] int NULL,
        [Action] varchar(60) NOT NULL,
        [TargetType] varchar(60) NOT NULL,
        [TargetId] varchar(64) NOT NULL,
        [PreviousValue] nvarchar(2000) NULL,
        [NewValue] nvarchar(2000) NULL,
        [Result] tinyint NOT NULL,
        [CorrelationId] varchar(64) NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CreateAuditEntries'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_ActorId_OccurredAt] ON [AuditEntries] ([ActorId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CreateAuditEntries'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_OccurredAt] ON [AuditEntries] ([OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CreateAuditEntries'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TargetType_TargetId] ON [AuditEntries] ([TargetType], [TargetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CreateAuditEntries'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002230057_CreateAuditEntries', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] ON;
    EXEC(N'INSERT INTO [AspNetRoles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
    VALUES (1, N''5c0b1d2a-8f7e-4a39-9d41-0a1e6c3b7f01'', N''Administrador'', N''ADMINISTRADOR''),
    (2, N''9e4a6f3c-2b15-4d87-a6c0-7d2f8e1b5a02'', N''Redator'', N''REDATOR'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003030830_AddIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003030830_AddIdentity', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003081958_AddPasswordRecoveryAttempts'
)
BEGIN
    CREATE TABLE [PasswordRecoveryAttempts] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(254) NOT NULL,
        [Ip] varchar(45) NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PasswordRecoveryAttempts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003081958_AddPasswordRecoveryAttempts'
)
BEGIN
    CREATE INDEX [IX_PasswordRecoveryAttempts_Email_RequestedAt] ON [PasswordRecoveryAttempts] ([Email], [RequestedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003081958_AddPasswordRecoveryAttempts'
)
BEGIN
    CREATE INDEX [IX_PasswordRecoveryAttempts_Ip_RequestedAt] ON [PasswordRecoveryAttempts] ([Ip], [RequestedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003081958_AddPasswordRecoveryAttempts'
)
BEGIN
    CREATE INDEX [IX_PasswordRecoveryAttempts_RequestedAt] ON [PasswordRecoveryAttempts] ([RequestedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003081958_AddPasswordRecoveryAttempts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003081958_AddPasswordRecoveryAttempts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [ParentId] int NULL,
        [Name] nvarchar(100) NOT NULL,
        [Slug] varchar(120) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsPostable] bit NOT NULL,
        [FieldGroup] varchar(40) NULL,
        [IsSystem] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Categories_NotOwnParent] CHECK ([ParentId] IS NULL OR [ParentId] <> [Id]),
        CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'DisplayOrder', N'FieldGroup', N'IsPostable', N'IsSystem', N'Name', N'ParentId', N'Slug', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [CreatedAt], [CreatedBy], [DisplayOrder], [FieldGroup], [IsPostable], [IsSystem], [Name], [ParentId], [Slug], [UpdatedAt], [UpdatedBy])
    VALUES (1, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Imóveis'', NULL, ''imoveis'', NULL, NULL),
    (2, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Automóveis, Peças e Acessórios'', NULL, ''automoveis-pecas-e-acessorios'', NULL, NULL),
    (4, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Celulares e Telefonia'', NULL, ''celulares-e-telefonia'', NULL, NULL),
    (5, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Casa, Decoração e Utensílios'', NULL, ''casa-decoracao-e-utensilios'', NULL, NULL),
    (6, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Esportes e Fitness'', NULL, ''esportes-e-fitness'', NULL, NULL),
    (7, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Serviços'', NULL, ''servicos-grupo'', NULL, NULL),
    (8, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Moda e beleza'', NULL, ''moda-e-beleza'', NULL, NULL),
    (9, ''2026-10-03T00:00:00.0000000Z'', NULL, 8, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Artigos infantis'', NULL, ''artigos-infantis'', NULL, NULL),
    (10, ''2026-10-03T00:00:00.0000000Z'', NULL, 9, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Animais de estimação'', NULL, ''animais-de-estimacao'', NULL, NULL),
    (11, ''2026-10-03T00:00:00.0000000Z'', NULL, 10, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Música e hobbies'', NULL, ''musica-e-hobbies'', NULL, NULL),
    (12, ''2026-10-03T00:00:00.0000000Z'', NULL, 11, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Agro e indústria'', NULL, ''agro-e-industria'', NULL, NULL),
    (13, ''2026-10-03T00:00:00.0000000Z'', NULL, 12, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Vagas de emprego'', NULL, ''vagas-de-emprego-grupo'', NULL, NULL),
    (14, ''2026-10-03T00:00:00.0000000Z'', NULL, 13, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Comércio'', NULL, ''comercio'', NULL, NULL),
    (15, ''2026-10-03T00:00:00.0000000Z'', NULL, 14, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Câmeras e Drones'', NULL, ''cameras-e-drones'', NULL, NULL),
    (16, ''2026-10-03T00:00:00.0000000Z'', NULL, 15, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Games'', NULL, ''games'', NULL, NULL),
    (17, ''2026-10-03T00:00:00.0000000Z'', NULL, 16, NULL, CAST(0 AS bit), CAST(1 AS bit), N''TVs e video'', NULL, ''tvs-e-video'', NULL, NULL),
    (18, ''2026-10-03T00:00:00.0000000Z'', NULL, 17, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Áudio'', NULL, ''audio'', NULL, NULL),
    (19, ''2026-10-03T00:00:00.0000000Z'', NULL, 18, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Informática'', NULL, ''informatica'', NULL, NULL),
    (20, ''2026-10-03T00:00:00.0000000Z'', NULL, 19, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Eletro'', NULL, ''eletro'', NULL, NULL),
    (21, ''2026-10-03T00:00:00.0000000Z'', NULL, 20, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Móveis'', NULL, ''moveis'', NULL, NULL),
    (22, ''2026-10-03T00:00:00.0000000Z'', NULL, 21, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Materiais de Construção'', NULL, ''materiais-de-construcao'', NULL, NULL),
    (23, ''2026-10-03T00:00:00.0000000Z'', NULL, 22, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Escritório e Home Office'', NULL, ''escritorio-e-home-office'', NULL, NULL),
    (3, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(0 AS bit), CAST(1 AS bit), N''Autopeças'', 2, ''autopecas'', NULL, NULL),
    (26, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Apartamentos'', 1, ''apartamentos'', NULL, NULL),
    (27, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Casas'', 1, ''casas'', NULL, NULL),
    (28, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Aluguel de quartos'', 1, ''aluguel-de-quartos'', NULL, NULL),
    (29, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Temporada'', 1, ''temporada'', NULL, NULL),
    (30, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Terrenos, sítios e fazendas'', 1, ''terrenos-sitios-e-fazendas'', NULL, NULL),
    (31, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Comércio e indústria'', 1, ''comercio-e-industria'', NULL, NULL),
    (33, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Carros, vans e utilitários'', 2, ''cars'', NULL, NULL),
    (34, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Caminhões'', 2, ''caminhoes'', NULL, NULL),
    (35, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Ônibus'', 2, ''onibus'', NULL, NULL),
    (36, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Motos'', 2, ''motos'', NULL, NULL),
    (37, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Barcos e aeronaves'', 2, ''barcos-e-aeronaves'', NULL, NULL),
    (43, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Celulares e Smartphones'', 4, ''celulares-e-smartphones'', NULL, NULL),
    (44, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Acessórios de Celular'', 4, ''acessorios-de-celular'', NULL, NULL),
    (45, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças de Celular'', 4, ''pecas-de-celular'', NULL, NULL),
    (46, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Smartwatches'', 4, ''smartwatches'', NULL, NULL),
    (47, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Acessórios Para Smartwatch'', 4, ''acessorios-para-smartwatch'', NULL, NULL),
    (48, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Telefonia Fixa e Sem Fio'', 4, ''telefonia-fixa-e-sem-fio'', NULL, NULL),
    (49, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Tecidos de Cama, Mesa e Banho'', 5, ''tecidos-de-cama-mesa-e-banho'', NULL, NULL),
    (50, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Decorações Para Casa'', 5, ''decoracoes-para-casa'', NULL, NULL);
    INSERT INTO [Categories] ([Id], [CreatedAt], [CreatedBy], [DisplayOrder], [FieldGroup], [IsPostable], [IsSystem], [Name], [ParentId], [Slug], [UpdatedAt], [UpdatedBy])
    VALUES (51, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Casa Inteligente'', 5, ''casa-inteligente'', NULL, NULL),
    (52, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Utensílios Para Cozinha'', 5, ''utensilios-para-cozinha'', NULL, NULL),
    (53, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Utensílios Para Banheiro e Limpeza'', 5, ''utensilios-para-banheiro-e-limpeza'', NULL, NULL),
    (54, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Iluminação'', 5, ''iluminacao'', NULL, NULL),
    (55, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Segurança Residencial'', 5, ''seguranca-residencial'', NULL, NULL),
    (56, ''2026-10-03T00:00:00.0000000Z'', NULL, 8, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Jardinagem e Plantas'', 5, ''jardinagem-e-plantas'', NULL, NULL),
    (57, ''2026-10-03T00:00:00.0000000Z'', NULL, 9, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Área Externa'', 5, ''area-externa'', NULL, NULL),
    (58, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Ciclismo'', 6, ''ciclismo'', NULL, NULL),
    (59, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Academia e Exercícios'', 6, ''academia-e-exercicios'', NULL, NULL),
    (60, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Acampamento'', 6, ''acampamento'', NULL, NULL),
    (61, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Esportes Sobre Rodas'', 6, ''esportes-sobre-rodas'', NULL, NULL),
    (62, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Esportes de Quadra e Ao Ar Livre'', 6, ''esportes-de-quadra-e-ao-ar-livre'', NULL, NULL),
    (63, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Esportes Aquáticos'', 6, ''esportes-aquaticos'', NULL, NULL),
    (64, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Roupas Esportivas'', 6, ''roupas-esportivas'', NULL, NULL),
    (65, ''2026-10-03T00:00:00.0000000Z'', NULL, 8, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Calçados Esportivos'', 6, ''calcados-esportivos'', NULL, NULL),
    (66, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Serviços'', 7, ''servicos'', NULL, NULL),
    (67, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Beleza e Cuidados Pessoais'', 8, ''beleza-e-cuidados-pessoais'', NULL, NULL),
    (68, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Roupas'', 8, ''roupas'', NULL, NULL),
    (69, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Calçados'', 8, ''calcados'', NULL, NULL),
    (70, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Bolsas, malas e mochilas'', 8, ''bolsas-malas-e-mochilas'', NULL, NULL),
    (71, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Acessórios'', 8, ''acessorios'', NULL, NULL),
    (72, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Roupas Infantis'', 9, ''roupas-infantis'', NULL, NULL),
    (73, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Brinquedos e Jogos'', 9, ''brinquedos-e-jogos'', NULL, NULL),
    (74, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Maternidade e Cuidados com o Bebê'', 9, ''maternidade-e-cuidados-com-o-bebe'', NULL, NULL),
    (75, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Calçados Infantis'', 9, ''calcados-infantis'', NULL, NULL),
    (76, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Roupas para Bebês'', 9, ''roupas-para-bebes'', NULL, NULL),
    (77, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Calçados Para Bebês'', 9, ''calcados-para-bebes'', NULL, NULL),
    (78, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Móveis Infantis'', 9, ''moveis-infantis'', NULL, NULL),
    (81, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Acessórios para pets'', 10, ''acessorios-para-pets'', NULL, NULL),
    (84, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Instrumentos musicais'', 11, ''instrumentos-musicais'', NULL, NULL),
    (85, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''CDs, DVDs etc'', 11, ''cds-dvds-etc'', NULL, NULL),
    (86, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Livros e revistas'', 11, ''livros-e-revistas'', NULL, NULL),
    (87, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Antiguidades'', 11, ''antiguidades'', NULL, NULL),
    (88, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Hobbies e coleções'', 11, ''hobbies-e-colecoes'', NULL, NULL),
    (89, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Tratores e máquinas agrícolas'', 12, ''tratores-e-maquinas-agricolas'', NULL, NULL),
    (90, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças para tratores e máquinas'', 12, ''pecas-para-tratores-e-maquinas'', NULL, NULL),
    (92, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Máquinas pesadas para construção'', 12, ''maquinas-pesadas-para-construcao'', NULL, NULL),
    (93, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Máquinas para produção industrial'', 12, ''maquinas-para-producao-industrial'', NULL, NULL),
    (94, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Outros itens para agro e indústria'', 12, ''outros-itens-para-agro-e-industria'', NULL, NULL),
    (95, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Produção Rural'', 12, ''producao-rural'', NULL, NULL),
    (96, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Vagas de emprego'', 13, ''vagas-de-emprego'', NULL, NULL),
    (97, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Trailers e carrinhos comerciais'', 14, ''trailers-e-carrinhos-comerciais'', NULL, NULL);
    INSERT INTO [Categories] ([Id], [CreatedAt], [CreatedBy], [DisplayOrder], [FieldGroup], [IsPostable], [IsSystem], [Name], [ParentId], [Slug], [UpdatedAt], [UpdatedBy])
    VALUES (98, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Equipamentos Para Comércio'', 14, ''equipamentos-para-comercio'', NULL, NULL),
    (99, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Gastronomia e Hotelaria'', 14, ''gastronomia-e-hotelaria'', NULL, NULL),
    (100, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Equipamentos Médicos e Hospitalares'', 14, ''equipamentos-medicos-e-hospitalares'', NULL, NULL),
    (101, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Uniformes de Trabalho e EPIs'', 14, ''uniformes-de-trabalho-e-epis'', NULL, NULL),
    (102, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Câmeras e Filmadoras'', 15, ''cameras-e-filmadoras'', NULL, NULL),
    (103, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Acessórios para Câmeras e Filmadoras'', 15, ''acessorios-para-cameras-e-filmadoras'', NULL, NULL),
    (104, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Drones'', 15, ''drones'', NULL, NULL),
    (105, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Consoles de Vídeo Game'', 16, ''consoles-de-video-game'', NULL, NULL),
    (106, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Jogos de Vídeo Game'', 16, ''jogos-de-video-game'', NULL, NULL),
    (107, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças e Acessórios de Vídeo Game'', 16, ''pecas-e-acessorios-de-video-game'', NULL, NULL),
    (108, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''TVs'', 17, ''tvs'', NULL, NULL),
    (109, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças e Acessórios para TV'', 17, ''pecas-e-acessorios-para-tv'', NULL, NULL),
    (110, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Projetores e Telas de Projeção'', 17, ''projetores-e-telas-de-projecao'', NULL, NULL),
    (111, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''DVD, Blu-Ray e Vídeo Cassete'', 17, ''dvd-blu-ray-e-video-cassete'', NULL, NULL),
    (112, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Dispositivos de Streaming'', 17, ''dispositivos-de-streaming'', NULL, NULL),
    (113, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Fones de Ouvido'', 18, ''fones-de-ouvido'', NULL, NULL),
    (114, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Aparelhos de Som'', 18, ''aparelhos-de-som'', NULL, NULL),
    (115, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Microfones e Gravadores'', 18, ''microfones-e-gravadores'', NULL, NULL),
    (116, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Equipamentos e Acessórios de Som'', 18, ''equipamentos-e-acessorios-de-som'', NULL, NULL),
    (117, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Computadores e Desktops'', 19, ''computadores-e-desktops'', NULL, NULL),
    (118, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Notebooks'', 19, ''notebooks'', NULL, NULL),
    (119, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Monitores'', 19, ''monitores'', NULL, NULL),
    (120, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Periféricos e Acessórios de Computador'', 19, ''perifericos-e-acessorios-de-computador'', NULL, NULL),
    (121, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças de Hardware'', 19, ''pecas-de-hardware'', NULL, NULL),
    (122, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Armazenamento'', 19, ''armazenamento'', NULL, NULL),
    (123, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Memória RAM'', 19, ''memoria-ram'', NULL, NULL),
    (124, ''2026-10-03T00:00:00.0000000Z'', NULL, 8, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Processadores'', 19, ''processadores'', NULL, NULL),
    (125, ''2026-10-03T00:00:00.0000000Z'', NULL, 9, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Placas de Vídeo'', 19, ''placas-de-video'', NULL, NULL),
    (126, ''2026-10-03T00:00:00.0000000Z'', NULL, 10, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Conectividade e Dispositivos de Rede'', 19, ''conectividade-e-dispositivos-de-rede'', NULL, NULL),
    (127, ''2026-10-03T00:00:00.0000000Z'', NULL, 11, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Tablets e E-Readers'', 19, ''tablets-e-e-readers'', NULL, NULL),
    (128, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Ar-condicionados'', 20, ''ar-condicionados'', NULL, NULL),
    (129, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Ventiladores e Climatizadores'', 20, ''ventiladores-e-climatizadores'', NULL, NULL),
    (130, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Geladeiras e Freezers'', 20, ''geladeiras-e-freezers'', NULL, NULL),
    (131, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Fogões e Fornos'', 20, ''fogoes-e-fornos'', NULL, NULL),
    (132, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Máquinas de Lavar e Secadoras'', 20, ''maquinas-de-lavar-e-secadoras'', NULL, NULL),
    (133, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Eletroportáteis Para Cozinha e Limpeza'', 20, ''eletroportateis-para-cozinha-e-limpeza'', NULL, NULL),
    (134, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Eletroportáteis Para Cuidados Pessoais'', 20, ''eletroportateis-para-cuidados-pessoais'', NULL, NULL),
    (135, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Camas e Colchões'', 21, ''camas-e-colchoes'', NULL, NULL),
    (136, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Sofás e Poltronas'', 21, ''sofas-e-poltronas'', NULL, NULL),
    (137, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Bancos e Cadeiras'', 21, ''bancos-e-cadeiras'', NULL, NULL),
    (138, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Mesas'', 21, ''mesas'', NULL, NULL),
    (139, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Escrivaninhas e Penteadeiras'', 21, ''escrivaninhas-e-penteadeiras'', NULL, NULL);
    INSERT INTO [Categories] ([Id], [CreatedAt], [CreatedBy], [DisplayOrder], [FieldGroup], [IsPostable], [IsSystem], [Name], [ParentId], [Slug], [UpdatedAt], [UpdatedBy])
    VALUES (140, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Racks e Painéis'', 21, ''racks-e-paineis'', NULL, NULL),
    (141, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Armários e Guarda-Roupas'', 21, ''armarios-e-guarda-roupas'', NULL, NULL),
    (142, ''2026-10-03T00:00:00.0000000Z'', NULL, 8, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Móveis Para Organização'', 21, ''moveis-para-organizacao'', NULL, NULL),
    (143, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Fundação e Estrutura'', 22, ''fundacao-e-estrutura'', NULL, NULL),
    (144, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Alvenaria'', 22, ''alvenaria'', NULL, NULL),
    (145, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Pisos e Revestimentos'', 22, ''pisos-e-revestimentos'', NULL, NULL),
    (146, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Portas e Janelas'', 22, ''portas-e-janelas'', NULL, NULL),
    (147, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Cubas e Pias'', 22, ''cubas-e-pias'', NULL, NULL),
    (148, ''2026-10-03T00:00:00.0000000Z'', NULL, 6, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Torneiras, Duchas e Vasos'', 22, ''torneiras-duchas-e-vasos'', NULL, NULL),
    (149, ''2026-10-03T00:00:00.0000000Z'', NULL, 7, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Instalações Elétricas e Hidráulicas'', 22, ''instalacoes-eletricas-e-hidraulicas'', NULL, NULL),
    (150, ''2026-10-03T00:00:00.0000000Z'', NULL, 8, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Ferramentas de Construção'', 22, ''ferramentas-de-construcao'', NULL, NULL),
    (151, ''2026-10-03T00:00:00.0000000Z'', NULL, 9, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Ferramentas de Pintura'', 22, ''ferramentas-de-pintura'', NULL, NULL),
    (152, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Itens Para Escritório'', 23, ''itens-para-escritorio'', NULL, NULL),
    (153, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Cadeiras de Escritório e Gamer'', 23, ''cadeiras-de-escritorio-e-gamer'', NULL, NULL),
    (154, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Móveis de Escritório'', 23, ''moveis-de-escritorio'', NULL, NULL),
    (155, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Papelaria'', 23, ''papelaria'', NULL, NULL),
    (38, ''2026-10-03T00:00:00.0000000Z'', NULL, 1, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças para carros, vans e utilitários'', 3, ''pecas-carros-vans-e-utilitarios'', NULL, NULL),
    (39, ''2026-10-03T00:00:00.0000000Z'', NULL, 4, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças para caminhões'', 3, ''pecas-caminhoes'', NULL, NULL),
    (40, ''2026-10-03T00:00:00.0000000Z'', NULL, 2, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças para motos'', 3, ''pecas-motos'', NULL, NULL),
    (41, ''2026-10-03T00:00:00.0000000Z'', NULL, 5, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças para barcos e aeronaves'', 3, ''pecas-barcos-e-aeronaves'', NULL, NULL),
    (42, ''2026-10-03T00:00:00.0000000Z'', NULL, 3, NULL, CAST(1 AS bit), CAST(1 AS bit), N''Peças para ônibus'', 3, ''pecas-onibus'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'DisplayOrder', N'FieldGroup', N'IsPostable', N'IsSystem', N'Name', N'ParentId', N'Slug', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentId_DisplayOrder] ON [Categories] ([ParentId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Categories_Name_Root] ON [Categories] ([Name]) WHERE [ParentId] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Categories_ParentId_Name] ON [Categories] ([ParentId], [Name]) WHERE [ParentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Categories_Slug] ON [Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003164338_AddCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003164338_AddCategories', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''RealEstate''
    WHERE [Id] = 26;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''RealEstate''
    WHERE [Id] = 27;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''RealEstate''
    WHERE [Id] = 30;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''RealEstate''
    WHERE [Id] = 31;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Services''
    WHERE [Id] = 66;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Jobs''
    WHERE [Id] = 96;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003170615_AssignFieldGroupsToRealEstateServicesAndJobs', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Parts''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Cars''
    WHERE [Id] = 33;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''TrucksAndBuses''
    WHERE [Id] = 34;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''TrucksAndBuses''
    WHERE [Id] = 35;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Motorcycles''
    WHERE [Id] = 36;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''BoatsAndAircraft''
    WHERE [Id] = 37;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003172530_AssignFieldGroupsToVehiclesAndParts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003172530_AssignFieldGroupsToVehiclesAndParts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003174013_AddSiteSettings'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [Id] int NOT NULL IDENTITY,
        [Key] varchar(100) NOT NULL,
        [Value] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003174013_AddSiteSettings'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_SiteSettings_Key] ON [SiteSettings] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003174013_AddSiteSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003174013_AddSiteSettings', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE TABLE [VehicleBrands] (
        [Id] int NOT NULL,
        [Kind] varchar(4) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Source] varchar(60) NOT NULL,
        CONSTRAINT [PK_VehicleBrands] PRIMARY KEY ([Id], [Kind]),
        CONSTRAINT [CK_VehicleBrands_Kind] CHECK ([Kind] IN ('car', 'moto'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE TABLE [VehicleModels] (
        [Id] int NOT NULL,
        [Kind] varchar(4) NOT NULL,
        [BrandId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Source] varchar(60) NOT NULL,
        CONSTRAINT [PK_VehicleModels] PRIMARY KEY ([Id], [Kind]),
        CONSTRAINT [CK_VehicleModels_Kind] CHECK ([Kind] IN ('car', 'moto')),
        CONSTRAINT [FK_VehicleModels_VehicleBrands] FOREIGN KEY ([BrandId], [Kind]) REFERENCES [VehicleBrands] ([Id], [Kind]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE TABLE [VehicleModelYears] (
        [ModelId] int NOT NULL,
        [Year] int NOT NULL,
        [Kind] varchar(4) NOT NULL,
        [Source] varchar(60) NOT NULL,
        CONSTRAINT [PK_VehicleModelYears] PRIMARY KEY ([ModelId], [Year], [Kind]),
        CONSTRAINT [CK_VehicleModelYears_Kind] CHECK ([Kind] IN ('car', 'moto')),
        CONSTRAINT [FK_VehicleModelYears_VehicleModels] FOREIGN KEY ([ModelId], [Kind]) REFERENCES [VehicleModels] ([Id], [Kind]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE TABLE [VehicleVersions] (
        [Id] int NOT NULL,
        [Kind] varchar(4) NOT NULL,
        [ModelId] int NOT NULL,
        [Year] int NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Source] varchar(60) NOT NULL,
        CONSTRAINT [PK_VehicleVersions] PRIMARY KEY ([Id], [Kind]),
        CONSTRAINT [CK_VehicleVersions_Kind] CHECK ([Kind] IN ('car', 'moto')),
        CONSTRAINT [FK_VehicleVersions_VehicleModelYears] FOREIGN KEY ([ModelId], [Year], [Kind]) REFERENCES [VehicleModelYears] ([ModelId], [Year], [Kind]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE INDEX [IX_VehicleModels_BrandId_Kind] ON [VehicleModels] ([BrandId], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE INDEX [IX_VehicleModelYears_ModelId_Kind] ON [VehicleModelYears] ([ModelId], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    CREATE INDEX [IX_VehicleVersions_ModelId_Year_Kind] ON [VehicleVersions] ([ModelId], [Year], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003181509_AddVehicleCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003181509_AddVehicleCatalog', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''RoomRental''
    WHERE [Id] = 28;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Seasonal''
    WHERE [Id] = 29;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Phones''
    WHERE [Id] = 43;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''TelephonyProducts''
    WHERE [Id] = 44;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''TelephonyProducts''
    WHERE [Id] = 45;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Smartwatches''
    WHERE [Id] = 46;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''TelephonyProducts''
    WHERE [Id] = 47;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''TelephonyProducts''
    WHERE [Id] = 48;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 64;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 65;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 68;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 69;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 72;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 75;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 76;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ClothingAndShoes''
    WHERE [Id] = 77;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Machinery''
    WHERE [Id] = 89;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Machinery''
    WHERE [Id] = 92;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Machinery''
    WHERE [Id] = 93;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Machinery''
    WHERE [Id] = 97;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 102;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 103;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 104;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 105;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 106;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 107;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 108;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 109;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 110;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 111;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 112;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 113;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 114;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 115;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 116;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 117;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 118;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 119;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 120;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 121;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 122;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 123;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 124;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 125;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 126;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''ElectronicsAndComputers''
    WHERE [Id] = 127;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 128;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 129;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 130;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 131;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 132;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 133;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    EXEC(N'UPDATE [Categories] SET [FieldGroup] = ''Appliances''
    WHERE [Id] = 134;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003230221_AssignFieldGroupsToRemainingCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003230221_AssignFieldGroupsToRemainingCategories', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE TABLE [Ads] (
        [Id] int NOT NULL IDENTITY,
        [CategoryId] int NULL,
        [Status] tinyint NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [Description] nvarchar(max) NULL,
        [PriceCents] bigint NULL,
        [Cep] char(8) NULL,
        [City] nvarchar(80) NULL,
        [Uf] char(2) NULL,
        [LocationManual] bit NOT NULL,
        [Attributes] nvarchar(max) NOT NULL,
        [VehicleBrandId] AS TRY_CAST(JSON_VALUE([Attributes], '$.brandId') AS int) PERSISTED,
        [VehicleModelId] AS TRY_CAST(JSON_VALUE([Attributes], '$.modelId') AS int) PERSISTED,
        [ModelYear] AS TRY_CAST(JSON_VALUE([Attributes], '$.modelYear') AS int) PERSISTED,
        [Km] AS TRY_CAST(JSON_VALUE([Attributes], '$.km') AS int) PERSISTED,
        [AreaM2] AS TRY_CAST(JSON_VALUE([Attributes], '$.areaM2') AS decimal(12,2)) PERSISTED,
        [TitleSearch] nvarchar(200) NULL,
        [DescriptionSearch] nvarchar(max) NULL,
        [AuthorId] int NOT NULL,
        [SentAt] datetime2 NULL,
        [PublishedAt] datetime2 NULL,
        [PublishedById] int NULL,
        [RejectedAt] datetime2 NULL,
        [RejectedById] int NULL,
        [RejectionReason] nvarchar(500) NULL,
        [ArchivedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Ads] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Ads_Attributes] CHECK (ISJSON([Attributes]) = 1 AND LEFT(LTRIM([Attributes]), 1) = '{'),
        CONSTRAINT [CK_Ads_Cep] CHECK ([Cep] IS NULL OR ([Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8)),
        CONSTRAINT [CK_Ads_Description] CHECK ([Description] IS NULL OR LEN([Description]) <= 6000),
        CONSTRAINT [CK_Ads_PriceCents] CHECK ([PriceCents] IS NULL OR ([PriceCents] > 0 AND [PriceCents] <= 9999999999)),
        CONSTRAINT [CK_Ads_Status] CHECK ([Status] BETWEEN 1 AND 5),
        CONSTRAINT [CK_Ads_Title] CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),
        CONSTRAINT [CK_Ads_Uf] CHECK ([Uf] IS NULL OR ([Uf] NOT LIKE '%[^A-Za-z]%' AND LEN([Uf]) = 2)),
        CONSTRAINT [FK_Ads_AspNetUsers_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Ads_AspNetUsers_PublishedById] FOREIGN KEY ([PublishedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Ads_AspNetUsers_RejectedById] FOREIGN KEY ([RejectedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Ads_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE TABLE [AdPhotos] (
        [Id] int NOT NULL IDENTITY,
        [AdId] int NOT NULL,
        [SortOrder] int NOT NULL,
        [StorageKey] varchar(100) NOT NULL,
        [Width] int NOT NULL,
        [Height] int NOT NULL,
        [SizeBytes] int NOT NULL,
        [OriginalKey] varchar(260) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AdPhotos] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AdPhotos_Size] CHECK ([Width] > 0 AND [Height] > 0 AND [SizeBytes] > 0),
        CONSTRAINT [CK_AdPhotos_SortOrder] CHECK ([SortOrder] >= 0),
        CONSTRAINT [FK_AdPhotos_Ads_AdId] FOREIGN KEY ([AdId]) REFERENCES [Ads] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_AdPhotos_AdId_SortOrder] ON [AdPhotos] ([AdId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AdPhotos_StorageKey] ON [AdPhotos] ([StorageKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_AreaM2] ON [Ads] ([AreaM2]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_AuthorId_Status_UpdatedAt] ON [Ads] ([AuthorId], [Status], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_CategoryId] ON [Ads] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_Km] ON [Ads] ([Km]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_PublishedById] ON [Ads] ([PublishedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_RejectedById] ON [Ads] ([RejectedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_Status_CategoryId_PublishedAt] ON [Ads] ([Status], [CategoryId], [PublishedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Ads_Status_PriceCents] ON [Ads] ([Status], [PriceCents]) WHERE [PriceCents] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_Status_Uf_City] ON [Ads] ([Status], [Uf], [City]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    CREATE INDEX [IX_Ads_VehicleBrandId_ModelYear] ON [Ads] ([VehicleBrandId], [ModelYear]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003233141_AddAds'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003233141_AddAds', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003235933_AddCepCacheAndCities'
)
BEGIN
    CREATE TABLE [CepCache] (
        [Cep] char(8) NOT NULL,
        [City] nvarchar(80) NOT NULL,
        [Uf] char(2) NOT NULL,
        [IbgeCode] int NULL,
        [FetchedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CepCache] PRIMARY KEY ([Cep]),
        CONSTRAINT [CK_CepCache_Cep] CHECK ([Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003235933_AddCepCacheAndCities'
)
BEGIN
    CREATE TABLE [Cities] (
        [IbgeCode] int NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [Uf] char(2) NOT NULL,
        [NameSearch] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_Cities] PRIMARY KEY ([IbgeCode])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003235933_AddCepCacheAndCities'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Cities_Uf_NameSearch] ON [Cities] ([Uf], [NameSearch]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003235933_AddCepCacheAndCities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003235933_AddCepCacheAndCities', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005153909_AddArchivedBy'
)
BEGIN
    ALTER TABLE [Ads] ADD [ArchivedById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005153909_AddArchivedBy'
)
BEGIN
    CREATE INDEX [IX_Ads_ArchivedById] ON [Ads] ([ArchivedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005153909_AddArchivedBy'
)
BEGIN
    ALTER TABLE [Ads] ADD CONSTRAINT [FK_Ads_AspNetUsers_ArchivedById] FOREIGN KEY ([ArchivedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005153909_AddArchivedBy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005153909_AddArchivedBy', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005161939_AddPublishedAtIndex'
)
BEGIN
    CREATE INDEX [IX_Ads_Status_PublishedAt_Id] ON [Ads] ([Status], [PublishedAt] DESC, [Id] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005161939_AddPublishedAtIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005161939_AddPublishedAtIndex', N'10.0.12');
END;

COMMIT;
GO

