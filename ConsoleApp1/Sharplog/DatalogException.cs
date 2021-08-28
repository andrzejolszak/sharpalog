using System;

namespace Sharplog
{
    /// <summary>Normal Datalog exception.</summary>
    /// <remarks>
    /// Normal Datalog exception.
    /// <p>
    /// It is used for a variety of reasons:
    /// </p>
    /// <ul>
    /// <li> Trying to execute a file that doesn't exist.
    /// <li> Trying to add invalid facts or rules to the database. For example, facts are invalid if they are not ground
    /// and rules are invalid if variables in the head don't appear in the body.
    /// <li> Using built-in predicates in invalid ways, such as comparing unbound variables.
    /// </ul>
    /// </remarks>
    [System.Serializable]
    public class DatalogException : Exception
    {
        private const long serialVersionUID = 1L;

        /// <summary>Constructor with a message</summary>
        /// <param name="message">A description of the problem</param>
        public DatalogException(string message)
            : base(message)
        {
        }

        /// <summary>Constructor with a cause</summary>
        /// <param name="cause">The exception that was thrown to cause this one</param>
        public DatalogException(Exception cause)
            : base(cause.Message, cause)
        {
        }

        /// <summary>Constructor with a message and a cause</summary>
        /// <param name="message">A description of the problem</param>
        /// <param name="cause">The exception that was thrown to cause this one</param>
        public DatalogException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }
}