using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core
{
  public interface ICypherParser
  {
    QueryNode ParseQuery(string query);
  }
}
