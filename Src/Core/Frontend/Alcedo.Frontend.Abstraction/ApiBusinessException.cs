namespace Alcedo.Frontend.Abstraction;

public class ApiBusinessException : Exception
{
    public ApiBusinessException(string message) : base(message)
    {
    }
}