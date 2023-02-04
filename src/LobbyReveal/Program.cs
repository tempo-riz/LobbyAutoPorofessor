using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ekko;
using Newtonsoft.Json;

namespace LobbyReveal
{
    internal class Program
    {
        private static LobbyHandler _handler;
        private static bool _update = true;

        public async static Task Main(string[] args)
        {
            var watcher = new LeagueClientWatcher();
            watcher.OnLeagueClient += (clientWatcher, client) =>
            {
                Console.WriteLine(client.Pid);
                _handler = new LobbyHandler(new LeagueApi(client.ClientAuthInfo.RiotClientAuthToken,
                    client.ClientAuthInfo.RiotClientPort));
                _handler.OnUpdate += (LH) =>
                {
                    var region = LH.GetRegion().ToString().ToLower();
                    var summoners = string.Join(",", LH.GetSummoners());

                    Console.Clear();
                    Console.WriteLine($"------------{region}------------");
                    Console.WriteLine("Current summoners:");
                    Console.WriteLine(summoners);
                    Console.WriteLine();

                    Process.Start($"https://porofessor.gg/pregame/{region}/{HttpUtility.UrlEncode(summoners)}");

                };
                _handler.Start();
            };
            new Thread(async () => { await watcher.Observe(); })
            {
                IsBackground = true
            }.Start();

            // keep the program alive
            while (true)
            {
                Thread.Sleep(int.MaxValue);
            }
        }
    }
}