using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning;

internal sealed record ParameterExecutionOperator(Func<QueryParameters, IOperator> Bind,
  IReadOnlyList<ParameterNode> References) : IOperator
{
  public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context,
    [EnumeratorCancellation] CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();
    var supplied = context.Parameters;
    var parameters = supplied as QueryParameters ?? new QueryParameters(supplied);
    foreach (var reference in References) parameters.Require(reference);
    ct.ThrowIfCancellationRequested();
    var operation = Bind(parameters);
    await foreach (var row in operation.ExecuteAsync(context, ct).WithCancellation(ct))
    {
      ct.ThrowIfCancellationRequested();
      yield return row;
    }
  }
}
