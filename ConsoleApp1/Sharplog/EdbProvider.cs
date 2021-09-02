using System.Collections.Generic;
using Sharpen;
using Sharplog.Engine;

namespace Sharplog
{
	/// <summary>
	/// The EdbProvider allows the EDB from Jatalog's perspective to be abstracted away from the actual
	/// storage mechanism.
	/// </summary>
	/// <remarks>
	/// The EdbProvider allows the EDB from Jatalog's perspective to be abstracted away from the actual
	/// storage mechanism.
	/// <p>
	/// The purpose is to allow different sources for the EDB data, such as CSV or XML files or even a SQL
	/// or NoSQL database.
	/// </p><p>
	/// Jatalog uses a
	/// <see cref="BasicEdbProvider"/>
	/// by default, which simply stores facts in memory, but it
	/// can be changed through the
	/// <see cref="Jatalog.SetEdbProvider(EdbProvider)"/>
	/// method.
	/// </p>
	/// </remarks>
	/// <seealso cref="BasicEdbProvider"/>
	public interface EdbProvider
	{
		/// <summary>
		/// Retrieves a
		/// <c>Collection</c>
		/// of all the facts in the database.
		/// </summary>
		/// <returns>All the facts in the EDB</returns>
		IndexedSet AllFacts();

		/// <summary>Adds a fact to the EDB database.</summary>
		/// <param name="fact">The fact to add</param>
		void Add(Expr fact);

		/// <summary>Removes facts from the database</summary>
		/// <param name="facts">the facts to remove</param>
		/// <returns>true if facts were removed</returns>
		bool RemoveAll(IEnumerable<Expr> facts);

		/// <summary>Retrieves all the facts in the database that match specific predicate.</summary>
		/// <param name="predicate">The predicate of the facts to be retrieved.</param>
		/// <returns>
		/// A collection of facts matching the
		/// <paramref name="predicate"/>
		/// </returns>
		IEnumerable<Expr> GetFacts(string predicate);
	}
}
