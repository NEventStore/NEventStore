namespace NEventStore.Example
{
    public class AuthorizationPipelineHook : PipelineHookBase
    {
        public override ICommit? SelectCommit(ICommit committed)
        {
            // return null if the user isn't authorized to see this commit
            return committed;
        }

        public override void PostCommit(ICommit committed)
        {
            // anything to do after the commit has been persisted.
        }

        public override bool PreCommit(CommitAttempt attempt)
        {
            // Can easily do logging or other such activities here
            return true; // true == allow commit to continue, false = stop.
        }
    }
}