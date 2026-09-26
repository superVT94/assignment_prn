namespace AssignmentPRN.DataAccess.Common;

public sealed class DuplicateEntityException : InvalidOperationException
{
    public DuplicateEntityException(string message)
        : base(message)
    {
    }

    public DuplicateEntityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
