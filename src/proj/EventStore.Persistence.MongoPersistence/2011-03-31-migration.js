db.Commits.dropIndex('GetFrom_Index');
db.Commits.find().forEach(function(c) { c.Events = new Array(); for(var i = 0; i < c.Payload.length; i++) { c.Events[i] = { StreamRevision: c.StartingStreamRevision++, Payload: c.Payload[i] }; }; delete c.StartingStreamRevision; delete c.StreamRevision; delete c.Payload; db.Commits.save(c); });
db.Commits.ensureIndex({'_id.StreamId': 1, 'Events.StreamRevision': 1 }, { name: 'GetFrom_Index', unique: true });

db.Streams.find().forEach(function(x) { x.Unsnapshotted = x.HeadRevision - x.SnapshotRevision; db.Streams.save(x); });
db.Streams.ensureIndex({'Unsnapshotted': 1 }, { name: 'Unsnapshotted_Index', unique: false });