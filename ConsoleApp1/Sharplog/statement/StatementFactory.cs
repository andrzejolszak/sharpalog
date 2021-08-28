using System.Collections.Generic;
using Sharpen;
using Sharplog;

namespace Sharplog.Statement
{
	/// <summary>
	/// Provides factory methods for building Statement instances for
	/// use with the fluent API.
	/// </summary>
	/// <remarks>
	/// Provides factory methods for building Statement instances for
	/// use with the fluent API.
	/// <p>
	/// <see cref="Sharplog.Jatalog.PrepareStatement(string)"/>
	/// can be used to parse
	/// Strings to statement object.
	/// </p>
	/// </remarks>
	/// <seealso cref="Statement"/>
	/// <seealso cref="Statement.Execute(Sharplog.Jatalog, System.Collections.Generic.IDictionary{K, V})"/>
	/// <seealso cref="Sharplog.Jatalog.PrepareStatement(string)"/>
	public class StatementFactory
	{
		/// <summary>Creates a statement to query the database.</summary>
		/// <param name="goals">The goals of the query</param>
		/// <returns>A statement that will query the database for the given goals.</returns>
		public static Sharplog.Statement.Statement Query(IList<Expr> goals)
		{
			return new QueryStatement(goals);
		}

		/// <summary>Creates a statement that will insert a fact into the EDB.</summary>
		/// <param name="fact">The fact to insert</param>
		/// <returns>A statement that will insert the given fact into the database.</returns>
		public static Sharplog.Statement.Statement InsertFact(Expr fact)
		{
			return new InsertFactStatement(fact);
		}

		/// <summary>Creates a statement that will insert a rule into the IDB.</summary>
		/// <param name="rule">The rule to insert</param>
		/// <returns>A statement that will insert the given rule into the database.</returns>
		public static Sharplog.Statement.Statement InsertRule(Rule rule)
		{
			return new InsertRuleStatement(rule);
		}

		/// <summary>Creates a statement that will delete facts from the database.</summary>
		/// <param name="goals">The goals of the facts to delete</param>
		/// <returns>A statement that will delete facts matching the goals from the database.</returns>
		public static Sharplog.Statement.Statement DeleteFacts(IList<Expr> goals)
		{
			return new DeleteStatement(goals);
		}
	}
}
