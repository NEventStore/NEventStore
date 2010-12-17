/*
DROP TABLE Dispatch;
DROP TABLE Commits;
DROP TABLE Streams;
*/

CREATE TABLE Streams
(
       StreamId binary(16) NOT NULL CHECK (StreamId != 0),
       Name nvarchar(256) NOT NULL,
       HeadRevision bigint NOT NULL CHECK (HeadRevision > 0),
       SnapshotRevision bigint NOT NULL CHECK (SnapshotRevision >= 0),
       CONSTRAINT PK_Streams PRIMARY KEY (StreamId)
);

CREATE TABLE Commits
(
       StreamId binary(16) NOT NULL,
       CommitId binary(16) NOT NULL CHECK (CommitId != 0),
       StreamRevision bigint NOT NULL CHECK (StreamRevision > 0),
       CommitSequence bigint NOT NULL CHECK (CommitSequence > 0),
       SystemSequence bigint NOT NULL PRIMARY KEY AUTO_INCREMENT,
       Headers blob NULL,
       Payload blob NOT NULL,
       Snapshot blob NULL
);
CREATE UNIQUE INDEX PK_Commits ON Commits (StreamId, CommitSequence);
CREATE UNIQUE INDEX IX_Commits_CommitId ON Commits (StreamId, CommitId);
CREATE UNIQUE INDEX IX_Commits_Revisions ON Commits (StreamId, StreamRevision);

CREATE TABLE Dispatch
(
       DispatchId bigint NOT NULL PRIMARY KEY AUTO_INCREMENT,
       StreamId binary(16) NOT NULL,
       CommitSequence bigint NOT NULL
);
CREATE UNIQUE INDEX IX_Dispatch ON Dispatch (StreamId, CommitSequence);

/* TODO: triggers to protect referential integrity as well as commits */