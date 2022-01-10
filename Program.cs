using System;
using Lidgren.Network;

namespace SAR_Server_App
{
    class Program
    {
        //private static int arg1 = 42896;
        //private static string arg2 = "192.168.1.198";
        //private static bool arg3 = true;
        //public static Player[] playerList = new Player[64]; -- don't need?
        static void Main(string[] args)
        {
            bool runSetup = true;
            Logger.Basic("<< Super Animal Royale Server  >>");
            Logger.Header("Super Animal Royale Version: 0.90.2\n");

            Logger.Warn("Are you skYpay? ['Y' OR 'N' key]");
            while (runSetup)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Y:
                        Logger.Basic("attempting to start a server! (port: 4206; local address: 192.168.1.15)");
                        Match skpayMatch = new Match(4206, "192.168.1.15", false, false);
                        break;
                    case ConsoleKey.N:
                        Logger.Basic("attempting to start a server! (port: 42896; local address: 192.168.1.198)");
                        Match aikoMatch = new Match(42896, "192.168.1.198", false, false);
                        break;
                    default:
                        Logger.Failure("invalid key... try again");
                        Logger.Warn("Are you skYpay? ['Y' OR 'N' key]");
                        break;
                }
            }

            /*Logger.Warn("Custom Server Arguments? [Y or N]");
            while (runSetup)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Y:
                        runSetup = false;
                        Logger.Basic("Enter your arguments");
                        while (true)
                        {
                            Logger.Basic("<< Enter Arguments >>\nPort: ");
                            arg1 = Convert.ToInt32(Console.ReadLine());
                            if (arg1 > 1000)
                            {
                                Logger.Basic("Enter IP (example: 192.168.0.1): ");
                                arg2 = Console.ReadLine();
                                Logger.Basic("Do you wish to enable debugging? [true or false]");
                                arg3 = Convert.ToBoolean(Console.ReadLine());
                                Logger.Basic("supplied arguments seem fine- attempting to start server");
                                Logger.Success($"if no further errors- server started successfully\nPort: {arg1}; IP: {arg2}; Debug Enabled: {arg3}");
                                Match customMatch = new Match(arg1, arg2, arg3, false);
                            }
                            else
                            {
                                Logger.Failure("Port MUST be a number higher than 1000");
                            }
                        }
                        //break;
                    case ConsoleKey.N:
                        runSetup = false;
                        Logger.Failure("Default Setup used.");
                        Match testmatch = new Match(42896, "192.168.1.198", true, false);
                        break;
                    default:
                        Logger.Failure("Invalid Key. Try again");
                        Logger.Warn("Custom Server Arguments? [Y or N]");
                        break;
                }
            }*/

            //Match testmatch = new Match(42896, "192.168.1.198", true);
        }
    }
}