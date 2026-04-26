namespace Aparesk.Eskineria.Core.Shared.Exceptions;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(NormalizeMessage(message))
    {
    }

    private static string NormalizeMessage(string message)
    {
        return string.IsNullOrWhiteSpace(message)
            ? "A domain rule was violated."
            : message.Trim();
    }
}
