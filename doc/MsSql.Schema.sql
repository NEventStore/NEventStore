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
       [CommitId] [uniqueidentifier] NOT NULL CHECK ([CommitId] != 0x0),
       [Revision] [bigint] NOT NULL CHECK ([Revision] > 0),
       [CommitSequence] [bigint] NOT NULL CHECK ([CommitSequence] > 0),
       [SystemSequence] [bigint] IDENTITY(1,1) NOT NULL,
       [Payload] [varbinary](MAX) NOT NULL CHECK (DATALENGTH([Payload]) > 0),
       [Snapshot] [varbinary](MAX) NULL CHECK ([Snapshot] IS NULL OR DATALENGTH([Snapshot]) > 0),
       CONSTRAINT [PK_Commits] PRIMARY KEY CLUSTERED ([StreamId], [CommitSequence])
)
CREATE UNIQUE NONCLUSTERED INDEX [IX_Commits] ON [dbo].[Commits] ([StreamId], [CommitId])
CREATE UNIQUE NONCLUSTERED INDEX [IX_Commits_Revisions] ON [dbo].[Commits] ([StreamId], [Revision])

CREATE TABLE [dbo].[Dispatch]
(
       [DispatchId] [bigint] IDENTITY(1,1) NOT NULL,
       [StreamId] [uniqueidentifier] NOT NULL,
       [CommitSequence] [bigint] NOT NULL,
       CONSTRAINT [PK_Dispatch] PRIMARY KEY CLUSTERED ([DispatchId])
)

ALTER TABLE [dbo].[Commits] WITH CHECK ADD CONSTRAINT [FK_Commits_Streams] FOREIGN KEY([StreamId])
REFERENCES [dbo].[Streams] ([StreamId])
ALTER TABLE [dbo].[Commits] CHECK CONSTRAINT [FK_Commits_Streams]

ALTER TABLE [dbo].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_Dispatch_Commits] FOREIGN KEY([StreamId], [CommitSequence])
REFERENCES [dbo].[Commits] ([StreamId], [CommitSequence])
ALTER TABLE [dbo].[Dispatch] CHECK CONSTRAINT [FK_Dispatch_Commits]

GO
CREATE TRIGGER [dbo].[PreventChangesToCommits] ON [dbo].[Commits] FOR UPDATE
AS BEGIN

       IF (UPDATE([StreamId])
       OR UPDATE([CommitSequence])
       OR UPDATE([CommitId])
       OR UPDATE([SystemSequence])
       OR UPDATE([Revision])
       OR UPDATE([Payload]))
       BEGIN
              RAISERROR('Commits cannot be modified.', 16, 1)
              ROLLBACK TRANSACTION
       END

END;
