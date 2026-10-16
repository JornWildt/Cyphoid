namespace Cyphoid.Core.Exceptions
{
  internal class RuntimeException : CypherException
  {
    public RuntimeException(string message, Exception innerException)
      : base(message, innerException) { }

    public RuntimeException(string message)
      : base(message)
    {
    }
  }
}
