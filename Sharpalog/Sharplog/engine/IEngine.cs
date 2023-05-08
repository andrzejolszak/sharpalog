using System.Collections.Generic;

namespace Sharplog.Engine
{
    public interface IEngine
    {
        List<IDictionary<string, string>> Query(Universe jatalog, List<Expr> goals);

        List<Expr> ReorderQuery(List<Expr> query);

        IndexedSet ExpandDatabase(Universe jatalog, List<Expr> goals);

        List<IDictionary<string, string>> MatchGoals(IList<Expr> goals, int index, IndexedSet facts, StackMap bindings);

        void TransformNewRule(Rule newRule);
    }
}