using System;
using System.Threading.Tasks;

namespace Tail.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new TailReader();

            if (args.Length > 0)
            {
                var observable = reader.Read(args[0]);
                var subscription = observable.Subscribe(Console.WriteLine);
                Console.ReadKey();
                subscription.Dispose();
            }
            else
            {
                Console.WriteLine("Please enter the filename to process.");
            }
        }
    }
}
