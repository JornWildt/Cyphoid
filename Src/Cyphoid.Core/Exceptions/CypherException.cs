namespace Cyphoid.Core.Exceptions
{
  public abstract class CypherException : Exception
  {
    protected CypherException(string message)
        : base(message)
    {
    }
  }
}
