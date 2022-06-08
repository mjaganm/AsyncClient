

namespace mjaganm.AsyncClient
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class AsyncClient
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var host = new HostBuilder().ConfigureServices(services =>
            {
                services.AddHttpClient();
                services.AddTransient<AsyncClient>();
            })
            .Build();

            AsyncClient client  = host.Services.GetRequiredService<AsyncClient>();
            await client.RunGetRequests();
        }

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient httpClient;
        private int maxDegreeOfParalleism = 40;
        private readonly int singleLoop = 20000;

        public Queue<string> StorageQueue = new Queue<string>();

        public AsyncClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            httpClient = _httpClientFactory.CreateClient("AsyncClient");
        }

        public async Task RunGetRequests()
        {
            int count = 1;
            while (true)
            {
                maxDegreeOfParalleism = count * Environment.ProcessorCount;
                Console.WriteLine($"Max Degree of Parallelism: {maxDegreeOfParalleism}");

                DateTime currentTime = DateTime.UtcNow;

                await RunSingleLoop();

                DateTime endTime = DateTime.UtcNow;

                int numOfRequests = StorageQueue.Count;
                Console.WriteLine($"Num of successful requests: {StorageQueue.Count}");

                // Remove Queue contents
                while (StorageQueue.Count > 0)
                {
                    Console.WriteLine(StorageQueue.Dequeue());
                }

                int executionInSeconds =  (int)(endTime - currentTime).TotalSeconds;
                // numOfRequests = this.singleLoop * this.maxDegreeOfParalleism;

                Console.WriteLine();
                Console.WriteLine($"Waiting before starting another loop of AsyncRequests: {numOfRequests} ");
                Console.WriteLine($"Current RPS is: {numOfRequests / executionInSeconds}");
                await Task.Delay(10000);

                Console.WriteLine($"Starting Next set of Async Client requests");

                count++;
                if (count == 20)
                {
                    count = 1;
                }
            }
        }

        public async Task RunSingleLoop()
        {
            for (int i = 0; i < this.singleLoop / this.maxDegreeOfParalleism; i++)
            {
                Task<string>[] responses = new Task<string>[this.maxDegreeOfParalleism];
                for (int j = 0; j < this.maxDegreeOfParalleism; j++)
                {
                    responses[j] = this.GetRequest();
                }

                await Task.WhenAll(responses);

                for (int j = 0; j < this.maxDegreeOfParalleism; j++)
                {
                    this.StorageQueue.Enqueue(responses[j].Result);
                }
            }

            return;
        }

        public async Task<string> GetRequest()
        {            
            var httpResponseMessage = await this.httpClient.GetAsync(
                "https://localhost:5001/weatherforecast");

            string response = string.Empty;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                response = await httpResponseMessage.Content.ReadAsStringAsync();
            }

            return response;
        }
    }
}
