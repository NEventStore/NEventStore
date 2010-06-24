CREATE TABLE [dbo].[Aggregates](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[Version] [int] NOT NULL,
	[Snapshot] [int] NOT NULL,
	[RuntimeType] [varchar](256) NOT NULL,
	[Created] [datetime] NOT NULL,
 CONSTRAINT [PK_Aggregates] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [dbo].[Snapshots](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[Version] [int] NOT NULL,
	[RuntimeType] [varchar](64) NOT NULL,
	[Created] [datetime] NOT NULL,
	[Payload] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_Snapshots] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[Version] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [dbo].[Events](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[Version] [int] NOT NULL,
	[RuntimeType] [varchar](256) NOT NULL,
	[Created] [datetime] NOT NULL,
	[Payload] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[Version] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[Aggregates]  WITH CHECK ADD  CONSTRAINT [CK_Aggregates_Min_Version] CHECK  (([Version]>=(0)))
ALTER TABLE [dbo].[Aggregates] CHECK CONSTRAINT [CK_Aggregates_Min_Version]
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [CK_Events_Min_Version] CHECK  (([Version]>=(1)))
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [CK_Events_Min_Version]
ALTER TABLE [dbo].[Snapshots] CHECK CONSTRAINT [FK_Snapshots_Aggregates]
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Aggregates]

ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [FK_Events_Aggregates] FOREIGN KEY([AggregateId])
REFERENCES [dbo].[Aggregates] ([AggregateId])
ALTER TABLE [dbo].[Snapshots]  WITH CHECK ADD  CONSTRAINT [FK_Snapshots_Aggregates] FOREIGN KEY([AggregateId])
REFERENCES [dbo].[Aggregates] ([AggregateId])