using ImageStacking.Stacking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageStacking
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;

            StackingController stackingController = new StackingController();
            stackingController.Run();

            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }
    }
}
