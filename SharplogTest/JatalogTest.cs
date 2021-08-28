using NUnit.Framework;

namespace Sharplog
{
    /// <summary>Basic unit tests.</summary>
    public class JatalogTest
    {
        [Test]
        public virtual void TestEquals()
        {
            /* Truth be told the only reason I need Jatalog.equals() to work
			* is so that I can test Jatalog.toString() */
            Sharplog.Jatalog thisJatalog = TestUtils.CreateDatabase();
            Sharplog.Jatalog thatJatalog = TestUtils.CreateDatabase();
            Assert.IsTrue(thisJatalog != thatJatalog);
            Assert.IsTrue(thisJatalog.Equals(thatJatalog));
            thatJatalog.Fact("foo", "bar");
            Assert.IsFalse(thisJatalog.Equals(thatJatalog));
        }

        [Test]
        public virtual void TestToString()
        {
            Sharplog.Jatalog thisJatalog = TestUtils.CreateDatabase();
            string @string = thisJatalog.ToString();
            Sharplog.Jatalog thatJatalog = new Sharplog.Jatalog();
            thatJatalog.ExecuteAll(@string);
            Assert.IsTrue(thisJatalog != thatJatalog);
            Assert.IsTrue(thisJatalog.Equals(thatJatalog));
        }
    }
}