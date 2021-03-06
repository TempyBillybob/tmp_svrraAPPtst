using System;

namespace WC.SARS
{
    class Program
    {
        static void Main(string[] args)
        {
            bool runSetup = true;
            Logger.Basic("<< Super Animal Royale Server  >>");
            Logger.Header("Super Animal Royale Version: 0.90.2\n");

            Logger.Warn("What region are you from? ['Y' OR 'N' key]");
            while (runSetup)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Y:
                        Logger.Basic("attempting to start a server! (port: 4206; local address: 192.168.1.15)");
                        Match match2 = new Match(4206, "192.168.1.15", false, false);
                        break;
                    case ConsoleKey.N:
                        Logger.Basic("attempting to start a server! (port: 42896; local address: 192.168.1.198)");
                        Match match1 = new Match(42896, "192.168.1.198", false, false);
                        break;
                    default:
                        Logger.Failure("invalid key... please try again");
                        Logger.Warn("What region are you from? ['Y' OR 'N' key]");
                        break;
                }
            }
        }
    }
}