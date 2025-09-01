using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TicTacToeServis.MessageBrocker
{
    class RabbitService : IRabbitService
    {
        private readonly IChannel _channel;
        private readonly string exchange;
        private readonly string type;

        private TicTacToeGame ticTacToe = new();

        public RabbitService(IChannel channel)
        {
            _channel = channel;
            exchange = "RoomfyQueue";
            type = ExchangeType.Topic;
        }

        public async Task ConsumeMessage(string routingKey)
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                                type: type);

            var declareOk = await _channel.QueueDeclareAsync();
            var queueName = declareOk.QueueName;
            await _channel.QueueBindAsync(queue: queueName,
                                          exchange: exchange,
                                          routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel: _channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"resived message from main api: {message}");
                await ticTacToe.TicTacToe(message);
            };

            await _channel.BasicConsumeAsync(queueName,
                                             autoAck: true,
                                             consumer: consumer);
        }

        public async Task SendError(string errorMessage)
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                                type: type);

            var body = Encoding.UTF8.GetBytes(errorMessage);

            await _channel.BasicPublishAsync(exchange: exchange,
                                             routingKey: "Roomfy.TicTacToe.error",
                                             body: body);
        }

        public async Task SendMessage(string routingKey, string message)
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                                type: type);

            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(exchange: exchange,
                                             routingKey: routingKey,
                                             body: body);
        }
    }
}
