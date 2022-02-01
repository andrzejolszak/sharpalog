using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sharplog.Statement;
using Stringes;

namespace Sharplog
{
    public static class TokenListExtensions
    {
        internal static bool EatCurrent(this List<Token<Token>> self, ref int index, Token tokenToEat)
        {
            if (self.Count > index && self[index].ID == tokenToEat)
            {
                index++;
                return true;
            }

            return false;
        }

        internal static bool EatCurrentVal(this List<Token<Token>> self, ref int index, string value)
        {
            if (self.Count > index && self[index].Value == value)
            {
                index++;
                return true;
            }

            return false;
        }
    }

    internal enum Token
    {
        Arrow,
        Dot,
        Minus,
        Plus,
        GreaterThan,
        LessThan,
        Comma,
        Tilde,
        Semicolon,
        Colon,
        Bar,
        At,
        String,
        Slash,
        Backslash,
        ParenOpen,
        ParenClose,
        BracketOpen,
        BracketClose,
        BraceOpen,
        BraceClose,
        Question,
        Bang,
        Equals,
        Deconstruct,
        LineComment,
        MultiLineComment,
        Number,
        Identifier,
        Whitespace,
        OpArrow1,
        OpArrow2,
        EOF
    }

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

        public static readonly Lexer<Token> _lexer = new Lexer<Token>
            {
                // Constant rules
                { "-->", Token.OpArrow1},
                { "=>", Token.OpArrow2},
                {":-", Token.Arrow},
                {".", Token.Dot},
                {"-", Token.Minus},
                {"+", Token.Plus},
                {",", Token.Comma},
                {"~", Token.Tilde},
                {";", Token.Semicolon},
                {":", Token.Colon},
                {"|", Token.Bar},
                {"@", Token.At},
                {">", Token.GreaterThan},
                {"<", Token.LessThan},
                {"/", Token.Slash},
                {"\\", Token.Backslash},
                {"(", Token.ParenOpen},
                {")", Token.ParenClose},
                {"[", Token.BracketOpen},
                {"]", Token.BracketClose},
                {"{", Token.BraceOpen},
                {"}", Token.BraceClose},
                {"?", Token.Question},
                {"!", Token.Bang},
                {"=", Token.Equals},
                {"=..", Token.Deconstruct},

                // Function rule
                {
                    reader =>
                    {
                        return reader.EatWhile(Char.IsDigit);
                    },
                    Token.Number
                },

                {
                    reader =>
                    {
                        // TODO current position in reader, current list of tokens
                        // TODO: return from this method
                        char c = (char)reader.PeekChar();
                        if (char.IsLetter(c) || c == '_')
                        {
                            return reader.EatWhile(x => Char.IsLetterOrDigit(x) || x == '_');
                        }

                        return false;
                    },
                    Token.Identifier
                },

                {
                    reader =>
                    {
                        Chare c = reader.PeekChare();
                        if (c == '%')
                        {
                            return reader.EatWhile(x => c.Line == x.Line);
                        }

                        return false;
                    },
                    Token.LineComment
                },

                {
                    reader =>
                    {
                        bool isMultiComment = reader.Eat("/*");
                        if (isMultiComment)
                        {
                            while (!reader.Eat("*/"))
                            {
                                reader.ReadChare();
                            }

                            return true;
                        }

                        return false;
                    },
                    Token.MultiLineComment
                },

                {
                    reader =>
                    {
                        bool isString = reader.Eat("\"");
                        if (isString)
                        {
                            reader.ReadUntil('"');
                            return true;
                        }

                        return false;
                    },
                    Token.String
                },

                {
                    reader =>
                    {
                        /*bool isString = reader.Eat("\'");
                        if (isString)
                        {
                            reader.ReadUntil('\'');
                            return true;
                        }*/

                        return false;
                    },
                    Token.String
                },

                // Regex rule
                {new Regex(@"\s"), Token.Whitespace}
            }
            .Ignore(Token.Whitespace)
            .Ignore(Token.LineComment)
            .Ignore(Token.MultiLineComment)
            .AddEndToken(Token.EOF);

        /// <exception cref="DatalogException"/>
        internal static Statement.Statement ParseStmt(List<Token<Token>> tokens, ref int currentIndex, bool isAssertQuery)
        {
            List<Expr> goals = new List<Expr>();
            try
            {
                Expr head = ParseExpr(tokens, ref currentIndex);

                if (tokens[currentIndex].ID == Token.Arrow)
                {
                    List<Expr> body = new List<Expr>();
                    do
                    {
                        currentIndex++;
                        Expr arg = ParseExpr(tokens, ref currentIndex);
                        body.Add(arg);
                    }
                    while (tokens[currentIndex].ID == Token.Comma);

                    if (tokens[currentIndex].ID != Token.Dot)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected '.' after rule");
                    }

                    // Get rid of atoms in head: foo(a):-... -> foo(A_$):-V$_a=a,...
                    string[] args = head.GetTerms();
                    for(int i = 0; i < args.Length; i++)
                    {
                        string arg = args[i];
                        if (!Universe.IsVariable(arg))
                        {
                            string newArg = "V$_" + arg.Replace("'", string.Empty);
                            args[i] = newArg;
                            body.Insert(0, new Expr("=", newArg, arg));
                        }
                    }

                    Rule newRule = new Rule(head, body);

                    if (isAssertQuery)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Only queries can be use as asserts.");
                    }

                    return new InsertRuleStatement(newRule);
                }
                else if (tokens[currentIndex].ID == Token.Dot)
                {
                    if (isAssertQuery)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Only queries can be use as asserts.");
                    }

                    // We're dealing with a fact, or a query
                    // It's a fact
                    return new InsertFactStatement(head);
                }
                else
                {
                    // It's a query
                    goals.Clear();
                    goals.Add(head);

                    if (tokens[currentIndex].ID != Token.Dot && tokens[currentIndex].ID != Token.Question && tokens[currentIndex].ID != Token.Comma && tokens[currentIndex].ID != Token.Tilde)
                    {
                        /* You _can_ write facts like `a = 5 .` but I recommend against it; if you do then you *must* have the space between the
                        5 and the '.' otherwise the parser sees it as 5.0 and the error message can be a bit confusing. */
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected one of '.', ',' or '?' after fact/query expression");
                    }

                    while (tokens[currentIndex].ID == Token.Comma)
                    {
                        currentIndex++;
                        goals.Add(ParseExpr(tokens, ref currentIndex));
                    }

                    if (tokens[currentIndex].ID == Token.Question)
                    {
                        return new QueryStatement(goals, isAssertQuery);
                    }
                    else if (tokens[currentIndex].ID == Token.Tilde)
                    {
                        if (isAssertQuery)
                        {
                            throw new DatalogException("[line " + tokens[currentIndex].Line + "] Only queries can be use as asserts.");
                        }

                        return new DeleteStatement(goals, null);
                    }
                    else
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected '?' or '~' after query");
                    }
                }
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
            }
        }
        
        public static bool TryParseUniverseDeclaration(List<Token<Token>> tokens, ref int currentIndex, out string universe)
        {
            universe = null;

            if (tokens[currentIndex].ID != Token.Identifier || tokens[currentIndex].Value != "universe")
            {
                return false;
            }

            currentIndex++;
            if (tokens[currentIndex].ID != Token.Identifier)
            {
                currentIndex-=2;
                return false;
            }

            // Dealing with a universe
            universe = tokens[currentIndex].Value;

            currentIndex++;
            if (tokens[currentIndex].ID != Token.BraceOpen)
            {
                throw new DatalogException("[line " + tokens[currentIndex].Line + "] Universe definition expected");
            }

            return true;
        }

        /// <exception cref="DatalogException"/>
        private static Expr ParseExpr(List<Token<Token>> tokens, ref int currentIndex)
        {
            try
            {
                bool negated = false;
                if (tokens[currentIndex].Value == "not")
                {
                    negated = true;
                    currentIndex++;
                }

                string lhs = null;
                string universe = null;
                lhs_parse:

                bool builtInExpected = false;
                if (tokens[currentIndex].ID == Token.Identifier)
                {
                    lhs = tokens[currentIndex].Value;
                }
                else if (tokens[currentIndex].ID == Token.String)
                {
                    lhs = tokens[currentIndex].Value;
                    builtInExpected = true;
                }
                else if (tokens[currentIndex].ID == Token.Number)
                {
                    lhs = tokens[currentIndex].Value.ToString();
                    builtInExpected = true;
                }
                else
                {
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Predicate or start of expression expected");
                }

                currentIndex++;
                if (tokens[currentIndex].ID == Token.Identifier || tokens[currentIndex].ID == Token.Equals || tokens[currentIndex].ID == Token.Bang || tokens[currentIndex].ID == Token.LessThan || tokens[currentIndex].ID == Token.GreaterThan)
                {
                    Expr e = ParseBuiltInPredicate(lhs, tokens, ref currentIndex);
                    e.negated = negated;
                    return e;
                }

                if (builtInExpected)
                {
                    // LHS was a number or a quoted string but we didn't get an operator
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Built-in predicate expected");
                }
                else if (tokens[currentIndex].ID == Token.Dot)
                {
                    throw new System.Exception("universe.rule syntax is not yet supported!");

                    if (universe != null)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Wrong universe reference syntax");
                    }

                    universe = lhs;
                    lhs = null;
                    currentIndex++;
                    goto lhs_parse;
                }
                else if (tokens[currentIndex].ID != Token.ParenOpen)
                {
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected '(' after predicate or an operator");
                }

                List<string> terms = new List<string>();
                if (tokens[currentIndex + 1].ID != Token.ParenClose)
                {
                    do
                    {
                        currentIndex++;
                        if (tokens[currentIndex].ID == Token.Identifier)
                        {
                            terms.Add(tokens[currentIndex].Value);
                        }
                        else if (tokens[currentIndex].ID == Token.String)
                        {
                            terms.Add("\"" + tokens[currentIndex].Value);
                        }
                        else if (tokens[currentIndex].ID == Token.Number)
                        {
                            terms.Add(tokens[currentIndex].Value.ToString());
                        }
                        else
                        {
                            throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected term in expression");
                        }

                        currentIndex++;
                    }
                    while (tokens[currentIndex].ID == Token.Comma);

                    if (tokens[currentIndex].ID != Token.ParenClose)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected ')'");
                    }

                    currentIndex++;
                }
                else
                {
                    currentIndex+=2;
                }


                Expr e_1 = new Expr(lhs, terms.ToArray());
                e_1.UniverseReference = universe;
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
        private static Expr ParseBuiltInPredicate(string lhs, List<Token<Token>> tokens, ref int currentIndex)
        {
            try
            {
                string @operator;
                if (tokens[currentIndex].ID == Token.Identifier)
                {
                    // At some point I was going to have "eq" and "ne" for string comparisons, but it wasn't a good idea.
                    @operator = tokens[currentIndex].Value;
                }
                else
                {
                    @operator = tokens[currentIndex].Value;
                    currentIndex++;
                    if (tokens[currentIndex].ID == Token.Equals || tokens[currentIndex].ID == Token.GreaterThan)
                    {
                        @operator = @operator + tokens[currentIndex].Value;
                    }
                    else
                    {
                        currentIndex--;
                    }
                }
                if (!validOperators.Contains(@operator))
                {
                    throw new DatalogException("Invalid operator '" + @operator + "'");
                }

                string rhs = null;
                currentIndex++;
                if (tokens[currentIndex].ID == Token.Identifier)
                {
                    rhs = tokens[currentIndex].Value;
                }
                else if (tokens[currentIndex].ID == Token.String)
                {
                    rhs = tokens[currentIndex].Value;
                }
                else if (tokens[currentIndex].ID == Token.Number)
                {
                    rhs = tokens[currentIndex].Value;
                }
                else
                {
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Right hand side of expression expected");
                }

                currentIndex++;

                return new Expr(@operator, lhs, rhs);
            }
            catch (IOException e)
            {
                throw new DatalogException(e);
            }
        }
    }
}