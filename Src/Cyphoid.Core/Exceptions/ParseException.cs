namespace Cyphoid.Core.Exceptions
{
  public class ParseException : CypherException
  {
    public ParseException(string message)
      : base(message)
    {
    }
  }
}
