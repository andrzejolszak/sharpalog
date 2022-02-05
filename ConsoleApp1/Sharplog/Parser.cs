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
        internal static bool TryEat(this List<Token<Token>> self, ref int index, Token tokenToEat)
        {
            if (self.Count > index && self[index].ID == tokenToEat)
            {
                index++;
                return true;
            }

            return false;
        }

        internal static bool TryEat(this List<Token<Token>> self, ref int index, string value)
        {
            if (self.Count > index && self[index].Value == value)
            {
                index++;
                return true;
            }

            return false;
        }

        internal static bool TryEatSequence(this List<Token<Token>> self, ref int index, params object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                object val = values[i];
                if (val is string asString && !(self.Count > (index+i) && self[index+i].Value == asString))
                {
                    return false;
                }
                else if (val is Token asToken && !(self.Count > (index + i) && self[index + i].ID == asToken))
                {
                    return false;
                }
                else if (!(val is string || val is Token))
                {
                    return false;
                }
            }

            index += values.Length;
            return true;
        }

        internal static bool TryEatOneOf(this List<Token<Token>> self, ref int index, params object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                object val = values[i];
                if (val is string asString && self.Count > index && self[index].Value == asString)
                {
                    index++;
                    return true;
                }
                else if (val is Token asToken && self.Count > index && self[index].ID == asToken)
                {
                    index++;
                    return true;
                }
            }

            return false;
        }

        internal static bool CanEatOneOf(this List<Token<Token>> self, ref int index, params object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                object val = values[i];
                if (val is string asString && self.Count > index && self[index].Value == asString)
                {
                    return true;
                }
                else if (val is Token asToken && self.Count > index && self[index].ID == asToken)
                {
                    return true;
                }
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
        GreaterThanEqual,
        LessThanEqual,
        Comma,
        Tilde,
        Colon,
        Slash,
        Backslash,
        ParenOpen,
        ParenClose,
        BraceOpen,
        BraceClose,
        Question,
        Bang,
        Equals,
        LineComment,
        MultiLineComment,
        Number,
        Identifier,
        Whitespace,
        DifferentV1,
        DifferentV2,
        EOF
    }

    /// <summary>Internal class that encapsulates the parser for the Datalog language.</summary>
    internal class Parser
    {
        /// <summary>
        /// Caution: this has to remain an array, and ordering of elements matters!
        /// </summary>
        private static readonly string[] validOperators = new []{ "=", "!=", "<>", "<=", "<", ">=", ">" };

        public static readonly Lexer<Token> _lexer = new Lexer<Token>
            {
                // Constant rules
                {"<>", Token.DifferentV1},
                {"!=", Token.DifferentV2},
                {":-", Token.Arrow},
                {".", Token.Dot},
                {"-", Token.Minus},
                {"+", Token.Plus},
                {",", Token.Comma},
                {"~", Token.Tilde},
                {":", Token.Colon},
                {">=", Token.GreaterThanEqual},
                {"<=", Token.LessThanEqual},
                {">", Token.GreaterThan},
                {"<", Token.LessThan},
                {"/", Token.Slash},
                {"\\", Token.Backslash},
                {"(", Token.ParenOpen},
                {")", Token.ParenClose},
                {"{", Token.BraceOpen},
                {"}", Token.BraceClose},
                {"?", Token.Question},
                {"!", Token.Bang},
                {"=", Token.Equals},

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
                       /* bool isQuotedIdentifier = reader.Eat("'");
                        if (isQuotedIdentifier)
                        {
                            while (!reader.Eat("'"))
                            {
                                reader.ReadChare();
                            }

                            return true;
                        }
                       */
                        return false;
                    },
                    Token.Identifier
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

                if (tokens.TryEat(ref currentIndex, Token.Arrow))
                {
                    List<Expr> body = new List<Expr>();
                    do
                    {
                        Expr arg = ParseExpr(tokens, ref currentIndex);
                        body.Add(arg);
                    }
                    while (tokens.TryEat(ref currentIndex, Token.Comma));

                    if (!tokens.TryEat(ref currentIndex, Token.Dot))
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
                else if (tokens.TryEat(ref currentIndex, Token.Dot))
                {
                    if (isAssertQuery)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex-1].Line + "] Only queries can be use as asserts.");
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

                    if (!tokens.CanEatOneOf(ref currentIndex, Token.Dot, Token.Question, Token.Comma, Token.Tilde))
                    {
                        /* You _can_ write facts like `a = 5 .` but I recommend against it; if you do then you *must* have the space between the
                        5 and the '.' otherwise the parser sees it as 5.0 and the error message can be a bit confusing. */
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected one of '.', ',' or '?' after fact/query expression");
                    }

                    while (tokens.TryEat(ref currentIndex, Token.Comma))
                    {
                        goals.Add(ParseExpr(tokens, ref currentIndex));
                    }

                    if (tokens.TryEat(ref currentIndex, Token.Question))
                    {
                        return new QueryStatement(goals, isAssertQuery);
                    }
                    else if (tokens.TryEat(ref currentIndex, Token.Tilde))
                    {
                        if (isAssertQuery)
                        {
                            throw new DatalogException("[line " + tokens[currentIndex-1].Line + "] Only queries can be use as asserts.");
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

            if (!tokens.TryEatSequence(ref currentIndex, "universe", Token.Identifier))
            {
                return false;
            }

            // Dealing with a universe
            universe = tokens[currentIndex-1].Value;

            if (!tokens.TryEat(ref currentIndex, Token.BraceOpen))
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
                if (tokens.TryEat(ref currentIndex, "not"))
                {
                    negated = true;
                }

                string lhs = null;
                string universe = null;

                lhs_parse:

                bool builtInExpected = false;
                if (tokens.TryEat(ref currentIndex, Token.Identifier))
                {
                    lhs = tokens[currentIndex-1].Value;
                }
                else if (tokens.TryEat(ref currentIndex, Token.Number))
                {
                    lhs = tokens[currentIndex-1].Value.ToString();
                    builtInExpected = true;
                }
                else
                {
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Predicate or start of expression expected");
                }

                if (tokens.TryEatOneOf(ref currentIndex, validOperators))
                {
                    string @operator = tokens[currentIndex - 1].Value;

                    string rhs = null;
                    if (tokens.TryEat(ref currentIndex, Token.Identifier))
                    {
                        rhs = tokens[currentIndex - 1].Value;
                    }
                    else if (tokens.TryEat(ref currentIndex, Token.Number))
                    {
                        rhs = tokens[currentIndex - 1].Value;
                    }
                    else
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Right hand side of expression expected for operator " + @operator);
                    }

                    return new Expr(@operator, lhs, rhs) { negated = negated };
                }

                if (builtInExpected)
                {
                    // LHS was a number or a quoted string but we didn't get an operator
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Built-in predicate expected");
                }
                else if (tokens.TryEat(ref currentIndex, Token.Dot))
                {
                    throw new Exception("universe.rule syntax is not yet supported!");

                    /*if (universe != null)
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Wrong universe reference syntax");
                    }

                    universe = lhs;
                    lhs = null;
                    currentIndex++;
                    goto lhs_parse;*/
                }
                else if (!tokens.TryEat(ref currentIndex, Token.ParenOpen))
                {
                    throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected '(' after predicate or an operator");
                }

                List<string> terms = new List<string>();
                if (!tokens.TryEat(ref currentIndex, Token.ParenClose))
                {
                    do
                    {
                        if (tokens.TryEat(ref currentIndex, Token.Identifier))
                        {
                            terms.Add(tokens[currentIndex-1].Value);
                        }
                        else if (tokens.TryEat(ref currentIndex, Token.Number))
                        {
                            terms.Add(tokens[currentIndex-1].Value.ToString());
                        }
                        else
                        {
                            throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected term in expression");
                        }
                    }
                    while (tokens.TryEat(ref currentIndex, Token.Comma));

                    if (!tokens.TryEat(ref currentIndex, Token.ParenClose))
                    {
                        throw new DatalogException("[line " + tokens[currentIndex].Line + "] Expected ')'");
                    }
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
    }
}