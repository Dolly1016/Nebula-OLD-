using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUAffinityEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 2) return;

                var p = System.Diagnostics.Process.GetProcessById(int.Parse(args[0]));
                p.ProcessorAffinity = (IntPtr)int.Parse(args[1]);

            }
            catch
            {
                Console.WriteLine("Error occured.");
            }
        }
    }
}
