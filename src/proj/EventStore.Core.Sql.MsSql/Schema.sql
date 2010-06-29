CREATE TABLE [dbo].[Aggregates]
(
    [Id] [uniqueidentifier] NOT NULL,
    [Version] [bigint] NOT NULL CHECK ([Version] >= 0),
    [Snapshot] [bigint] NOT NULL CHECK ([Snapshot] >= 0),
    [Created] [datetime] NOT NULL DEFAULT (GETUTCDATE()),
    [RuntimeType] [nvarchar](256) NOT NULL,
    CONSTRAINT [PK_Aggregates] PRIMARY KEY CLUSTERED ([Id])
)

CREATE TABLE [dbo].[Events]
(
    [Id] [uniqueidentifier] NOT NULL,
    [Version] [bigint] NOT NULL CHECK ([Version] > 0),
    [PartitionSequence] [bigint] IDENTITY(1,1) NOT NULL,
    [Created] [datetime] NOT NULL DEFAULT (GETUTCDATE()),
    [Payload] [varbinary](MAX) NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED ([Id], [Version])
)

CREATE TABLE [dbo].[Snapshots]
(
    [Id] [uniqueidentifier] NOT NULL,
    [Version] [bigint] NOT NULL CHECK ([Version] > 0),
    [Created] [datetime] NOT NULL DEFAULT (GETUTCDATE()),
    [Payload] [varbinary](MAX) NOT NULL,
    CONSTRAINT [PK_Snapshots] PRIMARY KEY CLUSTERED ([Id], [Version])
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_Events] ON [dbo].[Events] ([PartitionSequence])

ALTER TABLE [dbo].[Events] WITH CHECK ADD CONSTRAINT [FK_Events_Aggregates] FOREIGN KEY([Id])
REFERENCES [dbo].[Aggregates] ([Id])
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Aggregates]

ALTER TABLE [dbo].[Snapshots] WITH CHECK ADD CONSTRAINT [FK_Snapshots_Aggregates] FOREIGN KEY([Id])
REFERENCES [dbo].[Aggregates] ([Id])
ALTER TABLE [dbo].[Snapshots] CHECK CONSTRAINT [FK_Snapshots_Aggregates]