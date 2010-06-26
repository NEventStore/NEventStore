CREATE TABLE [Aggregates]
(
    [Id] GUID NOT NULL,
    [Version] BIGINT NOT NULL CHECK ([Version] >= 0),
    [Snapshot] BIGINT NOT NULL CHECK ([Snapshot] >= 0),
    [Created] DATETIME NOT NULL,
    [RuntimeType] NVARCHAR(256) NOT NULL,
    CONSTRAINT [PK_Aggregates] PRIMARY KEY ([Id])
);

CREATE TABLE [Events]
(
    [Id] GUID NOT NULL,
    [Version] BIGINT NOT NULL CHECK ([Version] > 0),
    [Sequence] INTEGER PRIMARY KEY,
    [Created] DATETIME NOT NULL,
    [RuntimeType] NVARCHAR(256) NOT NULL,
    [Payload] BLOB NOT NULL
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

CREATE UNIQUE INDEX [PK_Events] ON [Events] ([Id], [Version]);