/*
DROP TABLE Events;
DROP TABLE Commands;
DROP TABLE Snapshots;
DROP TABLE Aggregates;
*/

CREATE TABLE [dbo].[Aggregates]
(
    [Id] [uniqueidentifier] NOT NULL,
    [TenantId] [uniqueidentifier] NOT NULL,
    [Version] [bigint] NOT NULL CHECK ([Version] > 0),
    [Snapshot] [bigint] NOT NULL CHECK ([Snapshot] >= 0),
    [Created] [datetime] NOT NULL DEFAULT (GETUTCDATE()),
    [RuntimeType] [nvarchar](256) NOT NULL,
    CONSTRAINT [PK_Aggregates] PRIMARY KEY CLUSTERED ([Id])
)

CREATE TABLE [dbo].[Commands]
(
    [Id] [uniqueidentifier] NOT NULL,
    [Payload] [varbinary](MAX),
    CONSTRAINT [PK_Commands] PRIMARY KEY CLUSTERED ([Id])
)

CREATE TABLE [dbo].[Events]
(
    [Id] [uniqueidentifier] NOT NULL,
    [Version] [bigint] NOT NULL CHECK ([Version] > 0),
    [CommitSequence] [bigint] IDENTITY(1,1) NOT NULL,
    [Created] [datetime] NOT NULL DEFAULT (GETUTCDATE()),
    [CommandId] [uniqueidentifier],
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

CREATE UNIQUE NONCLUSTERED INDEX [IX_Events] ON [dbo].[Events] ([CommitSequence])
CREATE INDEX [IX_Events_CommandId] ON [Events] ([CommandId])

ALTER TABLE [dbo].[Events] WITH CHECK ADD CONSTRAINT [FK_Events_Aggregates] FOREIGN KEY([Id])
REFERENCES [dbo].[Aggregates] ([Id])
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Aggregates]

ALTER TABLE [dbo].[Snapshots] WITH CHECK ADD CONSTRAINT [FK_Snapshots_Aggregates] FOREIGN KEY([Id])
REFERENCES [dbo].[Aggregates] ([Id])
ALTER TABLE [dbo].[Snapshots] CHECK CONSTRAINT [FK_Snapshots_Aggregates]

ALTER TABLE [dbo].[Events] WITH CHECK ADD CONSTRAINT [FK_Events_Commands] FOREIGN KEY([CommandId])
REFERENCES [dbo].[Commands] ([Id])
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Aggregates]