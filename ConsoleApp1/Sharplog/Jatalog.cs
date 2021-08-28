using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sharpen;
using Sharplog.Engine;
using Sharplog.Output;

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
    /// <see cref="Rule(Expr, Expr[])"/>
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
    public class Jatalog
    {
        private EdbProvider edbProvider;

        private List<Sharplog.Rule> idb;

        private Sharplog.Engine.Engine engine = new Sharplog.Engine.Engine();

        /// <summary>Default constructor.</summary>
        /// <remarks>
        /// Default constructor.
        /// <p>
        /// Creates a Jatalog instance with an empty IDB and EDB.
        /// </p>
        /// </remarks>
        public Jatalog()
        {
            // Facts
            // Rules
            this.edbProvider = new BasicEdbProvider();
            this.idb = new List<Sharplog.Rule>();
        }

        /// <summary>Parses a string into a statement that can be executed against the database.</summary>
        /// <param name="statement">
        /// The string of the statement to parse.
        /// <ul>
        /// <li> Statements ending with '.'s will insert either rules or facts.
        /// <li> Statements ending with '?' are queries.
        /// <li> Statements ending with '~' are retract statements - they will remove
        /// facts from the database.
        /// </ul>
        /// </param>
        /// <returns>
        /// A Statement object whose
        /// <see cref="Sharplog.Statement.Statement.Execute(Jatalog)">execute</see>
        /// method
        /// can be called against the database at a later stage.
        /// </returns>
        /// <exception cref="DatalogException">
        /// on error, such as inserting invalid facts or rules or
        /// running invalid queries.
        /// </exception>
        /// <seealso cref="Sharplog.Statement.Statement"/>
        /// <exception cref="Sharplog.DatalogException"/>
        public static Sharplog.Statement.Statement PrepareStatement(string statement)
        {
            try
            {
                StreamTokenizer scan = GetTokenizer(new StreamReader(ToStream(statement)));
                return Parser.ParseStmt(scan);
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
            }
        }

        public static Stream ToStream(string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Helper method to create bindings for
        /// <see cref="Sharplog.Statement.Statement.Execute(Jatalog, System.Collections.Generic.IDictionary{K, V})"/>
        /// method.
        /// <p>
        /// For example, call it like
        /// <c>Jatalog.makeBindings("A", "aaa", "Z", "zzz")</c>
        /// to create a
        /// mapping where A maps to the value "aaa" and Z maps to "zzz":
        /// <c>&lt;A = "aaa"; Z = "zzz"&gt;</c>
        /// .
        /// </p>
        /// </summary>
        /// <param name="kvPairs">A list of key-value pairs - there must be an even value of arguments.</param>
        /// <returns>A Map containing the string values of the key-value pairs.</returns>
        /// <exception cref="DatalogException">on error</exception>
        /// <seealso cref="Sharplog.Statement.Statement.Execute(Jatalog, System.Collections.Generic.IDictionary{K, V})"/>
        /// <exception cref="Sharplog.DatalogException"/>
        public static StackMap<string, string> MakeBindings(params object[] kvPairs)
        {
            StackMap<string, string> mapping = new StackMap<string, string>();
            if (kvPairs.Length % 2 != 0)
            {
                throw new DatalogException("kvPairs must be even");
            }
            for (int i = 0; i < kvPairs.Length / 2; i++)
            {
                string k = kvPairs[i * 2].ToString();
                string v = kvPairs[i * 2 + 1].ToString();
                mapping.Add(k, v);
            }
            return mapping;
        }

        /// <summary>Checks whether a term represents a variable.</summary>
        /// <remarks>
        /// Checks whether a term represents a variable.
        /// Variables start with upper-case characters.
        /// </remarks>
        /// <param name="term">The term to test</param>
        /// <returns>true if the term is a variable</returns>
        public static bool IsVariable(string term)
        {
            return System.Char.IsUpper(term[0]);
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
        public virtual List<(Statement.Statement, IDictionary<string, string>)> ExecuteAll(System.IO.StreamReader reader, QueryOutput output)
        {
            try
            {
                StreamTokenizer scan = GetTokenizer(reader);
                // Tracks all query answers
                List<(Statement.Statement, IDictionary<string, string>)> answers = new List<(Statement.Statement, IDictionary<string, string>)>();
                scan.NextToken();
                while (scan.ttype != StreamTokenizer.TT_EOF)
                {
                    scan.PushBack();
                    var res = ExecuteSingleStatement(scan, reader, output);
                    if (res != null)
                    {
                        answers.AddRange(res);
                    }

                    scan.NextToken();
                }
                return answers;
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
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
        public virtual Dictionary<Statement.Statement, List<(Statement.Statement, IDictionary<string, string>)>> ExecuteAll(string statements)
        {
            // It would've been fun to wrap the results in a java.sql.ResultSet, but damn,
            // those are a lot of methods to implement:
            // https://docs.oracle.com/javase/8/docs/api/java/sql/ResultSet.html
            StreamReader reader = new StreamReader(ToStream(statements));
            return GroupByAsDictionary(ExecuteAll(reader, null), x => x.Item1);
        }

        public Dictionary<TKey, List<TSource>> GroupByAsDictionary<TSource, TKey>(IEnumerable<TSource> that, Func<TSource, TKey> groupKeySelector)
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
        public virtual IEnumerable<IDictionary<string, string>> Query(IList<Expr> goals, StackMap<string, string> bindings)
        {
            return engine.Query(this, goals, bindings);
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
        public virtual IEnumerable<IDictionary<string, string>> Query(params Expr[] goals)
        {
            return Query(goals.ToList(), null);
        }

        /// <summary>Validates all the rules and facts in the database.</summary>
        /// <exception cref="DatalogException">If any rules or facts are invalid. The message contains the reason.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public virtual void Validate()
        {
            foreach (Sharplog.Rule rule in idb)
            {
                rule.Validate();
            }
            // Search for negated loops:
            Sharplog.Engine.Engine.ComputeStratification(idb.ToList());
            // Different EdbProvider implementations may have different ideas about how
            // to iterate through the EDB in the most efficient manner. so in the future
            // it may be better to have the edbProvider validate the facts itself.
            foreach (Expr fact in edbProvider.AllFacts())
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
        public virtual Sharplog.Jatalog Rule(Expr head, params Expr[] body)
        {
            Sharplog.Rule newRule = new Sharplog.Rule(head, body);
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
        /// <exception cref="DatalogException">if the rule is invalid.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public virtual Sharplog.Jatalog Rule(Sharplog.Rule newRule)
        {
            newRule.Validate();
            idb.Add(newRule);
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
        public virtual Sharplog.Jatalog Fact(string predicate, params string[] terms)
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
        public virtual Sharplog.Jatalog Fact(Expr newFact)
        {
            if (!newFact.IsGround())
            {
                throw new DatalogException("Facts must be ground: " + newFact);
            }
            if (newFact.IsNegated())
            {
                throw new DatalogException("Facts cannot be negated: " + newFact);
            }
            // You can also match the arity of the fact against existing facts in the EDB,
            // but it's more of a principle than a technical problem; see Jatalog#validate()
            edbProvider.Add(newFact);
            return this;
        }

        /// <summary>Deletes all the facts in the database that matches a specific query</summary>
        /// <param name="goals">The query to which to match the facts.</param>
        /// <returns>true if any facts were deleted.</returns>
        /// <exception cref="DatalogException">on errors encountered during evaluation.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public virtual bool Delete(params Expr[] goals)
        {
            return Delete(goals.ToList(), null);
        }

        /// <summary>Deletes all the facts in the database that matches a specific query</summary>
        /// <param name="goals">The query to which to match the facts.</param>
        /// <param name="bindings">An optional (nullable) mapping of variable names to values.</param>
        /// <returns>true if any facts were deleted.</returns>
        /// <exception cref="DatalogException">on errors encountered during evaluation.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public virtual bool Delete(IList<Expr> goals, StackMap<string, string> bindings)
        {
            IEnumerable<IDictionary<string, string>> answers = Query(goals, bindings);
            IList<Expr> facts = answers.SelectMany((IDictionary<string, string> answer) => goals.Select((Expr goal) => goal.Substitute(answer))).ToList();
            // and substitute the answer on each goal
            return edbProvider.RemoveAll(facts);
        }

        public override string ToString()
        {
            // The output of this method should be parseable again and produce an exact replica of the database
            StringBuilder sb = new StringBuilder("% Facts:\n");
            foreach (Expr fact in edbProvider.AllFacts())
            {
                sb.Append(fact).Append(".\n");
            }
            sb.Append("\n% Rules:\n");
            foreach (Sharplog.Rule rule in idb)
            {
                sb.Append(rule).Append(".\n");
            }
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Sharplog.Jatalog))
            {
                return false;
            }
            Sharplog.Jatalog that = ((Sharplog.Jatalog)obj);
            if (this.idb.Count() != that.idb.Count())
            {
                return false;
            }
            foreach (Sharplog.Rule rule in idb)
            {
                if (!that.idb.Contains(rule))
                {
                    return false;
                }
            }
            IEnumerable<Expr> theseFacts = this.edbProvider.AllFacts();
            IEnumerable<Expr> thoseFacts = that.edbProvider.AllFacts();
            if (theseFacts.Count() != thoseFacts.Count())
            {
                return false;
            }
            foreach (Expr fact in theseFacts)
            {
                if (!thoseFacts.Contains(fact))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Retrieves the EdbProvider</summary>
        /// <returns>
        /// The
        /// <see cref="EdbProvider"/>
        /// </returns>
        public virtual EdbProvider GetEdbProvider()
        {
            return edbProvider;
        }

        /// <summary>Sets the EdbProvider that manages the database.</summary>
        /// <param name="edbProvider">
        /// the
        /// <see cref="EdbProvider"/>
        /// </param>
        public virtual void SetEdbProvider(EdbProvider edbProvider)
        {
            this.edbProvider = edbProvider;
        }

        public virtual IEnumerable<Sharplog.Rule> GetIdb()
        {
            return idb;
        }

        /* Specific tokenizer for our syntax */

        public override int GetHashCode()
        {
            var hashCode = 1357634632;
            hashCode = hashCode * -1521134295 + EqualityComparer<EdbProvider>.Default.GetHashCode(edbProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<Rule>>.Default.GetHashCode(idb);
            return hashCode;
        }

        /// <exception cref="System.IO.IOException"/>
        private static StreamTokenizer GetTokenizer(System.IO.StreamReader reader)
        {
            StreamTokenizer scan = new StreamTokenizer(reader);
            scan.OrdinaryChar('.');
            // '.' looks like a number to StreamTokenizer by default
            scan.CommentChar('%');
            // Prolog-style % comments; slashSlashComments and slashStarComments can stay as well.
            scan.QuoteChar('"');
            scan.QuoteChar('\'');
            // WTF? You can't disable parsing of numbers unless you reset the syntax (http://stackoverflow.com/q/8856750/115589)
            //scan.parseNumbers();
            return scan;
        }

        /* Internal method for executing one and only one statement */

        /// <exception cref="Sharplog.DatalogException"/>
        private List<(Statement.Statement, IDictionary<string, string>)> ExecuteSingleStatement(StreamTokenizer scan, System.IO.StreamReader reader, QueryOutput output)
        {
            Sharplog.Statement.Statement statement = Parser.ParseStmt(scan);
            try
            {
                IEnumerable<IDictionary<string, string>> answers = statement.Execute(this);
                if (answers != null && output != null)
                {
                    output.WriteResult(statement, answers);
                }
                return answers?.Select(x => (statement, x)).ToList();
            }
            catch (DatalogException e)
            {
                throw new DatalogException("[line " + scan.LineNumber + "] Error executing statement", e);
            }
        }
    }
}