using TicTacToeServis.MessageBrocker;
using RabbitMQ.Client;

namespace TicTacToeServis
{
    internal class Program
    {
        private static RabbitService rabbitService;

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Service starting...");
            Console.ResetColor();

            var factory = new ConnectionFactory { HostName = "localhost" };
            var connectoin = await factory.CreateConnectionAsync();
            var channel = await connectoin.CreateChannelAsync();

            rabbitService = new RabbitService(channel);
            await rabbitService.ConsumeMessage("Roomfy.TicTacToe.SendToSevice");

            Console.ReadLine();
        }
    }
}
