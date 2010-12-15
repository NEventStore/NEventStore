/*
DROP TABLE [dbo].[Dispatch];
DROP TABLE [dbo].[Commits];
DROP TABLE [dbo].[Streams];
*/

CREATE TABLE [dbo].[Streams]
(
       [StreamId] [uniqueidentifier] NOT NULL CHECK ([StreamId] != 0x0),
       [Name] [nvarchar](256) NOT NULL,
       [HeadRevision] [bigint] NOT NULL CHECK ([HeadRevision] > 0),
       [SnapshotRevision] [bigint] NOT NULL CHECK ([SnapshotRevision] >= 0) DEFAULT(0),
       CONSTRAINT [PK_Streams] PRIMARY KEY CLUSTERED ([StreamId])
)

CREATE TABLE [dbo].[Commits]
(
       [StreamId] [uniqueidentifier] NOT NULL,
       [Sequence] [bigint] NOT NULL CHECK ([Sequence] > 0),
       [CommitId] [uniqueidentifier] NOT NULL,
       [SystemSequence] [bigint] IDENTITY(1,1) NOT NULL,
       [Revision] [bigint] NOT NULL CHECK ([Revision] > 0),
       [Items] [smallint] NOT NULL CHECK ([Items] > 0) DEFAULT(0),
       [Payload] [varbinary](MAX) NOT NULL CHECK (DATALENGTH([Payload]) > 0),
       [Snapshot] [varbinary](MAX) NULL CHECK ([Snapshot] IS NULL OR DATALENGTH([Snapshot]) > 0),
       CONSTRAINT [PK_Commits] PRIMARY KEY CLUSTERED ([StreamId], [Sequence])
)
CREATE UNIQUE NONCLUSTERED INDEX [IX_Commits] ON [dbo].[Commits] ([StreamId], [CommitId])
CREATE UNIQUE NONCLUSTERED INDEX [IX_Commits_Revisions] ON [dbo].[Commits] ([StreamId], [Revision], [Items])

CREATE TABLE [dbo].[Dispatch]
(
       [DispatchId] [bigint] IDENTITY(1,1) NOT NULL,
       [StreamId] [uniqueidentifier] NOT NULL,
       [Sequence] [bigint] NOT NULL,
       CONSTRAINT [PK_Dispatch] PRIMARY KEY CLUSTERED ([DispatchId])
)

ALTER TABLE [dbo].[Commits] WITH CHECK ADD CONSTRAINT [FK_Commits_Streams] FOREIGN KEY([StreamId])
REFERENCES [dbo].[Streams] ([StreamId])
ALTER TABLE [dbo].[Commits] CHECK CONSTRAINT [FK_Commits_Streams]

ALTER TABLE [dbo].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_Dispatch_Commits] FOREIGN KEY([StreamId], [Sequence])
REFERENCES [dbo].[Commits] ([StreamId], [Sequence])
ALTER TABLE [dbo].[Dispatch] CHECK CONSTRAINT [FK_Dispatch_Commits]

GO
CREATE TRIGGER [dbo].[PreventChangesToCommits] ON [dbo].[Commits] FOR UPDATE
AS BEGIN

       IF (UPDATE([StreamId])
       OR UPDATE([Sequence])
       OR UPDATE([CommitId])
       OR UPDATE([SystemSequence])
       OR UPDATE([Revision])
       OR UPDATE([Items])
       OR UPDATE([Payload]))
       BEGIN
              RAISERROR('Commits cannot be modified.', 16, 1)
              ROLLBACK TRANSACTION
       END

END;

GO
CREATE TRIGGER [dbo].[PreventChangesToStreamId] ON [dbo].[Streams] FOR UPDATE
AS BEGIN

       IF UPDATE([StreamId])
       BEGIN
              RAISERROR('Stream identifiers cannot be modified.', 16, 1)
              ROLLBACK TRANSACTION
       END

END;

GO
CREATE TRIGGER [dbo].[PreventChangesToDispatches] ON [dbo].[Dispatch] FOR UPDATE
AS BEGIN

       RAISERROR('Dispatches cannot be modified.', 16, 1)
       ROLLBACK TRANSACTION

END;