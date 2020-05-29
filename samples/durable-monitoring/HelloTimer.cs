using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public static class HelloTimer
    {
        private static Random random = new Random();
        [FunctionName("HelloTimer")]
        public static async Task RunAsync([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, [DurableClient] IDurableClient client, ILogger log)
        {
            string instanceId = await client.StartNewAsync("HelloOrchestrator", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var tasks = Enumerable.Range(1, 100)
                .Select(i => new { index = i, sortKey = random.Next() })
                .OrderBy(i => i.sortKey)
                .Take(10)
                .Select(i => new EntityId("Counter", i.index.ToString()))
                .Select(id => client.SignalEntityAsync<ICounter>(id, proxy => proxy.Add(1)))
                .ToList();
            
            await Task.WhenAll(tasks);
        }
    }
}
