using System;

namespace Tail.App
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                var reader = new TailReader(args[0]);
                var observable = reader.Read();
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
