/*
DROP TABLE Events;
DROP TABLE Commands;
DROP TABLE Snapshots;
DROP TABLE Aggregates;
*/

CREATE TABLE Aggregates
(
    Id BYTEA NOT NULL,
    TenantId BYTEA NOT NULL,
    Version BIGINT NOT NULL CHECK (Version > 0),
    Snapshot BIGINT NOT NULL CHECK (Snapshot >= 0),
    Created TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    RuntimeType VARCHAR(256) NOT NULL,
    CONSTRAINT PK_Aggregates PRIMARY KEY (Id)
);

CREATE TABLE Commands
(
    Id BYTEA NOT NULL,
    Payload TEXT,
    CONSTRAINT PK_Commands PRIMARY KEY (Id)
);

CREATE TABLE Events
(
    Id BYTEA NOT NULL,
    Version BIGINT NOT NULL CHECK (Version > 0),
    CommitSequence BIGSERIAL NOT NULL PRIMARY KEY CHECK (CommitSequence > 0),
    Created TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CommandId BYTEA,
    Payload BYTEA NOT NULL
);

CREATE TABLE Snapshots
(
    Id BYTEA NOT NULL,
    Version BIGINT NOT NULL CHECK (Version > 0),
    Created TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    Payload BYTEA NOT NULL,
    CONSTRAINT PK_Snapshots PRIMARY KEY (Id, Version)
);

CREATE UNIQUE INDEX PK_Events ON Events (Id, Version);
CREATE INDEX IX_Events_Commands ON Events (CommandId);

ALTER TABLE Events ADD CONSTRAINT FK_Events_Aggregates FOREIGN KEY (Id) REFERENCES Aggregates (Id);
ALTER TABLE Events ADD CONSTRAINT FK_Events_Commands FOREIGN KEY (CommandId) REFERENCES Commands (Id);
ALTER TABLE Snapshots ADD CONSTRAINT FK_Snapshots_Aggregates FOREIGN KEY (Id) REFERENCES Aggregates (Id);