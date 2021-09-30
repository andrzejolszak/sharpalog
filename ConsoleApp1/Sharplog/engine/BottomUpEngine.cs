using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharplog.Engine
{
    public class BottomUpEngine : IEngine
    {
        /* Returns a list of rules that are relevant to the query.
        If for example you're querying employment status, you don't care about family relationships, etc.
        The advantages of this of this optimization becomes bigger the more complex the rules get. */
        /* This basically constructs the dependency graph for semi-naive evaluation: In the returned map, the string
        is a predicate in the rules' heads that maps to a collection of all the rules that have that predicate in
        their body so that we can easily find the rules that are affected when new facts are deduced in different
        iterations of buildDatabase().
        For example if you have a rule p(X) :- q(X) then there will be a mapping from "q" to that rule
        so that when new facts q(Z) are deduced, the rule will be run in the next iteration to deduce p(Z) */
        /* Retrieves all the rules that are affected by a collection of facts.
        * This is used as part of the semi-naive evaluation: When new facts are generated, we
        * take a look at which rules have those facts in their bodies and may cause new facts
        * to be derived during the next iteration.
        * The `dependents` parameter was built earlier in the buildDependentRules() method */
        /* Match the goals in a rule to the facts in the database (recursively).
        * If the goal is a built-in predicate, it is also evaluated here. */

        /// <exception cref="Sharplog.DatalogException"/>
        public List<IDictionary<string, string>> Query(Universe jatalog, IList<Expr> goals)
        {
            if (goals.Count == 0)
            {
                return new List<IDictionary<string, string>>(0);
            }

            IList<Expr> orderedGoals = this.ReorderQuery(goals);
            IndexedSet factsForDownstreamPredicates = ExpandDatabase(jatalog, orderedGoals);

            // Now match the expanded database to the goals
            return MatchGoals(orderedGoals, 0, factsForDownstreamPredicates, new StackMap());
        }

        public IndexedSet ExpandDatabase(Universe jatalog, IList<Expr> goals)
        {
            // Compute all downstream predicate names for the goals by following the rules, their goals, their rules, and so on...
            (HashSet<Expr> downstreamPredicates, HashSet<Rule> rulesForDownstreamPredicates) = GetAllDownstreamPredicates(jatalog, goals);
            IndexedSet factsForDownstreamPredicates = new IndexedSet();
            foreach (Expr predicate in downstreamPredicates)
            {
                factsForDownstreamPredicates.AddAll(jatalog.GetEdbProvider().GetFacts(predicate));
            }

            // Build the database. A Set ensures that the facts are unique
            List<HashSet<Rule>> strata = ComputeNegationBasedCallGraphStratification(rulesForDownstreamPredicates);
            foreach (HashSet<Rule> rules in strata)
            {
                if (rules.Count > 0)
                {
                    ExpandStrata(factsForDownstreamPredicates, rules);
                }
            }

            return factsForDownstreamPredicates;
        }

        public void TransformNewRule(Rule newRule)
        {
            newRule.SetBody(ReorderQuery(newRule.Body));
        }

        public IList<Expr> ReorderQuery(IList<Expr> query)
        {
            IList<Expr> ordered = new List<Expr>(query.Count);
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

        /// <exception cref="Sharplog.DatalogException"/>
        private List<HashSet<Rule>> ComputeNegationBasedCallGraphStratification(HashSet<Rule> allRules)
        {
            List<HashSet<Rule>> strata = new List<HashSet<Rule>>(10);
            IDictionary<string, int> strats = new Dictionary<string, int>(allRules.Count);

            foreach (Rule rule in allRules)
            {
                string pred = rule.Head.PredicateWithArity;
                if (!strats.TryGetValue(pred, out int stratum))
                {
                    stratum = DepthFirstSearch(rule.Head, allRules, new List<Expr>(), 0);
                    strats[pred] = stratum;
                }

                while (stratum >= strata.Count)
                {
                    strata.Add(new HashSet<Rule>());
                }

                strata[stratum].Add(rule);
            }

            return strata;
        }

        /* Reorganize the goals in a query so that negated literals are at the end.
        A rule such as `a(X) :- not b(X), c(X)` won't work if the `not b(X)` is evaluated first, since X will not
        be bound to anything yet, meaning there are an infinite number of values for X that satisfy `not b(X)`.
        Reorganising the goals will solve the problem: every variable in the negative literals will have a binding
        by the time they are evaluated if the rule is /safe/, which we assume they are - see Rule#validate()
        Also, the built-in predicates (except `=`) should only be evaluated after their variables have been bound
        for the same reason; see [ceri] for more information. */
        /* Computes the stratification of the rules in the IDB by doing a depth-first search.
        * It throws a DatalogException if there are negative loops in the rules, in which case the
        * rules aren't stratified and cannot be computed. */
        /* The recursive depth-first method that computes the stratification of a set of rules */

        private (HashSet<Expr>, HashSet<Rule>) GetAllDownstreamPredicates(Universe jatalog, IList<Expr> originalGoals)
        {
            HashSet<string> relevant = new HashSet<string>();
            HashSet<Expr> relevantExpr = new HashSet<Expr>();
            HashSet<Rule> relevantRules = new HashSet<Rule>();
            List<Expr> goals = new List<Expr>(originalGoals);
            while (goals.Count != 0)
            {
                Expr expr = goals[0];
                goals.RemoveAt(0);
                if (relevant.Add(expr.PredicateWithArity))
                {
                    relevantExpr.Add(expr);
                    if (jatalog.TryGetFromIdb(expr.PredicateWithArity, out HashSet<Rule> rules))
                    {
                        foreach (Rule rule in rules)
                        {
                            relevantRules.Add(rule);
                            goals.AddRange(rule.Body);
                        }
                    }
                }
            }

            return (relevantExpr, relevantRules);
        }

        private IDictionary<int, List<Rule>> BuildDependentRules(HashSet<Rule> rules)
        {
            IDictionary<int, List<Rule>> map = new Dictionary<int, List<Rule>>();
            foreach (Rule rule in rules)
            {
                foreach (Expr goal in rule.Body)
                {
                    if (!map.TryGetValue(goal.Index(), out List<Rule> dependants))
                    {
                        dependants = new List<Rule>(rules.Count);
                        map[goal.Index()] = dependants;
                    }

                    if (!dependants.Contains(rule))
                    {
                        dependants.Add(rule);
                    }
                }
            }
            return map;
        }

        public List<IDictionary<string, string>> MatchGoals(IList<Expr> goals, int index, IndexedSet facts, StackMap bindings)
        {
            // PERF this flow allocs a lot of StackMaps with their Dictionaries
            Expr goal = goals[index];
            bool lastGoal = index >= goals.Count - 1;
            if (goal.IsBuiltIn())
            {
                bool eval = goal.EvalBuiltIn(bindings);
                if ((eval && !goal.IsNegated()) || (!eval && goal.IsNegated()))
                {
                    if (lastGoal)
                    {
                        return new List<IDictionary<string, string>> { bindings.CloneAsDictionary() };
                    }
                    else
                    {
                        return MatchGoals(goals, index + 1, facts, bindings);
                    }
                }

                return new List<IDictionary<string, string>>(0);
            }

            List<IDictionary<string, string>> answers = new List<IDictionary<string, string>>();
            if (!goal.IsNegated())
            {
                // Positive rule: Match each fact to the first goal.
                // If the fact matches: If it is the last/only goal then we can return the bindings
                // as an answer, otherwise we recursively check the remaining goals.
                foreach (Expr fact in facts.GetIndexed(goal))
                {
                    int stackPointer = bindings.Stack.Count;
                    if (fact.GroundUnifyWith(goal, bindings))
                    {
                        if (lastGoal)
                        {
                            answers.Add(bindings.CloneAsDictionary());
                        }
                        else
                        {
                            // More goals to match. Recurse with the remaining goals.
                            answers.AddRange(MatchGoals(goals, index + 1, facts, bindings));
                        }
                    }

                    bindings.RemoveUntil(stackPointer);
                }
            }
            else
            {
                // Negated rule: If you find any fact that matches the goal, then the goal is false.
                // See definition 4.3.2 of [bra2] and section VI-B of [ceri].
                // Substitute the bindings in the rule first.
                // If your rule is `und(X) :- stud(X), not grad(X)` and you're at the `not grad` part, and in the
                // previous goal stud(a) was true, then bindings now contains X:a so we want to search the database
                // for the fact grad(a).
                if (bindings != null)
                {
                    goal = goal.Substitute(bindings.DictionaryObject());
                }

                foreach (Expr fact in facts.GetIndexed(goal))
                {
                    if (fact.GroundUnifyWith(goal, bindings))
                    {
                        return new List<IDictionary<string, string>>(0);
                    }
                }

                // not found
                if (lastGoal)
                {
                    answers.Add(bindings.CloneAsDictionary());
                }
                else
                {
                    answers.AddRange(MatchGoals(goals, index + 1, facts, bindings));
                }
            }

            return answers;
        }

        /// <exception cref="Sharplog.DatalogException"/>
        private int DepthFirstSearch(Expr goal, HashSet<Rule> graph, List<Expr> visited, int level)
        {
            string pred = goal.PredicateWithArity;
            // Step (1): Guard against negative recursion
            bool negated = goal.IsNegated();
            // for error reporting
            for (int i = visited.Count - 1; i >= 0; i--)
            {
                Expr e = visited[i];
                if (e.PredicateWithArity.Equals(pred))
                {
                    if (negated)
                    {
                        throw new DatalogException("Program is not stratified - predicate " + pred + " has a negative recursion");
                    }
                    return 0;
                }
                if (e.IsNegated())
                {
                    negated = true;
                }
            }

            visited.Add(goal);

            // Step (2): Do the actual depth-first search to compute the strata
            int m = 0;
            foreach (Rule rule in graph)
            {
                if (rule.Head.PredicateWithArity.Equals(pred))
                {
                    foreach (Expr expr in rule.Body)
                    {
                        int x = DepthFirstSearch(expr, graph, visited, level + 1);
                        if (expr.IsNegated())
                        {
                            x++;
                        }
                        if (x > m)
                        {
                            m = x;
                        }
                    }
                }
            }

            visited.RemoveAt(visited.Count - 1);
            return m;
        }

        /* This implements the semi-naive part of the evaluator.
        * For all the rules derive a collection of new facts; Repeat until no new
        * facts can be derived.
        * The semi-naive part is to only use the rules that are affected by newly derived
        * facts in each iteration of the loop.
        */

        private void ExpandStrata(IndexedSet currentFacts, HashSet<Rule> strataRules)
        {
            HashSet<Rule> remainingRules = strataRules;

            // Get the mapping from goals to rules depending on them within the strata
            IDictionary<int, List<Rule>> ruleDependencyGraph = BuildDependentRules(strataRules);

            while (true)
            {
                // Match each rule to the facts
                HashSet<Expr> newFacts = new HashSet<Expr>();
                foreach (Rule rule in remainingRules)
                {
                    newFacts.UnionWith(GetRuleMatches(currentFacts, rule));
                }

                // Repeat until there are no more facts added
                if (newFacts.Count == 0)
                {
                    return;
                }

                // Determine which rules from the strata depend on the newly derived facts
                remainingRules.Clear();
                foreach (Expr predicate in newFacts)
                {
                    if (ruleDependencyGraph.TryGetValue(predicate.Index(), out List<Rule> depRules))
                    {
                        remainingRules.UnionWith(depRules);
                    }
                }

                int prev = currentFacts.Count;
                currentFacts.AddAll(newFacts);

#if DEBUG
                if (currentFacts.Count != prev + newFacts.Count)
                {
                    throw new InvalidOperationException();
                }
#endif
            }
        }

        /* Match the facts in the EDB against a specific rule */

        private HashSet<Expr> GetRuleMatches(IndexedSet facts, Rule rule)
        {
#if DEBUG
            if ((rule.Body.Count == 0))
            {
                // If this happens, you're using the API wrong.
                throw new InvalidOperationException();
            }

            if (rule.Head.GetTerms().Count(x => Universe.IsVariable(x)) == 0)
            {
                // If this happens, you're using the API wrong.
                throw new InvalidOperationException();
            }
#endif

            // Match the rule body to the facts.
            HashSet<Expr> res = new HashSet<Expr>();

            seq.Clear();
            for (int i = 0; i < rule.Body.Count; i++)
            {
                /*
                TODO: optimize ordering
                Expr expr = rule.Body[i];
                if (facts.ContainsIndex(expr) && !expr.IsNegated())
                {
                    seq.Insert(0, i);
                }
                else*/
                {
                    seq.Add(i);
                }
            }

            // PERF: avoid evaluating same rule on same facts. Fact set is add-only, even though we have a delete statement, in practice we never delete facts.
            MatchGoals(rule.Head, rule.Body, seq, 0, facts, new StackMap(), res, null);
            return res;
        }

        private List<int> seq = new List<int>();

        private void MatchGoals(Expr ruleHead, IList<Expr> goals, List<int> seq, int seqIndex, IndexedSet facts, StackMap bindings, HashSet<Expr> res, string[] reusableArray)
        {
            // TODO: semi-duplicated
            Expr goal = goals[seq[seqIndex]];
            bool lastGoal = seqIndex == seq.Count - 1;
            if (goal.IsBuiltIn())
            {
                bool eval = goal.EvalBuiltIn(bindings);
                if ((eval && !goal.IsNegated()) || (!eval && goal.IsNegated()))
                {
                    if (lastGoal)
                    {
                        DeriveRuleFactAndAdd();
                    }
                    else
                    {
                        MatchGoals(ruleHead, goals, seq, seqIndex + 1, facts, bindings, res, reusableArray);
                    }
                }

                return;
            }

            if (!goal.IsNegated())
            {
                // Positive rule: Match each fact to the first goal.
                // If the fact matches: If it is the last/only goal then we can return the bindings
                // as an answer, otherwise we recursively check the remaining goals.
                goal = goal.Substitute(bindings.DictionaryObject());

                foreach (Expr fact in facts.GetIndexed(goal))
                {
                    int stackPointer = bindings.Stack.Count;
                    if (fact.GroundUnifyWith(goal, bindings))
                    {
                        if (lastGoal)
                        {
                            DeriveRuleFactAndAdd();
                        }
                        else
                        {
                            // More goals to match. Recurse with the remaining goals.
                            MatchGoals(ruleHead, goals, seq, seqIndex + 1, facts, bindings, res, reusableArray);
                        }
                    }

                    bindings.RemoveUntil(stackPointer);
                }
            }
            else
            {
                // Negated rule: If you find any fact that matches the goal, then the goal is false.
                // See definition 4.3.2 of [bra2] and section VI-B of [ceri].
                // Substitute the bindings in the rule first.
                // If your rule is `und(X) :- stud(X), not grad(X)` and you're at the `not grad` part, and in the
                // previous goal stud(a) was true, then bindings now contains X:a so we want to search the database
                // for the fact grad(a).
                goal = goal.Substitute(bindings.DictionaryObject());

                foreach (Expr fact in facts.GetIndexed(goal))
                {
                    if (fact.GroundUnifyWith(goal, bindings))
                    {
                        return;
                    }
                }

                // not found
                if (lastGoal)
                {
                    DeriveRuleFactAndAdd();
                }
                else
                {
                    MatchGoals(ruleHead, goals, seq, seqIndex + 1, facts, bindings, res, reusableArray);
                }
            }

            void DeriveRuleFactAndAdd()
            {
                reusableArray = reusableArray ?? new string[ruleHead.Arity];
                Expr derivedFact = ruleHead.Substitute(bindings.DictionaryObject(), reusableArray);
                if (!facts.Contains(derivedFact))
                {
                    res.Add(derivedFact);
                    reusableArray = null;
                }
            }
        }
    }
}