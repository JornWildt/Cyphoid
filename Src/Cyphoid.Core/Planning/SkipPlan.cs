using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Planning
{
  public record SkipPlan(
    PipelinePlan Input,
    ExprNode Skip) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      return parameters =>
      {
        var value = Skip.Bind(parameters).CompileEvaluator()(new Row(RowSchema.Empty));
        if (value.Type != MixedValue.ValueType.Int || value.AsInt() < 0 || value.AsInt() > int.MaxValue)
        {
          var error = new RuntimeException($"SKIP requires an integer between 0 and {int.MaxValue}.",
            value.Type == MixedValue.ValueType.Int ? CypherErrorCode.InvalidArgumentValue : CypherErrorCode.TypeMismatch);
          error.Locate(Skip.SourceSpan);
          throw error;
        }
        // SKIP applies to this exact row stream; never pass an offset to scans or expansions.
        return new SkipReferenceOperator(input(parameters), (int)value.AsInt());
      };
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Skip");
      Input.PrettyPrint(sb);
    }
  }
}
