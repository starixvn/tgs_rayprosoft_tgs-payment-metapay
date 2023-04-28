namespace Tgs.Payment.MetaPay;

internal class ApiFailException : Exception
{
    public ApiFailException()
    {
    }

    public ApiFailException(string? message) : base(message)
    {
    }
}