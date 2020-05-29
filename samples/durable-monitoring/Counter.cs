using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Company.Function
{
    public class Counter : ICounter
    {
        public int Value { get; set; }

        public void Add(int amount)
        {
            this.Value += amount;
        }

        [FunctionName(nameof(Counter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Counter>();
    }

    public interface ICounter
    {
        void Add(int amount);
    }
}