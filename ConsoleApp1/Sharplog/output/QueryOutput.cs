using System.Collections.Generic;
using Sharpen;

namespace Sharplog.Output
{
	/// <summary>Interface that is used to output the result of a Jatalog statement execution.</summary>
	/// <remarks>
	/// Interface that is used to output the result of a Jatalog statement execution.
	/// <p>
	/// If you're executing a file that may contain multiple queries, you can pass
	/// <see cref="Sharplog.Jatalog.ExecuteAll(System.IO.StreamReader, QueryOutput)"/>
	/// a
	/// <see cref="QueryOutput"/>
	/// object that will be used to display
	/// all the results from the separate queries, with their goals.
	/// Otherwise, if you set the QueryOutput parameter to
	/// <see langword="null"/>
	/// ,
	/// <see cref="Sharplog.Jatalog.ExecuteAll(System.IO.StreamReader, QueryOutput)"/>
	/// will just return the answers from the last query.
	/// </p>
	/// </remarks>
	/// <seealso cref="OutputUtils.AnswersToString(System.Collections.Generic.IEnumerable{E})"/>
	/// <seealso cref="OutputUtils.BindingsToString(System.Collections.Generic.IDictionary{K, V})"/>
	public interface QueryOutput
	{
		/// <summary>Method called by the engine to output the results of a query.</summary>
		/// <param name="statement">The statement that was evaluated to produce the output.</param>
		/// <param name="answers">The result of the query, as a Collection of variable mappings.</param>
		void WriteResult(Sharplog.Statement.Statement statement, IEnumerable<IDictionary<string, string>> answers);
	}
}
