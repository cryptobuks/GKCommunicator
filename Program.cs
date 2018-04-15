﻿using System;
using System.Net;

namespace DHTConnector
{
    class Program
    {
        public const string NETWORK_SIGN = "GEDKEEPER NETWORK";

        static int Main(string[] args)
        {
            int port;
            if (args.Length == 0 || !int.TryParse(args[0], out port)) {
                port = 6882;
            }

            var dhtClient = new DHTClient(port, IPAddress.Any);
            dhtClient.SubnetKey = NETWORK_SIGN;
            dhtClient.PeersFound += delegate (object sender, PeersFoundEventArgs e) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Found peers: {0}", e.Peers.Count);
                Console.ResetColor();
            };

            dhtClient.Run();
            dhtClient.JoinNetwork();
            dhtClient.SendFindNodes();

            Console.ReadLine();
            return 0;
        }
    }
}
