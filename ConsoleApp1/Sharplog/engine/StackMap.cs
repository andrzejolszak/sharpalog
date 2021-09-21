using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharplog.Engine
{
    /// <summary>
    /// Map&lt;&gt; implementation that has a parent Map&lt;&gt; where it looks up a value if the value is not in
    /// <c>this</c>
    /// .
    /// <p>
    /// Its behaviour is equivalent to a HashMap that is passed a parent map in its constructor, except that it keeps a reference
    /// to the parent map rather than copying it. It never modifies the parent map. (If the behaviour deviates from this, it is a bug
    /// and must be fixed).
    /// </p><p>
    /// Internally, it has two maps:
    /// <c>self</c>
    /// and
    /// <c>parent</c>
    /// . The
    /// <see cref="StackMap{K, V}.Get(object)"/>
    /// method will look up a key in self and if it doesn't find it, looks
    /// for it in
    /// <c>parent</c>
    /// . The
    /// <see cref="Sharpen.Collections.Put(object, object)"/>
    /// method always adds values to
    /// <c>self</c>
    /// . The parent in turn may also be a StackMap, so some method
    /// calls may be recursive. The
    /// <see cref="StackMap{K, V}.Flatten()"/>
    /// method combines the map with its parents into a single one.
    /// </p><p>
    /// It is used by Jatalog for the scoped contexts where the variable bindings enter and leave scope frequently during the
    /// recursive building of the database, but where the parent Map&lt;&gt; needs to be kept for subsequent recursions.
    /// </p><p>
    /// Methods like
    /// <see cref="StackMap{K, V}.()"/>
    /// ,
    /// <see cref="StackMap{K, V}.Keys()"/>
    /// and
    /// <see cref="StackMap{K, V}.Values()"/>
    /// are required by the Map&lt;&gt; interface that their returned collections be backed
    /// by the Map&lt;&gt;. Therefore, their implementations here will flatten the map first. Once these methods are called StackMap just
    /// becomes a wrapper around the internal HashMap, hence Jatalog avoids these methods internally.
    /// </p><p>
    /// The
    /// <see cref="Sharpen.Collections.Remove(object)"/>
    /// method also flattens
    /// <c>this</c>
    /// to avoid modifying the parent while and the
    /// <see cref="StackMap{K, V}.Clear()"/>
    /// method just sets parent to null
    /// and clears
    /// <c>self</c>
    /// .
    /// </p><p>
    /// I initially just assumed that using the StackMap would be faster, so I tried an implementation with a
    /// <see cref="System.Collections.Generic.Dictionary<object, object>{K, V}"/>
    /// where I just did a
    /// <c>newMap.putAll(parent)</c>
    /// and removed the StackMap entirely. My rough benchmarks showed the StackMap-based implementation to be about 30%
    /// faster than the alternative.
    /// I've also tried a version that extends
    /// <see cref="Sharpen.AbstractMap{K, V}"/>
    /// , but it proved to be significantly slower.
    /// </p>
    /// </summary>
    public class StackMap
    {
        private IDictionary<string, string> self;

        public List<string> Stack { get; } = new List<string>();

        public StackMap(int initDictSize = 3)
        {
            self = new Dictionary<string, string>(initDictSize);
        }

        public void ClearTest()
        {
            // We don't want to modify the parent, so we just orphan this
            self.Clear();
            Stack.Clear();
        }

        public IDictionary<string, string> CloneAsDictionary()
        {
            return new Dictionary<string, string>(this.self);
        }

        public IDictionary<string, string> DictionaryObject()
        {
            return this.self;
        }

        public void Add(string key, string value)
        {
#if DEBUG 
            if (value == null || !Jatalog.IsVariable(key) || Jatalog.IsVariable(value))
            {
                throw new InvalidOperationException();
            }
#endif

            self.Add(key, value);
            Stack.Add(key);
        }

        public void RemoveUntil(int stack)
        {
            while (this.Stack.Count > stack)
            {
                self.Remove(this.Stack[this.Stack.Count - 1]);
                this.Stack.RemoveAt(this.Stack.Count - 1);
            }
        }

        public bool TryGetValue(string key, out string result)
        {
            return self.TryGetValue(key, out result);
        }
    }
}