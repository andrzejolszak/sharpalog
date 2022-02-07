using System.Globalization;

namespace Stringes
{
    /// <summary>
    /// Represents a charactere, which provides location information on a character taken from a stringe.
    /// </summary>
    public sealed class Chare
    {
        private readonly Stringe _src;
        private readonly char _character;
        private readonly int _offset;

        /// <summary>
        /// The stringe from which the charactere was taken.
        /// </summary>
        public Stringe Source => _src;

        /// <summary>
        /// The underlying character.
        /// </summary>
        public char Character => _character;

        /// <summary>
        /// The position of the charactere in the stringe.
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        /// The line on which the charactere appears.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// The column on which the charactere appears.
        /// </summary>
        public int Column { get; }

        internal Chare(Stringe source, char c, int offset, (int line, int col) lineCol)
        {
            _src = source;
            _character = c;
            _offset = offset;
            Line = lineCol.line;
            Column = lineCol.col;
        }

        /// <summary>
        /// Returns the string representation of the current charactere.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => _character.ToString(CultureInfo.InvariantCulture);

        public static bool operator ==(Chare chare, char c)
        {
            return chare?._character == c;
        }

        public static bool operator !=(Chare chare, char c)
        {
            return !(chare == c);
        }
    }
}