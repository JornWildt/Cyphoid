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
    void InstallPlugin(Procedures.ICypherProcedurePlugin plugin);

    QueryNode ParseQuery(string query);
  }
}
