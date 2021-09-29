using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharplog.Engine
{
    public class TopDownEngine : IEngine
    {
        private void TopDown(IList<Expr> goals, int index, IndexedSet currentFacts, Universe jatalog, StackMap bindings, List<IDictionary<string, string>> res)
        {
            // TODO it should be possible to carry the current known facts in stack-wise manner to avoid repeated evaluations
            Expr goal = goals[index];
            bool lastGoal = index >= goals.Count - 1;
            if (goal.IsBuiltIn())
            {
                bool eval = goal.EvalBuiltIn(bindings);
                if ((eval && !goal.IsNegated()) || (!eval && goal.IsNegated()))
                {
                    if (lastGoal)
                    {
                        res.Add(bindings.CloneAsDictionary());
                    }
                    else
                    {
                        TopDown(goals, index + 1, currentFacts, jatalog, bindings, res);
                    }
                }

                return;
            }

            if (!goal.IsNegated())
            {
                foreach (Expr fact in currentFacts.GetIndexed(goal))
                {
                    int stackPointer = bindings.Stack.Count;
                    if (fact.GroundUnifyWith(goal, bindings))
                    {
                        if (lastGoal)
                        {
                            res.Add(bindings.CloneAsDictionary());
                        }
                        else
                        {
                            // More goals to match. Recurse with the remaining goals.
                            TopDown(goals, index + 1, currentFacts, jatalog, bindings, res);
                        }
                    }

                    bindings.RemoveUntil(stackPointer);
                }

                if (jatalog.TryGetFromIdb(goal.PredicateWithArity, out HashSet<Rule> rules))
                {
                    foreach (Rule rule in rules)
                    {
                        // Binding names need to be first mapped to head names for this rule
                        StackMap ruleBindings = new StackMap();
                        for (int i = 0; i < goal.Arity; i++)
                        {
                            string goalArgName = goal.GetTerms()[i];
                            string ruleArgName = rule.Head.GetTerms()[i];
                            if (Universe.IsVariable(goalArgName) && Universe.IsVariable(ruleArgName) && bindings.TryGetValue(goalArgName, out string value))
                            {
                                ruleBindings.Add(ruleArgName, value);
                            }

                            if (!Universe.IsVariable(goalArgName) && Universe.IsVariable(ruleArgName))
                            {
                                ruleBindings.Add(ruleArgName, goalArgName);
                            }

                            if (!Universe.IsVariable(ruleArgName) && !Universe.IsVariable(ruleArgName) && goalArgName != ruleArgName)
                            {
                                goto nextRule;
                            }
                        }

                        if (goal.IsGround())
                        {
                            throw new InvalidOperationException();
                        }

                        List<IDictionary<string, string>> ruleRes = new List<IDictionary<string, string>>();
                        TopDown(rule.Body, 0, currentFacts, jatalog, ruleBindings, ruleRes);

                        if (ruleRes.Count == 0)
                        {
                            goto nextRule;
                        }

                        foreach (IDictionary<string, string> ruleBindingResults in ruleRes)
                        {
                            int stackPointer = bindings.Stack.Count;
                            for (int i = 0; i < goal.Arity; i++)
                            {
                                string goalArgName = goal.GetTerms()[i];
                                string ruleArgName = rule.Head.GetTerms()[i];
                                if (Universe.IsVariable(goalArgName) && Universe.IsVariable(ruleArgName) && ruleBindingResults.TryGetValue(ruleArgName, out string value))
                                {
                                    if (bindings.TryGetValue(goalArgName, out string existing))
                                    {
                                        if (value != existing)
                                        {
                                            throw new InvalidOperationException();
                                        }
                                    }
                                    else
                                    {
                                        bindings.Add(goalArgName, value);
                                    }
                                }

                                if (!Universe.IsVariable(goalArgName) && Universe.IsVariable(ruleArgName) && ruleBindingResults.TryGetValue(ruleArgName, out value) && value != goalArgName)
                                {
                                    bindings.RemoveUntil(stackPointer);
                                    goto nextResult;
                                }
                            }

                            // currentFacts.Add(rule.Head.Substitute(ruleBindingResults));

                            if (lastGoal)
                            {
                                res.Add(bindings.CloneAsDictionary());
                            }
                            else
                            {
                                // More goals to match. Recurse with the remaining goals.
                                TopDown(goals, index + 1, currentFacts, jatalog, bindings, res);
                            }

                            bindings.RemoveUntil(stackPointer);

                            nextResult: continue;
                        }

                        nextRule: continue;
                    }
                }
            }
            else
            {
                if (bindings != null)
                {
                    goal = goal.Substitute(bindings.DictionaryObject());
                }

                if (goal.IsGround())
                {
                    // currentFacts.Add(goal);
                    return;
                }

                foreach (Expr fact in currentFacts.GetIndexed(goal))
                {
                    if (fact.GroundUnifyWith(goal, bindings))
                    {
                        // currentFacts.Add(goal.Substitute(bindings.DictionaryObject()));
                        return;
                    }
                }

                if (jatalog.TryGetFromIdb(goal.PredicateWithArity, out HashSet<Rule> rules))
                {
                    foreach (Rule rule in rules)
                    {
                        // Binding names need to be first mapped to head names for this rule
                        StackMap ruleBindings = new StackMap();
                        for (int i = 0; i < goal.Arity; i++)
                        {
                            string goalArgName = goal.GetTerms()[i];
                            string ruleArgName = rule.Head.GetTerms()[i];
                            if (Universe.IsVariable(goalArgName) && Universe.IsVariable(ruleArgName) && bindings.TryGetValue(goalArgName, out string value))
                            {
                                ruleBindings.Add(ruleArgName, value);
                            }

                            if (!Universe.IsVariable(goalArgName) && Universe.IsVariable(ruleArgName))
                            {
                                ruleBindings.Add(ruleArgName, goalArgName);
                            }

                            if (!Universe.IsVariable(ruleArgName) && !Universe.IsVariable(ruleArgName) && goalArgName == ruleArgName)
                            {
                                goto nextRule;
                            }
                        }

                        List<IDictionary<string, string>> ruleRes = new List<IDictionary<string, string>>();
                        TopDown(rule.Body, 0, currentFacts, jatalog, ruleBindings, ruleRes);

                        if (ruleRes.Count > 0)
                        {
                            foreach (IDictionary<string, string> ruleBindingResults in ruleRes)
                            {
                                // currentFacts.Add(rule.Head.Substitute(ruleBindingResults));
                            }

                            return;
                        }

                        nextRule: continue;
                    }
                }

                // not found
                if (lastGoal)
                {
                    res.Add(bindings.CloneAsDictionary());
                }
                else
                {
                    TopDown(goals, index + 1, currentFacts, jatalog, bindings, res);
                }
            }
        }

        /// <exception cref="Sharplog.DatalogException"/>
        public List<IDictionary<string, string>> Query(Universe jatalog, IList<Expr> goals)
        {
            if ((goals.Count == 0))
            {
                return new List<IDictionary<string, string>>(0);
            }

            // Reorganize the goals so that negated literals are at the end.
            List<Expr> orderedGoals = ReorderQuery(goals);

            List<IDictionary<string, string>> res = new List<IDictionary<string, string>>();
            StackMap bindings = new StackMap();
            IndexedSet currentFacts = new IndexedSet();
            currentFacts.AddAll(jatalog.GetEdbProvider().AllFacts().All);
            TopDown(orderedGoals, 0, currentFacts, jatalog, bindings, res);
            return res;
        }

        public void TransformNewRule(Rule newRule)
        {
            newRule.SetBody(ReorderQuery(newRule.Body));
        }

        private List<Expr> ReorderQuery(IList<Expr> query)
        {
            List<Expr> ordered = new List<Expr>(query.Count);
            foreach (Expr e in query)
            {
                if (!e.IsNegated() && !(e.IsBuiltIn() && !e.predicate.Equals("=")))
                {
                    ordered.Add(e);
                }
            }
            // Note that a rule like s(A, B) :- r(A, B), X = Y, q(Y), A > X. will cause an error relating to both sides
            // of the '=' being unbound, and it can be fixed by moving the '=' operators to here, but I've decided against
            // it, because the '=' should be evaluated ASAP, and it is difficult to determine programatically when that is.
            // The onus is thus on the user to structure '=' operators properly.
            foreach (Expr e in query)
            {
                if (e.IsNegated() || (e.IsBuiltIn() && !e.predicate.Equals("=")))
                {
                    ordered.Add(e);
                }
            }
            return ordered;
        }
    }
}