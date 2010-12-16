/*
DROP TABLE Dispatch;
DROP TABLE Commits;
DROP TABLE Streams;
*/

CREATE TABLE Streams
(
       StreamId guid NOT NULL CHECK (StreamId != 0),
       Name nvarchar(256) NOT NULL,
       HeadRevision bigint NOT NULL CHECK (HeadRevision > 0),
       SnapshotRevision bigint NOT NULL CHECK (SnapshotRevision >= 0) DEFAULT(0),
       CONSTRAINT PK_Streams PRIMARY KEY (StreamId)
);

CREATE TABLE Commits
(
       StreamId guid NOT NULL,
       CommitId guid NOT NULL CHECK (CommitId != 0),
       StreamRevision bigint NOT NULL CHECK (StreamRevision > 0),
       CommitSequence bigint NOT NULL CHECK (CommitSequence > 0),
       SystemSequence bigint IDENTITY(1,1) NOT NULL,
       Headers blob NULL,
       Payload blob NOT NULL,
       Snapshot blob NULL,
       CONSTRAINT PK_Commits PRIMARY KEY (StreamId, CommitSequence)
);

CREATE UNIQUE INDEX IX_Commits ON Commits (StreamId, CommitId);
CREATE UNIQUE INDEX IX_Commits_Revisions ON Commits (StreamId, StreamRevision);

CREATE TABLE Dispatch
(
       DispatchId bigint IDENTITY(1,1) NOT NULL,
       StreamId guid NOT NULL,
       CommitSequence bigint NOT NULL,
       CONSTRAINT PK_Dispatch PRIMARY KEY (DispatchId)
);

/* TODO: triggers to protect referential integrity as well as commits */