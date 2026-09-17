BEGIN TRANSACTION;
IF SCHEMA_ID(N'audit') IS NULL EXEC(N'CREATE SCHEMA [audit];');

CREATE TABLE [audit].[Operation] (
    [OperationId] uniqueidentifier NOT NULL,
    [CorrelationId] nvarchar(128) NOT NULL,
    [ActionType] nvarchar(32) NOT NULL,
    [EntityCode] nvarchar(128) NOT NULL,
    [PhysicalTableName] nvarchar(256) NULL,
    [RootRecordId] bigint NULL,
    [RootDisplayName] nvarchar(512) NULL,
    [ActorUserId] uniqueidentifier NULL,
    [ActorUserNameSnapshot] nvarchar(256) NULL,
    [EmpresaSubsidiariaId] int NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [Description] nvarchar(2048) NULL,
    [AffectedRecordCount] int NOT NULL,
    [ReversesOperationId] uniqueidentifier NULL,
    [Status] nvarchar(32) NOT NULL,
    [SchemaVersion] int NOT NULL,
    CONSTRAINT [PK_Operation] PRIMARY KEY ([OperationId])
);

CREATE TABLE [audit].[RecordKeyMap] (
    [Id] bigint NOT NULL IDENTITY,
    [OperationId] uniqueidentifier NOT NULL,
    [PhysicalTableName] nvarchar(256) NOT NULL,
    [OldPrimaryKeyJson] nvarchar(max) NOT NULL,
    [NewPrimaryKeyJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_RecordKeyMap] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecordKeyMap_Operation_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [audit].[Operation] ([OperationId]) ON DELETE CASCADE
);

CREATE TABLE [audit].[RecordSnapshot] (
    [SnapshotId] bigint NOT NULL IDENTITY,
    [OperationId] uniqueidentifier NOT NULL,
    [EntityCode] nvarchar(128) NOT NULL,
    [PhysicalTableName] nvarchar(256) NOT NULL,
    [PrimaryKeyJson] nvarchar(max) NOT NULL,
    [ForeignKeysJson] nvarchar(max) NOT NULL,
    [RowDataJson] nvarchar(max) NOT NULL,
    [DeleteOrder] int NOT NULL,
    [RestoreOrder] int NOT NULL,
    [IsRoot] bit NOT NULL,
    [DisplayName] nvarchar(512) NULL,
    CONSTRAINT [PK_RecordSnapshot] PRIMARY KEY ([SnapshotId]),
    CONSTRAINT [FK_RecordSnapshot_Operation_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [audit].[Operation] ([OperationId]) ON DELETE CASCADE
);

CREATE INDEX [IX_Operation_ActionType] ON [audit].[Operation] ([ActionType]);

CREATE INDEX [IX_Operation_ActorUserId] ON [audit].[Operation] ([ActorUserId]);

CREATE INDEX [IX_Operation_CorrelationId] ON [audit].[Operation] ([CorrelationId]);

CREATE INDEX [IX_Operation_EmpresaSubsidiariaId] ON [audit].[Operation] ([EmpresaSubsidiariaId]);

CREATE INDEX [IX_Operation_EntityCode] ON [audit].[Operation] ([EntityCode]);

CREATE INDEX [IX_Operation_OccurredAtUtc] ON [audit].[Operation] ([OccurredAtUtc]);

CREATE INDEX [IX_Operation_ReversesOperationId] ON [audit].[Operation] ([ReversesOperationId]);

CREATE INDEX [IX_RecordKeyMap_OperationId_PhysicalTableName] ON [audit].[RecordKeyMap] ([OperationId], [PhysicalTableName]);

CREATE INDEX [IX_RecordSnapshot_OperationId_PhysicalTableName_DeleteOrder] ON [audit].[RecordSnapshot] ([OperationId], [PhysicalTableName], [DeleteOrder]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907054022_AuditOperationsAndSnapshots', N'10.0.0');

COMMIT;
GO

