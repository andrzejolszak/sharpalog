using System;
using System.IO;
using System.Text;

namespace Sharpen
{
    public class StreamTokenizer
    {
        /* Only one of these will be non-null */
        public const int TT_EOF = -1;
        public const int TT_EOL = '\n';
        public const int TT_NUMBER = -2;
        public const int TT_WORD = -3;
        private const int NEED_CHAR = Int32.MaxValue;
        private const int SKIP_LF = Int32.MaxValue - 1;
        private const byte CT_WHITESPACE = 1;
        private const byte CT_DIGIT = 2;
        private const byte CT_ALPHA = 4;
        private const byte CT_QUOTE = 8;
        private const byte CT_COMMENT = 16;
        private const int TT_NOTHING = -4;
        private MemoryStream stream;

        /**
         * The next character to be considered by the nextToken method.  May also
         * be NEED_CHAR to indicate that a new character should be Read, or SKIP_LF
         * to indicate that a new character should be Read and, if it is a '\n'
         * character, it should be discarded and a second new character should be
         * Read.
         */
        public int ttype = TT_NOTHING;
        public int peekc = NEED_CHAR;
        public bool pushedBack;
        /** The line number of the last token Read */

        private const bool eolIsSignificantP = false;
        private const bool slashSlashCommentsP = false;
        private const bool slashStarCommentsP = false;

        private byte[] characterType = new byte[256];

        public StreamTokenizer(MemoryStream r) : this()
        {
            if (r == null)
            {
                throw new ArgumentNullException();
            }
            stream = r;
        }

        private StreamTokenizer()
        {
            WordChars('a', 'z');
            WordChars('A', 'Z');
            WordChars('_', '_');
            WordChars(128 + 32, 255);
            WhitespaceChars(0, ' ');
            CommentChar('/');
            QuoteChar('"');
            QuoteChar('\'');
            ParseNumbers();
            LineNumber = 1;
        }

        public int LineNumber { get; private set; }

        /**
         * After a call to the <code>nextToken</code> method, this field
         * contains the type of the token just Read. For a single character
         * token, its value is the single character, converted to an integer.
         * For a quoted string token, its value is the quote character.
         * Otherwise, its value is one of the following:
         * <ul>
         * <li><code>TT_WORD</code> indicates that the token is a word.
         * <li><code>TT_NUMBER</code> indicates that the token is a number.
         * <li><code>TT_EOL</code> indicates that the end of line has been Read.
         *     The field can only have this value if the
         *     <code>eolIsSignificant</code> method has been called with the
         *     argument <code>true</code>.
         * <li><code>TT_EOF</code> indicates that the end of the input stream
         *     has been reached.
         * </ul>
         * <p>
         * The initial value of this field is -4.
         *
         */
        /**
         * A constant indicating that the end of the stream has been Read.
         */
        /**
         * A constant indicating that the end of the line has been Read.
         */
        /**
         * A constant indicating that a number token has been Read.
         */
        /**
         * A constant indicating that a word token has been Read.
         */
        /* A constant indicating that no token has been Read, used for
         * initializing ttype.  FIXME This could be made public and
         * made available as the part of the API in a future release.
         */
        /**
         * If the current token is a word token, this field contains a
         * string giving the characters of the word token. When the current
         * token is a quoted string token, this field contains the body of
         * the string.
         * <p>
         * The current token is a word when the value of the
         * <code>ttype</code> field is <code>TT_WORD</code>. The current token is
         * a quoted string token when the value of the <code>ttype</code> field is
         * a quote character.
         * <p>
         * The initial value of this field is null.
         *
         * @see     java.io.StreamTokenizer#quoteChar(int)
         * @see     java.io.StreamTokenizer#TT_WORD
         * @see     java.io.StreamTokenizer#ttype
         */
        public string StringValue { get; private set; }

        /**
         * If the current token is a number, this field contains the value
         * of that number. The current token is a number when the value of
         * the <code>ttype</code> field is <code>TT_NUMBER</code>.
         * <p>
         * The initial value of this field is 0.0.
         */
        public double NumberValue { get; private set; }

        /**
         * Specifies that all characters <i>c</i> in the range
         * <code>low&nbsp;&lt;=&nbsp;<i>c</i>&nbsp;&lt;=&nbsp;high</code>
         * are word constituents. A word token consists of a word constituent
         * followed by zero or more word constituents or number constituents.
         *
         * @param   low   the low end of the range.
         * @param   hi    the high end of the range.
         */

        public void WordChars(int low, int hi)
        {
            if (low < 0)
            {
                low = 0;
            }
            if (hi >= characterType.Length)
            {
                hi = characterType.Length - 1;
            }
            while (low <= hi)
            {
                characterType[low++] |= CT_ALPHA;
            }
        }

        /**
         * Specifies that all characters <i>c</i> in the range
         * <code>low&nbsp;&lt;=&nbsp;<i>c</i>&nbsp;&lt;=&nbsp;high</code>
         * are white space characters. White space characters serve only to
         * separate tokens in the input stream.
         *
         * <p>Any other attribute settings for the characters in the specified
         * range are cleared.
         *
         * @param   low   the low end of the range.
         * @param   hi    the high end of the range.
         */

        public void WhitespaceChars(int low, int hi)
        {
            if (low < 0)
            {
                low = 0;
            }
            if (hi >= characterType.Length)
            {
                hi = characterType.Length - 1;
            }
            while (low <= hi)
            {
                characterType[low++] = CT_WHITESPACE;
            }
        }

        /**
         * Specifies that all characters <i>c</i> in the range
         * <code>low&nbsp;&lt;=&nbsp;<i>c</i>&nbsp;&lt;=&nbsp;high</code>
         * are "ordinary" in this tokenizer. See the
         * <code>ordinaryChar</code> method for more information on a
         * character being ordinary.
         *
         * @param   low   the low end of the range.
         * @param   hi    the high end of the range.
         * @see     java.io.StreamTokenizer#ordinaryChar(int)
         */

        /**
         * Specifies that the character argument is "ordinary"
         * in this tokenizer. It removes any special significance the
         * character has as a comment character, word component, string
         * delimiter, white space, or number character. When such a character
         * is encountered by the parser, the parser treats it as a
         * single-character token and sets <code>ttype</code> field to the
         * character value.
         *
         * <p>Making a line terminator character "ordinary" may interfere
         * with the ability of a <code>StreamTokenizer</code> to count
         * lines. The <code>lineno</code> method may no longer reflect
         * the presence of such terminator characters in its line count.
         *
         * @param   ch   the character.
         */

        public void OrdinaryChar(int ch)
        {
            if (ch >= 0 && ch < characterType.Length)
                characterType[ch] = 0;
        }

        /**
         * Specified that the character argument starts a single-line
         * comment. All characters from the comment character to the end of
         * the line are ignored by this stream tokenizer.
         *
         * <p>Any other attribute settings for the specified character are cleared.
         *
         * @param   ch   the character.
         */

        public void CommentChar(int ch)
        {
            if (ch >= 0 && ch < characterType.Length)
            {
                characterType[ch] = CT_COMMENT;
            }
        }

        /**
         * Specifies that matching pairs of this character delimit string
         * constants in this tokenizer.
         * <p>
         * When the <code>nextToken</code> method encounters a string
         * constant, the <code>ttype</code> field is set to the string
         * delimiter and the <code>sval</code> field is set to the body of
         * the string.
         * <p>
         * If a string quote character is encountered, then a string is
         * recognized, consisting of all characters after (but not including)
         * the string quote character, up to (but not including) the next
         * occurrence of that same string quote character, or a line
         * terminator, or end of file. The usual escape sequences such as
         * <code>"&#92;n"</code> and <code>"&#92;t"</code> are recognized and
         * converted to single characters as the string is parsed.
         *
         * <p>Any other attribute settings for the specified character are cleared.
         *
         * @param   ch   the character.
         */

        public void QuoteChar(int ch)
        {
            if (ch >= 0 && ch < characterType.Length)
                characterType[ch] = CT_QUOTE;
        }

        /**
         * Specifies that numbers should be parsed by this tokenizer. The
         * syntax table of this tokenizer is modified so that each of the twelve
         * characters:
         * <blockquote><pre>
         *      0 1 2 3 4 5 6 7 8 9 . -
         * </pre></blockquote>
         * <p>
         * has the "numeric" attribute.
         * <p>
         * When the parser encounters a word token that has the format of a
         * double precision floating-point number, it treats the token as a
         * number rather than a word, by setting the <code>ttype</code>
         * field to the value <code>TT_NUMBER</code> and putting the numeric
         * value of the token into the <code>nval</code> field.
         */

        public void ParseNumbers()
        {
            for (int i = '0'; i <= '9'; i++)
            {
                characterType[i] |= CT_DIGIT;
            }
            characterType['.'] |= CT_DIGIT;
            characterType['-'] |= CT_DIGIT;
        }

        /**
         * Determines whether or not ends of line are treated as tokens.
         * If the flag argument is true, this tokenizer treats end of lines
         * as tokens; the <code>nextToken</code> method returns
         * <code>TT_EOL</code> and also sets the <code>ttype</code> field to
         * this value when an end of line is Read.
         * <p>
         * A line is a sequence of characters ending with either a
         * carriage-return character (<code>'&#92;r'</code>) or a newline
         * character (<code>'&#92;n'</code>). In addition, a carriage-return
         * character followed immediately by a newline character is treated
         * as a single end-of-line token.
         * <p>
         * If the <code>flag</code> is false, end-of-line characters are
         * treated as white space and serve only to separate tokens.
         *
         * @param   flag   <code>true</code> indicates that end-of-line characters
         *                 are separate tokens; <code>false</code> indicates that
         *                 end-of-line characters are white space.
         */

        /**
         * Determines whether or not the tokenizer recognizes C-style comments.
         * If the flag argument is <code>true</code>, this stream tokenizer
         * recognizes C-style comments. All text between successive
         * occurrences of <code>/*</code> and <code>*&#47;</code> are discarded.
         * <p>
         * If the flag argument is <code>false</code>, then C-style comments
         * are not treated specially.
         *
         * @param   flag   <code>true</code> indicates to recognize and ignore
         *                 C-style comments.
         */
        /**
         * Determines whether or not the tokenizer recognizes C++-style comments.
         * If the flag argument is <code>true</code>, this stream tokenizer
         * recognizes C++-style comments. Any occurrence of two consecutive
         * slash characters (<code>'/'</code>) is treated as the beginning of
         * a comment that extends to the end of the line.
         * <p>
         * If the flag argument is <code>false</code>, then C++-style
         * comments are not treated specially.
         *
         * @param   flag   <code>true</code> indicates to recognize and ignore
         *                 C++-style comments.
         */
        /**
         * Determines whether or not word token are automatically lowercased.
         * If the flag argument is <code>true</code>, then the value in the
         * <code>sval</code> field is lowercased whenever a word token is
         * returned (the <code>ttype</code> field has the
         * value <code>TT_WORD</code> by the <code>nextToken</code> method
         * of this tokenizer.
         * <p>
         * If the flag argument is <code>false</code>, then the
         * <code>sval</code> field is not modified.
         *
         * @param   fl   <code>true</code> indicates that all word tokens should
         *               be lowercased.
         */
        /** Read the next character */

        public int NextToken()
        {
            if (pushedBack)
            {
                pushedBack = false;
                return ttype;
            }
            byte[] ct = characterType;
            StringValue = null;

            int c = peekc;
            if (c < 0)
                c = NEED_CHAR;
            if (c == SKIP_LF)
            {
                c = Read();
                if (c < 0)
                    return ttype = TT_EOF;
                if (c == '\n')
                    c = NEED_CHAR;
            }
            if (c == NEED_CHAR)
            {
                c = Read();
                if (c < 0)
                    return ttype = TT_EOF;
            }
            ttype = c;      /* Just to be safe */

            /* Set peekc so that the next invocation of nextToken will Read
             * another character unless peekc is reset in this invocation
             */
            peekc = NEED_CHAR;

            int ctype = c < 256 ? ct[c] : CT_ALPHA;
            while ((ctype & CT_WHITESPACE) != 0)
            {
                if (c == '\r')
                {
                    LineNumber++;
                    if (eolIsSignificantP)
                    {
                        peekc = SKIP_LF;
                        return ttype = TT_EOL;
                    }
                    c = Read();
                    if (c == '\n')
                        c = Read();
                }
                else
                {
                    if (c == '\n')
                    {
                        LineNumber++;
                        if (eolIsSignificantP)
                        {
                            return ttype = TT_EOL;
                        }
                    }
                    c = Read();
                }
                if (c < 0)
                    return ttype = TT_EOF;
                ctype = c < 256 ? ct[c] : CT_ALPHA;
            }

            if ((ctype & CT_DIGIT) != 0)
            {
                bool neg = false;
                if (c == '-')
                {
                    c = Read();
                    if (c != '.' && (c < '0' || c > '9'))
                    {
                        peekc = c;
                        return ttype = '-';
                    }
                    neg = true;
                }
                double v = 0;
                int decexp = 0;
                int seendot = 0;
                int prev = 0;
                while (true)
                {
                    if (c == '.' && seendot == 0)
                        seendot = 1;
                    else if ('0' <= c && c <= '9')
                    {
                        v = v * 10 + (c - '0');
                        decexp += seendot;
                    }
                    else
                        break;

                    prev = c;
                    c = Read();
                }

                if (prev == '.')
                {
                    stream.Position -= 2;
                    c = Read();
                }

                peekc = c;
                if (decexp != 0)
                {
                    double denom = 10;
                    decexp--;
                    while (decexp > 0)
                    {
                        denom *= 10;
                        decexp--;
                    }
                    /* Do one division of a likely-to-be-more-accurate number */
                    v = v / denom;
                }
                NumberValue = neg ? -v : v;
                return ttype = TT_NUMBER;
            }

            if ((ctype & CT_ALPHA) != 0)
            {
                StringBuilder buf = new StringBuilder();

                do
                {
                    buf.Append((char)c);
                    c = Read();
                    ctype = c < 0 ? CT_WHITESPACE : c < 256 ? ct[c] : CT_ALPHA;
                } while ((ctype & (CT_ALPHA | CT_DIGIT)) != 0);
                peekc = c;
                StringValue = buf.ToString();
                return ttype = TT_WORD;
            }

            if ((ctype & CT_QUOTE) != 0)
            {
                StringBuilder buf = new StringBuilder();

                ttype = c;
                /* Invariants (because \Octal needs a lookahead):
                 *   (i)  c contains char value
                 *   (ii) d contains the lookahead
                 */
                int d = Read();
                while (d >= 0 && d != ttype && d != '\n' && d != '\r')
                {
                    if (d == '\\')
                    {
                        c = Read();
                        int first = c;   /* To allow \377, but not \477 */
                        if (c >= '0' && c <= '7')
                        {
                            c = c - '0';
                            int c2 = Read();
                            if ('0' <= c2 && c2 <= '7')
                            {
                                c = (c << 3) + (c2 - '0');
                                c2 = Read();
                                if ('0' <= c2 && c2 <= '7' && first <= '3')
                                {
                                    c = (c << 3) + (c2 - '0');
                                    d = Read();
                                }
                                else
                                    d = c2;
                            }
                            else
                                d = c2;
                        }
                        else
                        {
                            switch (c)
                            {
                                case 'a':
                                    c = 0x7;
                                    break;

                                case 'b':
                                    c = '\b';
                                    break;

                                case 'f':
                                    c = 0xC;
                                    break;

                                case 'n':
                                    c = '\n';
                                    break;

                                case 'r':
                                    c = '\r';
                                    break;

                                case 't':
                                    c = '\t';
                                    break;

                                case 'v':
                                    c = 0xB;
                                    break;
                            }
                            d = Read();
                        }
                    }
                    else
                    {
                        c = d;
                        d = Read();
                    }
                    buf.Append((char)c);
                }

                /* If we broke out of the loop because we found a matching quote
                 * character then arrange to Read a new character next time
                 * around; otherwise, save the character.
                 */
                peekc = (d == ttype) ? NEED_CHAR : d;

                StringValue = buf.ToString();
                return ttype;
            }

            if (c == '/' && (slashSlashCommentsP || slashStarCommentsP))
            {
                c = Read();
                if (c == '*' && slashStarCommentsP)
                {
                    int prevc = 0;
                    while ((c = Read()) != '/' || prevc != '*')
                    {
                        if (c == '\r')
                        {
                            LineNumber++;
                            c = Read();
                            if (c == '\n')
                            {
                                c = Read();
                            }
                        }
                        else
                        {
                            if (c == '\n')
                            {
                                LineNumber++;
                                c = Read();
                            }
                        }
                        if (c < 0)
                            return ttype = TT_EOF;
                        prevc = c;
                    }
                    return NextToken();
                }
                else if (c == '/' && slashSlashCommentsP)
                {
                    while ((c = Read()) != '\n' && c != '\r' && c >= 0) ;
                    peekc = c;
                    return NextToken();
                }
                else
                {
                    /* Now see if it is still a single line comment */
                    if ((ct['/'] & CT_COMMENT) != 0)
                    {
                        while ((c = Read()) != '\n' && c != '\r' && c >= 0) ;
                        peekc = c;
                        return NextToken();
                    }
                    else
                    {
                        peekc = c;
                        return ttype = '/';
                    }
                }
            }

            if ((ctype & CT_COMMENT) != 0)
            {
                while ((c = Read()) != '\n' && c != '\r' && c >= 0) ;
                peekc = c;
                return NextToken();
            }

            return ttype = c;
        }

        public void PushBack()
        {
            if (ttype != TT_NOTHING)   /* No-op if nextToken() not called */
            {
                pushedBack = true;
            }
        }

        public override string ToString()
        {
            string ret;
            switch (ttype)
            {
                case TT_EOF:
                    ret = "EOF";
                    break;

                case TT_EOL:
                    ret = "EOL";
                    break;

                case TT_WORD:
                    ret = StringValue;
                    break;

                case TT_NUMBER:
                    ret = "n=" + NumberValue;
                    break;

                case TT_NOTHING:
                    ret = "NOTHING";
                    break;

                default:
                    {
                        /*
                         * ttype is the first character of either a quoted string or
                         * is an ordinary character. ttype can definitely not be less
                         * than 0, since those are reserved values used in the previous
                         * case statements
                         */
                        if (ttype < 256 &&
                            ((characterType[ttype] & CT_QUOTE) != 0))
                        {
                            ret = StringValue;
                            break;
                        }

                        char[] s = new char[3];
                        s[0] = s[2] = '\'';
                        s[1] = (char)ttype;
                        ret = new string(s);
                        break;
                    }
            }
            return "Token[" + ret + "], line " + LineNumber;
        }

        public (long, bool, int, int, int, string, double) CurrentState => (stream.Position, pushedBack, peekc, ttype, LineNumber, StringValue, NumberValue);

        public void RewindToState((long position, bool pushedBack, int peekc, int ttype, int line, string stringVal, double numberVal) state)
        {
            stream.Position = state.position;
            this.pushedBack = state.pushedBack;
            this.peekc = state.peekc;
            this.ttype = state.ttype;
            this.LineNumber = state.line;
            this.StringValue = state.stringVal;
            this.NumberValue = state.numberVal;
        }

        private int Read()
        {
            if (stream != null)
            {
                return stream.ReadByte();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}