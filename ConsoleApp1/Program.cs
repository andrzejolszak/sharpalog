using Sharplog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        public static void Main(string[] args)
        {
            Jatalog target = new Jatalog();
            string src = File.ReadAllText("../../../examples/tum.de.dl");
            var res = target.ExecuteAll(src);
        }
    }
}
