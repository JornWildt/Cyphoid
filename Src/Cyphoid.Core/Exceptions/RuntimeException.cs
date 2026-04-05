namespace Cyphoid.Core.Exceptions
{
  internal class RuntimeException : CypherException
  {
    public RuntimeException(string message)
      : base(message)
    {
    }
  }
}
