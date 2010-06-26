CREATE TABLE [dbo].[Aggregates](
	[Id] [uniqueidentifier] NOT NULL,
	[Version] [bigint] NOT NULL,
	[Snapshot] [bigint] NOT NULL,
	[Created] [datetime] NOT NULL,
	[RuntimeType] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_Aggregates] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))

CREATE TABLE [dbo].[Snapshots](
	[Id] [uniqueidentifier] NOT NULL,
	[Version] [bigint] NOT NULL,
	[Created] [datetime] NOT NULL,
	[RuntimeType] [nvarchar](256) NOT NULL,
	[Payload] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_Snapshots] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[Version] ASC
))

CREATE TABLE [dbo].[Events](
	[Id] [uniqueidentifier] NOT NULL,
	[Version] [bigint] NOT NULL,
	[Created] [datetime] NOT NULL,
	[RuntimeType] [nvarchar](256) NOT NULL,
	[Payload] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[Version] ASC
))

ALTER TABLE [dbo].[Aggregates]  WITH CHECK ADD  CONSTRAINT [CK_Aggregates_Min_Version] CHECK  (([Version]>=(0)))
ALTER TABLE [dbo].[Aggregates] CHECK CONSTRAINT [CK_Aggregates_Min_Version]
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [CK_Events_Min_Version] CHECK  (([Version]>=(1)))
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [CK_Events_Min_Version]

ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [FK_Events_Aggregates] FOREIGN KEY([Id])
REFERENCES [dbo].[Aggregates] ([Id])
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Aggregates]

ALTER TABLE [dbo].[Snapshots]  WITH CHECK ADD  CONSTRAINT [FK_Snapshots_Aggregates] FOREIGN KEY([Id])
REFERENCES [dbo].[Aggregates] ([Id])
ALTER TABLE [dbo].[Snapshots] CHECK CONSTRAINT [FK_Snapshots_Aggregates]