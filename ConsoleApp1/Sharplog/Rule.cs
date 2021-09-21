using Sharplog.Engine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharplog
{
    /// <summary>Class that represents a Datalog rule.</summary>
    /// <remarks>
    /// Class that represents a Datalog rule.
    /// A rule has a head that is an expression and a body that is a list of expressions.
    /// It takes the form
    /// <c>foo(X, Y) :- bar(X, Y), baz(X), fred(Y)</c>
    /// </remarks>
    /// <seealso cref="Expr"/>
    public class Rule
    {
        private Expr head;

        private IList<Expr> body;

        /// <summary>Constructor that takes an expression as the head of the rule and a list of expressions as the body.</summary>
        /// <remarks>
        /// Constructor that takes an expression as the head of the rule and a list of expressions as the body.
        /// The expressions in the body may be reordered to be able to evaluate rules correctly.
        /// </remarks>
        /// <param name="head">The head of the rule (left hand side)</param>
        /// <param name="body">The list of expressions that make up the body of the rule (right hand side)</param>
        public Rule(Expr head, IList<Expr> body)
        {
            this.head = head;
            this.body = Engine.Engine.ReorderQuery(body);
        }

        /// <summary>Constructor for the fluent API that allows a variable number of expressions in the body.</summary>
        /// <remarks>
        /// Constructor for the fluent API that allows a variable number of expressions in the body.
        /// The expressions in the body may be reordered to be able to evaluate rules correctly.
        /// </remarks>
        /// <param name="head">The head of the rule (left hand side)</param>
        /// <param name="body">The list of expressions that make up the body of the rule (right hand side)</param>
        public Rule(Expr head, params Expr[] body)
            : this(head, body.ToList())
        {
        }

        /// <summary>Checks whether a rule is valid.</summary>
        /// <remarks>
        /// Checks whether a rule is valid.
        /// There are a variety of reasons why a rule may not be valid:
        /// <ul>
        /// <li> Each variable in the head of the rule <i>must</i> appear in the body.
        /// <li> Each variable in the body of a rule should appear at least once in a positive (that is non-negated) expression.
        /// <li> Variables that are used in built-in predicates must appear at least once in a positive expression.
        /// </ul>
        /// </remarks>
        /// <exception cref="DatalogException">if the rule is not valid, with the reason in the message.</exception>
        /// <exception cref="Sharplog.DatalogException"/>
        public void Validate()
        {
            // Check for /safety/: each variable in the body of a rule should appear at least once in a positive expression,
            // to prevent infinite results. E.g. p(X) :- not q(X, Y) is unsafe because there are an infinite number of values
            // for Y that satisfies `not q`. This is a requirement for negation - [gree] contains a nice description.
            // We also leave out variables from the built-in predicates because variables must be bound to be able to compare
            // them, i.e. a rule like `s(A, B) :- r(A,B), A > X` is invalid ('=' is an exception because it can bind variables)
            // You won't be able to tell if the variables have been bound to _numeric_ values until you actually evaluate the
            // expression, though.
            HashSet<string> bodyVariables = new HashSet<string>();
            foreach (Expr clause in GetBody())
            {
                if (clause.IsBuiltIn())
                {
                    if (clause.GetTerms().Length != 2)
                    {
                        throw new DatalogException("Operator " + clause.GetPredicate() + " must have only two operands");
                    }
                    string a = clause.GetTerms()[0];
                    string b = clause.GetTerms()[1];
                    if (clause.GetPredicate().Equals("="))
                    {
                        if (Sharplog.Jatalog.IsVariable(a) && Sharplog.Jatalog.IsVariable(b) && !bodyVariables.Contains(a) && !bodyVariables.Contains(b))
                        {
                            throw new DatalogException("Both variables of '=' are unbound in clause " + a + " = " + b);
                        }
                    }
                    else
                    {
                        if (Sharplog.Jatalog.IsVariable(a) && !bodyVariables.Contains(a))
                        {
                            throw new DatalogException("Unbound variable " + a + " in " + clause);
                        }
                        if (Sharplog.Jatalog.IsVariable(b) && !bodyVariables.Contains(b))
                        {
                            throw new DatalogException("Unbound variable " + b + " in " + clause);
                        }
                    }
                }
                if (clause.IsNegated())
                {
                    foreach (string term in clause.GetTerms())
                    {
                        if (Sharplog.Jatalog.IsVariable(term) && !bodyVariables.Contains(term))
                        {
                            throw new DatalogException("Variable " + term + " of rule " + ToString() + " must appear in at least one positive expression");
                        }
                    }
                }
                else
                {
                    foreach (string term in clause.GetTerms())
                    {
                        if (Sharplog.Jatalog.IsVariable(term))
                        {
                            bodyVariables.Add(term);
                        }
                    }
                }
            }
            // Enforce the rule that variables in the head must appear in the body
            foreach (string term in GetHead().GetTerms())
            {
                if (!Sharplog.Jatalog.IsVariable(term))
                {
                    throw new DatalogException("Constant " + term + " in head of rule " + ToString());
                }
                if (!bodyVariables.Contains(term))
                {
                    throw new DatalogException("Variables " + term + " from the head of rule " + ToString() + " must appear in the body");
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetHead());
            sb.Append(" :- ");
            for (int i = 0; i < GetBody().Count; i++)
            {
                sb.Append(GetBody()[i]);
                if (i < GetBody().Count - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }
        /// <summary>Creates a new Rule with all variables from bindings substituted.</summary>
        /// <remarks>
        /// Creates a new Rule with all variables from bindings substituted.
        /// eg. a Rule
        /// <c>p(X,Y) :- q(X),q(Y),r(X,Y)</c>
        /// with bindings {X:aa}
        /// will result in a new Rule
        /// <c>p(aa,Y) :- q(aa),q(Y),r(aa,Y)</c>
        /// </remarks>
        /// <param name="bindings">The bindings to substitute.</param>
        /// <returns>the Rule with the substituted bindings.</returns>
        public Rule Substitute(StackMap bindings)
        {
            IList<Expr> subsBody = new List<Expr>();
            foreach (Expr e in GetBody())
            {
                subsBody.Add(e.Substitute(bindings.DictionaryObject()));
            }

            return new Rule(this.GetHead().Substitute(bindings.DictionaryObject()), subsBody);
        }

        public Expr GetHead()
        {
            return head;
        }

        public IList<Expr> GetBody()
        {
            return body;
        }
    }
}