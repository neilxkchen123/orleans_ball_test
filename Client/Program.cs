using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Interfaces;
using System.Net;
using Orleans.Configuration;
using System.Collections.Generic;
using Global;

namespace OrleansClient
{
    /// <summary>
    /// Orleans test silo client
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    await DoClientWork(client);
                }
                Console.ReadKey();

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            int attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "BallTestApp";
                        })
                        .ConfigureApplicationParts(parts => { 
                            parts.AddApplicationPart(typeof(IGridGrain).Assembly).WithReferences();
                            parts.AddApplicationPart(typeof(IBallGrain).Assembly).WithReferences();
                        })
                        .ConfigureLogging(logging => logging.AddConsole())
                        .Build();

                    await client.Connect();
                    Console.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }

            return client;
        }

        private static async Task DoClientWork(IClusterClient client)
        {
           
            List<IGridGrain> grid_list = new List<IGridGrain>();
            var tasks = new List<Task>();
            for (uint x_grid_index = 0; x_grid_index < Common.x_grid_num; x_grid_index++)
                for (uint y_grid_index = 0; y_grid_index < Common.y_grid_num; y_grid_index++)
                {
                    long grid_id = Common.GetGridId(x_grid_index, y_grid_index);
                    IGridGrain grid = client.GetGrain<IGridGrain>(grid_id);
                    grid_list.Add(grid);
                    tasks.Add(grid.Init());
                }
           
            await Task.WhenAll(tasks);
			
            while (true) {
                try
                {
                    var grid_tasks = new List<Task>();
                    foreach (var grid in grid_list)
                    {
                        grid_tasks.Add(grid.Move());
                    }
                    //await Task.WhenAll(grid_tasks);
                    await Task.Delay(TimeSpan.FromSeconds(0.05));

                }
                catch (Orleans.Transactions.OrleansTransactionAbortedException e)
                {
                    Console.WriteLine($"\n  worker: transaction exception: {e}");
                }
                catch (System.TimeoutException e)
                {
                    Console.WriteLine($"\n  worker: timeout exception: {e}");
                }
            }
        }
    }
}
