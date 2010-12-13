/*
DROP TABLE [dbo].[Dispatch];
DROP TABLE [dbo].[Commits];
DROP TABLE [dbo].[Streams];
*/

CREATE TABLE [dbo].[Streams]
(
       [StreamId] [uniqueidentifier] NOT NULL CHECK ([StreamId] != 0x0),
       [Name] [nvarchar](256) NOT NULL,
       [Revision] [bigint] NOT NULL CHECK ([Revision] > 0),
       [Snapshot] [bigint] NOT NULL CHECK ([Snapshot] >= 0),
       CONSTRAINT [PK_Streams] PRIMARY KEY CLUSTERED ([StreamId])
)

CREATE TABLE [dbo].[Commits]
(
       [StreamId] [uniqueidentifier] NOT NULL,
       [Sequence] [bigint] NOT NULL CHECK ([Sequence] > 0),
       [CommitId] [uniqueidentifier] NOT NULL,
       [SystemSequence] [bigint] IDENTITY(1,1) NOT NULL,
       [Revision] [bigint] NOT NULL CHECK ([Revision] > 0),
       [Payload] [varbinary](MAX) NOT NULL CHECK (DATALENGTH([Payload]) > 0),
       [Snapshot] [varbinary](MAX) NULL CHECK ([Snapshot] IS NULL OR DATALENGTH([Snapshot]) > 0),
       CONSTRAINT [PK_Commits] PRIMARY KEY CLUSTERED ([StreamId], [Sequence])
)
CREATE UNIQUE NONCLUSTERED INDEX [IX_Commits] ON [dbo].[Commits] ([StreamId], [CommitId])

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
CREATE TRIGGER [dbo].[AddCommitsToDispatch] ON [dbo].[Commits] AFTER INSERT
AS BEGIN

       SET NOCOUNT ON;

       INSERT
         INTO [dbo].[Dispatch]
            ( [StreamId], [Sequence] )
       SELECT [StreamId], [Sequence]
         FROM [Inserted];

END;

GO
CREATE TRIGGER [dbo].[PointToLatestCommits] ON [dbo].[Commits] AFTER INSERT
AS BEGIN

       SET NOCOUNT ON;
       
       UPDATE [dbo].[Streams]
          SET [Revision] = [I].[Revision]
         FROM [dbo].[Streams] AS [S]
        INNER JOIN [Inserted] AS [I]
           ON [S].[StreamId] = [I].[StreamId]
        WHERE [S].[Revision] < [I].[Revision];

END;

GO
CREATE TRIGGER [dbo].[PointToLatestSnapshots] ON [dbo].[Commits] AFTER UPDATE
AS BEGIN

       SET NOCOUNT ON;

       IF NOT UPDATE([Snapshot]) RETURN;

       -- if a snapshot was supplied, point the Snapshots column of the associated row in the
       -- Streams table to it.
       UPDATE [dbo].[Streams]
          SET [Snapshot] = [U].[Revision]
         FROM [dbo].[Streams] AS [S]
        INNER JOIN [Updated] AS [U]
           ON [S].[StreamId] = [U].[StreamId]
        WHERE [U].[Snapshot] IS NOT NULL
          AND [S].[Snapshot] < [U].[Revision];

       -- if the snapshot was set to null/removed, point the Snapshots column of the associated row in the
       -- Streams table to the most recent snapshot, if any.
       UPDATE [dbo].[Streams]
          SET [Snapshot] = COALESCE(MAX([M].[Revision]), 0)
         FROM [dbo].[Streams] AS [S]
        INNER JOIN [Updated] AS [U]
           ON [S].[StreamId] = [U].[StreamId]
        INNER JOIN [dbo].[Streams] AS [M]
           ON [S].[StreamId] = [M].[StreamId]
        WHERE [U].[Snapshot] IS NULL
          AND [M].[Snapshot] IS NOT NULL;
          
END;

GO
CREATE TRIGGER [dbo].[PreventChangesToCommits] ON [dbo].[Commits] FOR UPDATE
AS BEGIN

       IF (UPDATE([StreamId])
       OR UPDATE([Sequence])
       OR UPDATE([CommitId])
       OR UPDATE([SystemSequence])
       OR UPDATE([Revision])
       OR UPDATE([Payload]))
       BEGIN
              RAISERROR('Commits cannot be modified.', 16, 1)
              ROLLBACK TRANSACTION
       END

END;