using System.Runtime.CompilerServices;

// Let the Editor assembly (headless dev tools like SaveLoadAudit / MapGenAudit) reach the internal
// save/load restore accessors on runtime classes. Internals stay hidden from everything else.
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
