/*
DROP TABLE Events;
DROP TABLE Commands;
DROP TABLE Snapshots;
DROP TABLE Aggregates;
*/

CREATE TABLE Aggregates
(
    Id BINARY(16) NOT NULL,
    Version BIGINT NOT NULL CHECK (Version >= 0),
    Snapshot BIGINT NOT NULL CHECK (Snapshot >= 0),
    Created DATETIME NOT NULL,
    RuntimeType NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_Aggregates PRIMARY KEY (Id)
);

CREATE TABLE Commands
(
    Id BINARY(16) NOT NULL,
    Payload BLOB,
    CONSTRAINT PK_Commands PRIMARY KEY (Id)
);

CREATE TABLE Events
(
    Id BINARY(16) NOT NULL,
    Version BIGINT NOT NULL CHECK (Version > 0),
    CommitSequence BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT CHECK (CommitSequence > 0),
    Created DATETIME NOT NULL,
    CommandId BINARY(16),
    Payload BLOB NOT NULL
);

CREATE TABLE Snapshots
(
    Id BINARY(16) NOT NULL,
    Version BIGINT NOT NULL CHECK (Version > 0),
    Created DATETIME NOT NULL,
    Payload BLOB NOT NULL,
    CONSTRAINT PK_Snapshots PRIMARY KEY (Id, Version)
);

CREATE UNIQUE INDEX PK_Events ON Events (Id, Version);
CREATE INDEX IX_Events_Commands ON Events (CommandId);

ALTER TABLE Events ADD CONSTRAINT FK_Events_Aggregates FOREIGN KEY (Id) REFERENCES Aggregates (Id);
ALTER TABLE Events ADD CONSTRAINT FK_Events_Commands FOREIGN KEY (CommandId) REFERENCES Commands (Id);
ALTER TABLE Snapshots ADD CONSTRAINT FK_Snapshots_Aggregates FOREIGN KEY (Id) REFERENCES Aggregates (Id);