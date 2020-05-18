using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public static class HelloTimer
    {
        [FunctionName("HelloTimer")]
        public static async Task RunAsync([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, [DurableClient] IDurableOrchestrationClient client, ILogger log)
        {
            string instanceId = await client.StartNewAsync("HelloOrchestrator", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
