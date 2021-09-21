using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sharpen;
using Sharplog.Statement;

namespace Sharplog
{
    /// <summary>Internal class that encapsulates the parser for the Datalog language.</summary>
    internal class Parser
    {
        /* Parses a Datalog statement.
        * A statement can be:
        * - a fact, like parent(alice, bob).
        * - a rule, like ancestor(A, B) :- ancestor(A, C), parent(C, B).
        * - a query, like ancestor(X, bob)?
        * - a delete clause, like delete parent(alice, bob).
        */
        private static readonly IList<string> validOperators = new List<string> { "=", "!=", "<>", "<", "<=", ">", ">=" };

        /// <exception cref="Sharplog.DatalogException"/>
        internal static Statement.Statement ParseStmt(StreamTokenizer scan)
        {
            List<Expr> goals = new List<Expr>();
            try
            {
                Expr head = ParseExpr(scan);
                if (scan.NextToken() == ':')
                {
                    // We're dealing with a rule
                    if (scan.NextToken() != '-')
                    {
                        throw new DatalogException("[line " + scan.LineNumber + "] Expected ':-'");
                    }
                    IList<Expr> body = new List<Expr>();
                    do
                    {
                        Expr arg = ParseExpr(scan);
                        body.Add(arg);
                    }
                    while (scan.NextToken() == ',');
                    if (scan.ttype != '.')
                    {
                        throw new DatalogException("[line " + scan.LineNumber + "] Expected '.' after rule");
                    }
                    Rule newRule = new Rule(head, body);
                    return new InsertRuleStatement(newRule);
                }
                else if (scan.ttype == '.')
                {
                    // We're dealing with a fact, or a query
                    // It's a fact
                    return new InsertFactStatement(head);
                }
                else
                {
                    // It's a query
                    goals.Clear();
                    goals.Add(head);
                    if (scan.ttype != '.' && scan.ttype != '?' && scan.ttype != ',')
                    {
                        /* You _can_ write facts like `a = 5 .` but I recommend against it; if you do then you *must* have the space between the
                        5 and the '.' otherwise the parser sees it as 5.0 and the error message can be a bit confusing. */
                        throw new DatalogException("[line " + scan.LineNumber + "] Expected one of '.', ',' or '?' after fact/query expression");
                    }
                    while (scan.ttype == ',')
                    {
                        goals.Add(ParseExpr(scan));
                        scan.NextToken();
                    }
                    if (scan.ttype == '?')
                    {
                        return new QueryStatement(goals);
                    }
                    else if (scan.ttype == '~')
                    {
                        return new DeleteStatement(goals);
                    }
                    else
                    {
                        throw new DatalogException("[line " + scan.LineNumber + "] Expected '?' or '~' after query");
                    }
                }
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
            }
        }

        /* parses an expression */

        /// <exception cref="Sharplog.DatalogException"/>
        internal static Expr ParseExpr(StreamTokenizer scan)
        {
            try
            {
                scan.NextToken();
                bool negated = false;
                if (scan.ttype == StreamTokenizer.TT_WORD && scan.StringValue.Equals("not", System.StringComparison.OrdinalIgnoreCase))
                {
                    negated = true;
                    scan.NextToken();
                }
                string lhs = null;
                bool builtInExpected = false;
                if (scan.ttype == StreamTokenizer.TT_WORD)
                {
                    lhs = scan.StringValue;
                }
                else if (scan.ttype == '"' || scan.ttype == '\'')
                {
                    lhs = scan.StringValue;
                    builtInExpected = true;
                }
                else if (scan.ttype == StreamTokenizer.TT_NUMBER)
                {
                    lhs = scan.NumberValue.ToString();
                    builtInExpected = true;
                }
                else
                {
                    throw new DatalogException("[line " + scan.LineNumber + "] Predicate or start of expression expected");
                }
                scan.NextToken();
                if (scan.ttype == StreamTokenizer.TT_WORD || scan.ttype == '=' || scan.ttype == '!' || scan.ttype == '<' || scan.ttype == '>')
                {
                    scan.PushBack();
                    Expr e = ParseBuiltInPredicate(lhs, scan);
                    e.negated = negated;
                    return e;
                }
                if (builtInExpected)
                {
                    // LHS was a number or a quoted string but we didn't get an operator
                    throw new DatalogException("[line " + scan.LineNumber + "] Built-in predicate expected");
                }
                else if (scan.ttype != '(')
                {
                    throw new DatalogException("[line " + scan.LineNumber + "] Expected '(' after predicate or an operator");
                }
                List<string> terms = new List<string>();
                if (scan.NextToken() != ')')
                {
                    scan.PushBack();
                    do
                    {
                        if (scan.NextToken() == StreamTokenizer.TT_WORD)
                        {
                            terms.Add(scan.StringValue);
                        }
                        else if (scan.ttype == '"' || scan.ttype == '\'')
                        {
                            terms.Add("\"" + scan.StringValue);
                        }
                        else if (scan.ttype == StreamTokenizer.TT_NUMBER)
                        {
                            terms.Add(scan.NumberValue.ToString());
                        }
                        else
                        {
                            throw new DatalogException("[line " + scan.LineNumber + "] Expected term in expression");
                        }
                    }
                    while (scan.NextToken() == ',');
                    if (scan.ttype != ')')
                    {
                        throw new DatalogException("[line " + scan.LineNumber + "] Expected ')'");
                    }
                }
                Expr e_1 = new Expr(lhs, terms.ToArray());
                e_1.negated = negated;
                return e_1;
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
            }
        }

        /* Parses one of the built-in predicates, eg X <> Y
        * It is represented internally as a Expr with the operator as the predicate and the
        * operands as its terms, eg. <>(X, Y)
        */

        /// <exception cref="Sharplog.DatalogException"/>
        private static Expr ParseBuiltInPredicate(string lhs, StreamTokenizer scan)
        {
            try
            {
                string @operator;
                scan.NextToken();
                if (scan.ttype == StreamTokenizer.TT_WORD)
                {
                    // At some point I was going to have "eq" and "ne" for string comparisons, but it wasn't a good idea.
                    @operator = scan.StringValue;
                }
                else
                {
                    @operator = char.ToString((char)scan.ttype);
                    scan.NextToken();
                    if (scan.ttype == '=' || scan.ttype == '>')
                    {
                        @operator = @operator + char.ToString((char)scan.ttype);
                    }
                    else
                    {
                        scan.PushBack();
                    }
                }
                if (!validOperators.Contains(@operator))
                {
                    throw new DatalogException("Invalid operator '" + @operator + "'");
                }
                string rhs = null;
                scan.NextToken();
                if (scan.ttype == StreamTokenizer.TT_WORD)
                {
                    rhs = scan.StringValue;
                }
                else if (scan.ttype == '"' || scan.ttype == '\'')
                {
                    rhs = scan.StringValue;
                }
                else if (scan.ttype == StreamTokenizer.TT_NUMBER)
                {
                    rhs = scan.NumberValue.ToString();
                }
                else
                {
                    throw new DatalogException("[line " + scan.LineNumber + "] Right hand side of expression expected");
                }
                return new Expr(@operator, lhs, rhs);
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
            }
        }
    }
}