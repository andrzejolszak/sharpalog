using System.Collections.Generic;

namespace Sharplog.Engine
{
    public interface IEngine
    {
        List<IDictionary<string, string>> Query(Universe jatalog, IList<Expr> goals);

        IList<Expr> ReorderQuery(IList<Expr> query);

        IndexedSet ExpandDatabase(Universe jatalog, IList<Expr> goals);

        List<IDictionary<string, string>> MatchGoals(IList<Expr> goals, int index, IndexedSet facts, StackMap bindings);

        void TransformNewRule(Rule newRule);
    }
}