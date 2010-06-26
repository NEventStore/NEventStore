CREATE TABLE [Aggregates]
(
    [Id] GUID,
    [Version] BIGINT CHECK ([Version] >= 0),
    [Snapshot] BIGINT CHECK ([Snapshot] >= 0),
    [Created] DATETIME,
    [RuntimeType] NVARCHAR(256),
    CONSTRAINT [PK_Aggregates] PRIMARY KEY ([Id])
);

CREATE TABLE [Events]
(
    [Id] GUID NOT NULL, 
    [Version] BIGINT NOT NULL CHECK ([Version] > 0),
    [Sequence] AUTOINC CHECK ([Sequence] > 0), 
    [Created] DATETIME NOT NULL, 
    [RuntimeType] NVARCHAR(256) NOT NULL, 
    [Payload] BLOB NOT NULL, 
    CONSTRAINT [PK_Events] PRIMARY KEY ([Id], [Version])
);

CREATE TABLE [Snapshots]
(
    [Id] GUID NOT NULL,
    [Version] BIGINT NOT NULL CHECK ([Version] > 0),
    [Created] DATETIME NOT NULL,
    [RuntimeType] NVARCHAR(256) NOT NULL,
    [Payload] BLOB NOT NULL,
    CONSTRAINT [PK_Snapshots] PRIMARY KEY ([Id], [Version])
);

CREATE UNIQUE INDEX [IX_Events] ON [Events] ([Sequence]);