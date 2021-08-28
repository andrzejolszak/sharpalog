using System.Collections.Generic;
using NUnit.Framework;
using Sharpen;
using Sharplog.Engine;

namespace Sharplog
{
    public class StackMapTest
    {
        /*
        * I'm not too concerned to get 100% coverage for StackMap because it is
        * basically just the get(), put() and containsKey() methods that are used
        * by Jatalog
        */

        /// <exception cref="Sharplog.DatalogException"/>
        [Test]
        public virtual void TestBase()
        {
            StackMap<string, string> parent = new StackMap<string, string>();
            StackMap<string, string> child = new StackMap<string, string>(parent);
            Assert.IsTrue((child.Count == 0));
            parent.Add("W", "0");
            parent.Add("X", "1");
            Assert.IsFalse((child.Count == 0));
            child.Add("Y", "2");
            Assert.IsTrue(child.Get("Y").Equals("2"));
            Assert.IsTrue(child.Get("X").Equals("1"));
            Assert.IsTrue(child.Get("Z") == null);
            Assert.IsTrue(child.ContainsKey("X"));
            Assert.IsTrue(child.ContainsKey("Y"));
            Assert.IsFalse(child.ContainsKey("Z"));
            Assert.IsTrue(child.ContainsValue("1"));
            Assert.IsTrue(child.ContainsValue("2"));
            Assert.IsFalse(child.ContainsValue("3"));
            child.Add("X", "5");
            Assert.IsTrue(child.Get("X").Equals("5"));
            Assert.IsTrue(parent.Get("X").Equals("1"));
            Assert.IsTrue(child.ContainsValue("5"));
            //Assert.IsFalse(parent.ContainsValue("5"));
            Assert.IsTrue(child.Count == 3);
            Assert.IsTrue(child.ToString().Contains("X: 5"));
            Assert.IsTrue(child.ToString().Contains("Y: 2"));
            Assert.IsTrue(child.ToString().Contains("W: 0"));
            IDictionary<string, string> flat = child.Flatten();
            Assert.IsTrue(flat.GetOrNull("W").Equals("0"));
            Assert.IsTrue(flat.GetOrNull("X").Equals("5"));
            Assert.IsTrue(flat.GetOrNull("Y").Equals("2"));
            Assert.IsTrue(flat.GetOrNull("Z") == null);
            child.Clear();
            Assert.IsTrue(parent.Get("X").Equals("1"));
            Assert.IsTrue(child.Count == 0);
            Assert.IsTrue(child.Get("X") == null);
        }
    }
}