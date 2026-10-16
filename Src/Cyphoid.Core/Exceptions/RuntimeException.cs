namespace Cyphoid.Core.Exceptions
{
  public class RuntimeException : CypherException
  {
    public RuntimeException(string message, Exception innerException,
      CypherErrorCode code = CypherErrorCode.EvaluationFailed)
      : base(message, innerException)
    {
      Code = code;
      Phase = CypherErrorPhase.Execution;
    }

    public RuntimeException(string message, CypherErrorCode code = CypherErrorCode.TypeMismatch)
      : base(message)
    {
      Code = code;
      Phase = CypherErrorPhase.Execution;
    }
  }
}
