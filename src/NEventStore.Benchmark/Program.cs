using BenchmarkDotNet.Running;

namespace NEventStore.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);

            if (args.Length > 0)
            {
                switcher.Run(args);
                return;
            }

            ShowMenu();

            while (true)
            {
                Console.Write("Select benchmark execution mode [1-2]: ");
                var selection = Console.ReadLine();

                switch (selection)
                {
                    case "1":
                        switcher.Run([]);
                        return;
                    case "2":
                        switcher.RunAllJoined(config: null, args: []);
                        return;
                    default:
                        Console.WriteLine("Invalid selection. Enter 1 for Run or 2 for RunAllJoined.");
                        break;
                }
            }
        }

        private static void ShowMenu()
        {
            Console.WriteLine("Benchmark execution mode:");
            Console.WriteLine("  1. Run");
            Console.WriteLine("  2. RunAllJoined");
        }
    }
}
