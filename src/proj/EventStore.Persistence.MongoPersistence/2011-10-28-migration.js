// Script to convert MongoDB EventStore to more compact and efficient version
// 
// Usage: execute script against the event store using mongo.exe command line utility, e.g.:
//
//     C:\data\bin\>mongo EventStore 2011-10-28-migration.js
//
// Important: Backup your database before running and ideally execute against a replica.

print('Converting Streams Collection');
db.Streams.dropIndexes();
db.Streams.update({},{$rename:{SnapshotRevision:'s',HeadRevision:'h',Unsnapshotted:'u'}},false,true);
print('Rebuilding Streams Collection Indexes');
db.Streams.ensureIndex({ u: 1 }, { name: 'Unsnapshotted' });
print('Streams Collection Completed');

print('Converting Snapshots Collection');
db.Snapshots.renameCollection('SnapshotsTemp');
db.SnapshotsTemp.find().forEach(function(s) { 
	var id = s._id;
	s._id = { i:s._id.StreamId, r:NumberInt(s._id.StreamRevision)};
	s.p = s.Payload; 
	delete s.Payload; 
	db.Snapshots.insert(s);
	db.SnapshotsTemp.remove({_id:id}); 
});
db.SnapshotsTemp.drop();
print('Streams Collection Completed');

print('Converting Commits Collection');
db.Commits.dropIndexes();
db.Commits.renameCollection('CommitsTemp');
db.CommitsTemp.find().forEach(function(c) { 
	var id = c._id;

	for(var i = 0; i < c.Events.length; i++) { 
		c.Events[i].r = NumberInt(c.Events[i].StreamRevision); 
		c.Events[i].p = c.Events[i].Payload;
		delete c.Events[i].StreamRevision; 
		delete c.Events[i].Payload;
		c.Events[i].p.h = c.Events[i].p.Headers;
		c.Events[i].p.b = c.Events[i].p.Body;
		delete c.Events[i].p.Headers;
		delete c.Events[i].p.Body;
	};

	c.i = c._id.StreamId; 
	c.n = NumberInt(c._id.CommitSequence); 
	c.h = c.Headers; 
	c.s = c.CommitStamp; 
	c.e = c.Events;
	c.d = c.Dispatched; 
	c._id = c.CommitId; 

	delete c.CommitStamp; 
	delete c.CommitId; 
	delete c.Headers; 
	delete c.Events; 
	delete c.Dispatched;

	db.Commits.insert(c);
	db.CommitsTemp.remove({_id:id});
});
db.CommitsTemp.drop();
print('Rebuilding Commits Collection Indexes');
db.Commits.ensureIndex({ d:1 }, { name:'Dispatch' });
db.Commits.ensureIndex({ i:1, n:1 }, { name:'UniqueCommit', unique:true });
db.Commits.ensureIndex({ i:1, 'e.r':1 }, { name:'GetFromRevision' });
db.Commits.ensureIndex({ s:1 }, { name:'GetFromDate' });
print('Commits Collection Completed');