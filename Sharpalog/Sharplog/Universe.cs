using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Sharplog.Engine;
using Sharplog.Statement;
using Stringes;

namespace Sharplog
{
    /// <summary>Main entry-point for the Jatalog engine.</summary>
    /// <remarks>
    /// Main entry-point for the Jatalog engine.
    /// <p>
    /// It consists of several aspects:
    /// </p><ul>
    /// <li> A database, storing the facts and rules.
    /// <li> A parser, for reading and executing statements in the Datalog language.
    /// <li> An evaluation engine, which executes Datalog queries.
    /// <li> A fluent API for accessing and querying the Datalog database programmatically from Java programs.
    /// </ul>
    /// <h3>The Database</h3>
    /// <ul>
    /// <li> The facts, called the <i>Extensional Database</i> (EDB) which is stored as a Collection of <i>ground literal</i>
    /// <see cref="Expr"/>
    /// objects.
    /// <p>The methods
    /// <see cref="Fact(Expr)"/>
    /// and
    /// <see cref="Fact(string, string[])"/>
    /// are used to add facts to the database.</p>
    /// <li> The rules, called the <i>Intensional Database</i> (IDB) which is stored as a Collection of
    /// <see cref="Rule"/>
    /// objects.
    /// <p>The methods
    /// <see cref="Rule(Rule)"/>
    /// and
    /// <see cref="RuleTest(Expr, Expr[])"/>
    /// are used to add rules to the database.</p>
    /// </ul>
    /// <h3>The Parser</h3>
    /// <p>
    /// <see cref="ExecuteAll(System.IO.StreamReader, Sharplog.Output.QueryOutput)"/>
    /// uses a
    /// <see cref="System.IO.StreamReader"/>
    /// to read a series of Datalog statements from a file or a String
    /// and executes them.
    /// </p><p>
    /// Statements can insert facts or rules in the database or execute queries against the database.
    /// </p><p>
    /// <see cref="ExecuteAll(string)"/>
    /// is a shorthand wrapper that can be used with the fluent API.
    /// </p>
    /// <h3>The Evaluation Engine</h3>
    /// Jatalog's evaluation engine is bottom-up, semi-naive with stratified negation.
    /// <p>
    /// <i>Bottom-up</i> means that the evaluator will start with all the known facts in the EDB and use the rules to derive new facts
    /// and repeat this process until no more new facts can be derived. It will then match all of the facts to the goal of the query
    /// to determine the answer
    /// (The alternative is <i>top-down</i> where the evaluator starts with a series of goals and use the rules and facts in the
    /// database to prove the goal).
    /// </p><p>
    /// <i>Semi-naive</i> is an optimization wherein the evaluator will only consider a subset of the rules that may be affected
    /// by facts derived during the previous iteration rather than all of the rules.
    /// </p><p>
    /// <i>Stratified negation</i> arranges the order in which rules are evaluated in such a way that negated goals "makes sense". Consider,
    /// for example, the rule
    /// <c>p(X) :- q(X), not r(X).</c>
    /// : All the
    /// <c>r(X)</c>
    /// facts must be derived first before the
    /// <c>p(X)</c>
    /// facts can be derived. If the rules are evaluated in the wrong order then the evaluator may derive a fact
    /// <c>p(a)</c>
    /// in one
    /// iteration and then derive
    /// <c>r(a)</c>
    /// in a future iteration which will contradict each other.
    /// </p><p>
    /// Stratification also puts additional constraints on the usage of negated expressions in Jatalog, which the engine checks for.
    /// </p><p>
    /// In addition Jatalog implements some built-in predicates: equals "=", not equals "&lt;&gt;", greater than "&gt;", greater or
    /// equals "&gt;=", less than "&lt;" and less or equals "&lt;=".
    /// </p>
    /// <h3>The Fluent API</h3>
    /// Several methods exist to make it easy to use Jatalog from a Java program without invoking the parser.
    /// <hr>
    /// <i>I tried to stick to [ceri]'s definitions, but what they call literals ended up being called <b>expressions</b> in Jatalog. See
    /// <see cref="Expr"/>
    /// </i>
    /// </remarks>
    public class Universe
    {
        private readonly IEngine _engine;

        private readonly SignatureIndexedFactSet _currentExpansionCacheFacts = new SignatureIndexedFactSet();

        private readonly HashSet<Expr> _currentExpansionCacheGoals = new HashSet<Expr>();

        /// <summary>Default constructor.</summary>
        /// <remarks>
        /// Default constructor.
        /// <p>
        /// Creates a Jatalog instance with an empty IDB and EDB.
        /// </p>
        /// </remarks>
        public Universe(bool bottomUpEvaluation = true, string name = null, IEngine engineInstance = null, SignatureIndexedFactSet edb = null, IDictionary<string, HashSet<Rule>> idb = null)
        {
            this.Edb = edb ?? new SignatureIndexedFactSet();
            this.Idb = idb ?? new Dictionary<string, HashSet<Rule>>();
            this.Name = name;

            /*
            TODO: Top-down evaluation just an experiment for now
            if (bottomUpEvaluation)
            {
                engine = new BottomUpEngine();
            }
            else
            {
                engine = new TopDownEngine();
            }*/

            this._engine = engineInstance ?? new BottomUpEngine();
        }

        public string Name { get; }

        public long Version { get; set; }

        public long CurrentFactExpansionCacheSize => this._currentExpansionCacheFacts.Count;

        public SignatureIndexedFactSet Edb { get; }

        public IDictionary<string, HashSet<Rule>> Idb { get; }

        public IEnumerable<Expr> GetFacts(Expr predicate) => Edb.GetIndexed(predicate);

        /// <summary>Checks whether a term represents a variable.</summary>
        /// <remarks>
        /// Checks whether a term represents a variable.
        /// Variables start with upper-case characters.
        /// </remarks>
        /// <param name="term">The term to test</param>
        /// <returns>true if the term is a variable</returns>
        public static bool IsVariable(string term)
        {
            byte c = (byte)term[0];
            byte upperCaseUnderscoreMask = 0b00100000;
            return (c & upperCaseUnderscoreMask) == 0;
        }

        /// <summary>
        /// Executes all the statements in a file/string or another object wrapped by a
        /// <see cref="System.IO.StreamReader"/>
        /// .
        /// <p>
        /// An optional
        /// <see cref="Sharplog.Output.QueryOutput"/>
        /// object can be supplied as a parameter to output the results of multiple queries.
        /// </p><p>
        /// This is how to interpret the returned
        /// <c>Collection&lt;Map&lt;String, String&gt;&gt;</c>
        /// , assuming you store it in a variable
        /// called
        /// <c>answers</c>
        /// :
        /// </p>
        /// <ul>
        /// <li> If
        /// <c>answers</c>
        /// is
        /// <see langword="null"/>
        /// , the statement didn't produce any results; i.e. it was a fact or a rule, not a query.
        /// <li> If
        /// <c>answers</c>
        /// is empty, then it was a query that doesn't have any answers, so the output is "No."
        /// <li> If
        /// <c>answers</c>
        /// is a list of empty maps, then it was the type of query that only wanted a yes/no
        /// answer, like
        /// <c>siblings(alice,bob)?</c>
        /// and the answer is "Yes."
        /// <li> Otherwise
        /// <c>answers</c>
        /// is a list of all bindings that satisfy the query.
        /// </ul>
        /// </summary>
        /// <param name="reader">The reader from which the statements are read.</param>
        /// <param name="output">
        /// The object through which output should be written. Can be
        /// <see langword="null"/>
        /// in which case no output will be written.
        /// </param>
        /// <returns>The answer of the last statement in the file, as a Collection of variable mappings.</returns>
        /// <exception cref="DatalogException">on syntax and I/O errors encountered while executing.</exception>
        /// <seealso cref="Sharplog.Output.QueryOutput"/>
        /// <exception cref="Sharplog.DatalogException"/>
        public List<(Statement.Statement, IDictionary<string, string>)> ExecuteAll2(string stream, out List<Token<Token>> tokens, bool parseOnly = false)
        {
            int i = 0;
            tokens = null;

            try
            {
                // Tracks all query answers
                List<(Statement.Statement, IDictionary<string, string>)> answers = new List<(Statement.Statement, IDictionary<string, string>)>();
                Dictionary<string, Universe> universes = new Dictionary<string, Universe>();
                Universe currentUniverse = this;
                string? currentObjectId = null;

                tokens = Parser._lexer
                    .Tokenize(stream)
                    .Where(x => x.ID != Token.EOF)
                    .Select(x => x.ID == Token.Identifier && x.Value.StartsWith("'") ? new Token<Token>(Token.Identifier, x.Trim('\'')) : x)
                    .ToList();

                int prevIndex = -1;
                while (i < tokens.Count)
                {
                    if (i <= prevIndex)
                    {
                        throw new InvalidOperationException("Parsing progress stalled at index " + i);
                    }

                    prevIndex = i;

                    if (Parser.TryParseUniverseDeclaration(tokens, ref i, out string universe))
                    {
                        if (currentUniverse != null && currentUniverse != this)
                        {
                            throw new DatalogException("[line " + tokens[i].Line + "] Cannot nest universes");
                        }

                        if (currentObjectId is not null)
                        {
                            throw new DatalogException("[line " + tokens[i].Line + "] Cannot nest universes inside of objects");
                        }

                        currentUniverse = new Universe(name: universe);
                        universes.Add(universe, currentUniverse);
                        continue;
                    }

                    if (Parser.TryParseObjectDeclaration(tokens, ref i, out string objectId))
                    {
                        if (currentObjectId != null)
                        {
                            throw new DatalogException("[line " + tokens[i].Line + "] Nesting objects currently not supported");
                        }

                        if (objectId == "gen_guid")
                        {
                            objectId = "id_" + Guid.NewGuid().ToString().Replace("-", "_");
                        }

                        currentObjectId = objectId;
                        Expr objectFact = new Expr("object", currentObjectId);
                        this.Fact(objectFact);

                        continue;
                    }

                    if (tokens.TryEat(ref i, Token.BraceClose))
                    {
                        if (currentObjectId is not null)
                        {
                            currentObjectId = null;
                        }
                        else if (currentUniverse != this)
                        {
                            currentUniverse = this;
                        }
                        else
                        {
                            throw new DatalogException("[line " + tokens[i].Line + "] Invalid closing brace");
                        }

                        continue;
                    }

                    bool isAssert = false;
                    if (tokens.TryEatSequence(ref i, "assert", Token.Colon))
                    {
                        isAssert = true;
                    }
                    else if (tokens.TryEatSequence(ref i, "import", Token.Identifier))
                    {
                        if (!universes.TryGetValue(tokens[i - 1].Value, out Universe imported))
                        {
                            throw new DatalogException("[line " + tokens[i - 1].Line + "] Undefined universe " + tokens[i - 1].Value);
                        }

                        currentUniverse.Edb.AddAll(imported.Edb.All);
                        foreach (var r in imported.Idb.SelectMany(x => x.Value))
                        {
                            currentUniverse.Rule(r);
                        }

                        if (!tokens.TryEat(ref i, Token.Dot))
                        {
                            throw new DatalogException("[line " + tokens[i].Line + "] Wrong syntax");
                        }

                        continue;
                    }
                    else if (tokens.TryEat(ref i, Token.LineComment) || tokens.TryEat(ref i, Token.MultiLineComment))
                    {
                        continue;
                    }

                    int statementLine = i;
                    Statement.Statement statement = Parser.ParseStmt(tokens, ref i, isAssert);

                    if (currentObjectId is not null && (statement is InsertRuleStatement || statement is QueryStatement || statement is DeleteStatement))
                    {
                        throw new DatalogException("[line " + tokens[i].Line + "] Only insert-fact statements can be used within an object context");
                    }

                    if (currentObjectId is not null && statement is InsertFactStatement asInsertFact)
                    {
                        asInsertFact.PrependObjectId(currentObjectId);
                    }

                    if (!parseOnly)
                    {
                        var res = ExecuteSingleStatement(currentUniverse, statement, statementLine);
                        if (res != null)
                        {
                            answers.AddRange(res);
                        }
                    }
                }

                return answers;
            }
            catch (Exception e)
            {
                // TODO: assume current line broken, and collect all exceptions instead of returning just the first one
                throw new DatalogException(e) { TokenIndex = i, Tokens = tokens };
            }
        }

        /// <summary>Executes the Datalog statements in a string.</summary>
        /// <param name="statements">the statements to execute as a string.</param>
        /// <returns>
        /// The answer of the string, as a Collection of variable mappings.
        /// See
        /// <see cref="ExecuteAll(System.IO.StreamReader, Sharplog.Output.QueryOutput)"/>
        /// for details on how to interpret the result.
        /// </returns>
        /// <exception cref="DatalogException">on syntax errors encountered while executing.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public Dictionary<Statement.Statement, List<(Statement.Statement, IDictionary<string, string>)>> ExecuteAll(string statements, bool parseOnly = false)
        {
            return GroupByAsDictionary(ExecuteAll2(statements, out var _,  parseOnly), x => x.Item1);
        }

        public static Dictionary<TKey, List<TSource>> GroupByAsDictionary<TSource, TKey>(IEnumerable<TSource> that, Func<TSource, TKey> groupKeySelector)
        {
            IEnumerable<IGrouping<TKey, TSource>> groups = that.GroupBy(groupKeySelector);
            return groups.ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>Executes a query with the specified goals against the database.</summary>
        /// <param name="goals">The list of goals of the query.</param>
        /// <param name="bindings">An optional (nullable) mapping of variable names to values.</param>
        /// <returns>
        /// The answer of the last statement in the file, as a Collection of variable mappings.
        /// See
        /// <see cref="Sharplog.Output.OutputUtils.AnswersToString(System.Collections.Generic.IEnumerable{E})"/>
        /// for details on how to interpret the result.
        /// </returns>
        /// <exception cref="DatalogException">on syntax errors encountered while executing.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public List<IDictionary<string, string>> Query(List<Expr> goals)
        {
            if (goals.Count == 0)
            {
                return new List<IDictionary<string, string>>(0);
            }

            // TODO: need to treat foo(A, B) and foo(X, Y) as the same goal!
            // TODO: Could do a lookup in existing expanded fact cache first, e.g. for query foo(banan, X) that was maybe not seen as a goal, but is an expanded fact
            List<Expr> nonCachedGoals = goals.Where(x => !this._currentExpansionCacheGoals.Contains(x)).ToList();

            if (nonCachedGoals.Count > 0)
            {
                List<Expr> orderedNonCacheGoals = _engine.ReorderQuery(nonCachedGoals);
                SignatureIndexedFactSet factsForDownstreamPredicates = _engine.ExpandDatabase(this, orderedNonCacheGoals);

                this._currentExpansionCacheFacts.AddAll(factsForDownstreamPredicates.All);
                this._currentExpansionCacheGoals.UnionWith(nonCachedGoals);
            }

            // Now match the expanded database to the goals
            List<Expr> orderedGoals = _engine.ReorderQuery(goals);
            return _engine.MatchGoals(orderedGoals, 0, this._currentExpansionCacheFacts, new VariableBindingStackMap());
        }

        /// <summary>Executes a query with the specified goals against the database.</summary>
        /// <remarks>
        /// Executes a query with the specified goals against the database. This is
        /// part of the fluent API.
        /// </remarks>
        /// <param name="goals">The goals of the query.</param>
        /// <returns>
        /// The answer of the last statement in the file, as a Collection of
        /// variable mappings. See
        /// <see cref="ExecuteAll(System.IO.StreamReader, Sharplog.Output.QueryOutput)"/>
        /// for
        /// details on how to interpret the result.
        /// </returns>
        /// <exception cref="DatalogException">on syntax errors encountered while executing.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public IEnumerable<IDictionary<string, string>> Query(params Expr[] goals)
        {
            return Query(goals.ToList());
        }

        /// <summary>Validates all the rules and facts in the database.</summary>
        /// <exception cref="DatalogException">If any rules or facts are invalid. The message contains the reason.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public void ValidateTest()
        {
            foreach (Rule rule in Idb.SelectMany(x => x.Value))
            {
                rule.Validate();
            }

            // Different EdbProvider implementations may have different ideas about how
            // to iterate through the EDB in the most efficient manner. so in the future
            // it may be better to have the edbProvider validate the facts itself.
            foreach (Expr fact in this.Edb.All)
            {
                fact.ValidFact();
            }
        }

        // Methods for the fluent interface
        /// <summary>
        /// Adds a new
        /// <see cref="Rule"/>
        /// to the IDB database.
        /// This is part of the fluent API.
        /// </summary>
        /// <param name="head">The head of the rule</param>
        /// <param name="body">The expressions that make up the body of the rule.</param>
        /// <returns>
        ///
        /// <c>this</c>
        /// so that methods can be chained.
        /// </returns>
        /// <exception cref="DatalogException">if the rule is invalid.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public Universe RuleTest(Expr head, params Expr[] body)
        {
            Rule newRule = new Rule(head, body.ToList());
            return Rule(newRule);
        }

        /// <summary>Adds a new rule to the IDB database.</summary>
        /// <remarks>
        /// Adds a new rule to the IDB database.
        /// This is part of the fluent API.
        /// </remarks>
        /// <param name="newRule">the rule to add.</param>
        /// <returns>
        ///
        /// <c>this</c>
        /// so that methods can be chained.
        /// </returns>
        public Universe Rule(Rule newRule)
        {
            this._engine.TransformNewRule(newRule);
            newRule.Validate();

            if (!Idb.TryGetValue(newRule.Head.PredicateWithArity, out HashSet<Rule> rules))
            {
                // TODO: it's possible to add multiple copies of same rule
                rules = new HashSet<Rule>();
                Idb.Add(newRule.Head.PredicateWithArity, rules);
            }

            rules.Add(newRule);
            
            InvalidateCache();

            return this;
        }

        /// <summary>Adds a new fact to the EDB database.</summary>
        /// <remarks>
        /// Adds a new fact to the EDB database.
        /// This is part of the fluent API.
        /// </remarks>
        /// <param name="predicate">The predicate of the fact.</param>
        /// <param name="terms">the terms of the fact.</param>
        /// <returns>
        ///
        /// <c>this</c>
        /// so that methods can be chained.
        /// </returns>
        /// <exception cref="DatalogException">
        /// if the fact is invalid. Facts must be
        /// <see cref="Expr.IsGround()">ground</see>
        /// and
        /// cannot be
        /// <see cref="Expr.IsNegated()">negated</see>
        /// </exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public Universe Fact(string predicate, params string[] terms)
        {
            return Fact(new Expr(predicate, terms));
        }

        /// <summary>Adds a new fact to the EDB database.</summary>
        /// <remarks>
        /// Adds a new fact to the EDB database.
        /// This is part of the fluent API.
        /// </remarks>
        /// <param name="newFact">The fact to add.</param>
        /// <returns>
        ///
        /// <c>this</c>
        /// so that methods can be chained.
        /// </returns>
        /// <exception cref="DatalogException">
        /// if the fact is invalid. Facts must be
        /// <see cref="Expr.IsGround()">ground</see>
        /// and
        /// cannot be
        /// <see cref="Expr.IsNegated()">negated</see>
        /// </exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public Universe Fact(Expr newFact)
        {
            if (!newFact.IsGround())
            {
                throw new DatalogException("Facts must be ground: " + newFact);
            }
            if (newFact.Negated)
            {
                throw new DatalogException("Facts cannot be negated: " + newFact);
            }

            // You can also match the arity of the fact against existing facts in the EDB,
            // but it's more of a principle than a technical problem; see Jatalog#validate()
            this.Edb.Add(newFact);

            InvalidateCache();

            return this;
        }

        private void InvalidateCache()
        {
            this.Version++;
            this._currentExpansionCacheGoals.Clear();
            this._currentExpansionCacheFacts.Clear();
        }

        /// <summary>Deletes all the facts in the database that matches a specific query</summary>
        /// <param name="goals">The query to which to match the facts.</param>
        /// <returns>true if any facts were deleted.</returns>
        /// <exception cref="DatalogException">on errors encountered during evaluation.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public bool Delete(params Expr[] goals)
        {
            return Delete(goals.ToList());
        }

        /// <summary>Deletes all the facts in the database that matches a specific query</summary>
        /// <param name="goals">The query to which to match the facts.</param>
        /// <param name="bindings">An optional (nullable) mapping of variable names to values.</param>
        /// <returns>true if any facts were deleted.</returns>
        /// <exception cref="DatalogException">on errors encountered during evaluation.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public bool Delete(List<Expr> goals)
        {
            List<IDictionary<string, string>> answers = Query(goals);
            List<Expr> facts = new List<Expr>(answers.Count * goals.Count);
            foreach (IDictionary<string, string> answer in answers)
            {
                foreach (Expr goal in goals)
                {
                    Expr derivedFact = goal.Substitute(answer);
                    facts.Add(derivedFact);
                }
            }

            InvalidateCache();

            // and substitute the answer on each goal
            return this.Edb.RemoveAll(facts);
        }

        public bool TryGetFromIdb(string predicate, out HashSet<Rule> rules)
        {
            return this.Idb.TryGetValue(predicate, out rules);
        }

        /* Internal method for executing one and only one statement */

        /// <exception cref="Sharplog.DatalogException"/>
        private static List<(Statement.Statement, IDictionary<string, string>)> ExecuteSingleStatement(Universe universe, Statement.Statement statement, int line)
        {
            try
            {
                if (statement is QueryStatement asQuery && asQuery.IsAssert)
                {
                    IEnumerable<IDictionary<string, string>> aaa = statement.Execute(universe);
                    if (!aaa.Any())
                    {
                        throw new DatalogException("Assertion failed");
                    }

                    return null;
                }

                IEnumerable<IDictionary<string, string>> answers = statement.Execute(universe);
                return answers?.Select(x => (statement, x)).ToList();
            }
            catch (DatalogException e)
            {
                throw new DatalogException("[line " + line + "] Error executing statement: " + e.Message, e);
            }
        }
    }
}