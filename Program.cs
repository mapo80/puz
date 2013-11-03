using System;
using System.Collections.Generic;
using System.Text;

namespace PuzReader
{
    class Program
    {
        static void Main(string[] args)
        {
            PuzFile p = new PuzFile();
            p.start("wsj110624.puz");

#if DEBUG
            Console.WriteLine("Press any key to close...");
            Console.ReadLine();
#endif
        }
    }
}
