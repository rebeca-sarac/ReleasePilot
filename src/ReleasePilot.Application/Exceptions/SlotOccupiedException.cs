namespace ReleasePilot.Application.Exceptions;

public sealed class SlotOccupiedException : Exception
{
    public SlotOccupiedException()
        : base("An InProgress promotion already exists for this application and environment.") { }
}
