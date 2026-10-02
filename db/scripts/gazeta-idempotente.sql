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
    WHERE [MigrationId] = N'20261002230057_CriarAuditEntries'
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
    WHERE [MigrationId] = N'20261002230057_CriarAuditEntries'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_ActorId_OccurredAt] ON [AuditEntries] ([ActorId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CriarAuditEntries'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_OccurredAt] ON [AuditEntries] ([OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CriarAuditEntries'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_TargetType_TargetId] ON [AuditEntries] ([TargetType], [TargetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002230057_CriarAuditEntries'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002230057_CriarAuditEntries', N'10.0.12');
END;

COMMIT;
GO

