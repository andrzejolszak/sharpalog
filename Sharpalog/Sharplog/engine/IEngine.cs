﻿using System.Collections.Generic;

namespace Sharplog.Engine
{
    public interface IEngine
    {
        List<IDictionary<string, string>> Query(Universe jatalog, List<Expr> goals);

        List<Expr> ReorderQuery(List<Expr> query);

        void ExpandDatabase(Universe jatalog, List<Expr> goals, SignatureIndexedFactSet facts);

        List<IDictionary<string, string>> MatchGoals(IList<Expr> goals, int index, SignatureIndexedFactSet facts, VariableBindingStackMap bindings);

        void TransformNewRule(Rule newRule);
    }
}