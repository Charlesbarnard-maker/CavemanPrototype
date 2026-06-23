namespace Caveman
{
    /// <summary>
    /// Anything that consumes assigned workers from the Colony pool to operate
    /// (collectors, workshops). Lets the UI and Colony treat them uniformly.
    /// </summary>
    public interface IStaffable
    {
        int AssignedWorkers { get; }
        int MaxWorkers { get; }
        bool TryAssign();
        void Unassign();
        string StaffLabel { get; }
    }
}
