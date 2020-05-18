using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public static class HelloOrchestrator
    {
        private static Random random = new Random();

        [FunctionName("HelloOrchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            while (true)
            {
                var output = await context.CallActivityAsync<string>("HelloOrchestrator_Hello", context.InstanceId);
                outputs.Add(output);

                if (output == "stop")
                {
                    break;
                }

                var waitTill = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(60));
                await context.CreateTimer(waitTill, CancellationToken.None);
            }

            return outputs;
        }

        [FunctionName("HelloOrchestrator_Hello")]
        public static async Task<string> SayHelloAsync([ActivityTrigger] string id, [DurableClient] IDurableOrchestrationClient client, ILogger log)
        {
            var rand = random.Next(100);

            if (rand < 5)
            {
                log.LogInformation("Throw");
                throw new Exception("Exception!!");
            }
            if (rand < 10)
            {
                log.LogInformation("Terminate");
                await client.TerminateAsync(id, "Terminating");
                return "terminate";
            }
            else if (rand < 30)
            {
                log.LogInformation("Stop");
                return "stop";
            }
            else
            {
                return "continue";
            }
        }

        [FunctionName("HelloOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("HelloOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}