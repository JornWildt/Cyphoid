namespace Cyphoid.Core.Exceptions
{
  public abstract class CypherException : Exception
  {
    protected CypherException(string message, Exception innerException)
      : base(message, innerException) { }

    protected CypherException(string message)
        : base(message)
    {
    }
  }
}
