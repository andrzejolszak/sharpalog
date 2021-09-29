using System.Collections.Generic;

namespace Sharplog.Engine
{
    public interface IEngine
    {
        List<IDictionary<string, string>> Query(Universe jatalog, IList<Expr> goals);

        void TransformNewRule(Rule newRule);
    }
}