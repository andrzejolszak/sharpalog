using System.Collections.Generic;
using Sharpen;
using Sharplog.Engine;

namespace Sharplog.Statement
{
	/// <summary>Represents a statement that can be executed against a Jatalog database.</summary>
	/// <remarks>
	/// Represents a statement that can be executed against a Jatalog database.
	/// <p>
	/// There are several types of statements: to insert facts, to insert rules,
	/// to retract facts and to query the database.
	/// </p><p>
	/// Instances of Statement are created by
	/// <see cref="StatementFactory"/>
	/// .
	/// </p><p>
	/// Strings can be parsed to Statements through
	/// <see cref="Sharplog.Jatalog.PrepareStatement(string)"/>
	/// </p>
	/// </remarks>
	/// <seealso cref="StatementFactory"/>
	/// <seealso cref="Sharplog.Jatalog.PrepareStatement(string)"/>
	public interface Statement
	{
		/// <summary>Executes a statement against a Jatalog database.</summary>
		/// <param name="datalog">The database against which to execute the statement.</param>
		/// <param name="bindings">
		/// an optional (nullable) mapping of variables to values.
		/// <p>
		/// A statement like "a(B,C)?" with bindings
		/// <c>&lt;B = "foo", C = "bar"&gt;</c>
		/// is equivalent to the statement "a(foo,bar)?"
		/// </p>
		/// </param>
		/// <returns>
		/// The result of the statement.
		/// <ul>
		/// <li> If null, the statement was an insert or delete that didn't produce query results.
		/// <li> If empty the query's answer is "No."
		/// <li> If a list of empty maps, then answer is "Yes."
		/// <li> Otherwise it is a list of all bindings that satisfy the query.
		/// </ul>
		/// Jatalog provides a
		/// <see cref="Sharplog.Output.OutputUtils.AnswersToString(System.Collections.Generic.IEnumerable{E})"/>
		/// method that can convert answers to
		/// Strings
		/// </returns>
		/// <exception cref="Sharplog.DatalogException">if an error occurs in processing the statement</exception>
		/// <seealso cref="Sharplog.Output.OutputUtils.AnswersToString(System.Collections.Generic.IEnumerable{E})"/>
		IEnumerable<IDictionary<string, string>> Execute(Sharplog.Jatalog datalog, StackMap bindings);
	}
}
