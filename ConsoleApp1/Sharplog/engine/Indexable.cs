namespace Sharplog.Engine
{
    /// <summary>
    /// An interface for an object that can be indexed for use with
    /// <see cref="IndexedSet{E, I}"/>
    /// </summary>
    /// <?/>
    public interface Indexable
    {
        /// <summary>Retrieves the element according to which this instance is indexed.</summary>
        /// <returns>The index of this instance</returns>
        int Index();
    }
}