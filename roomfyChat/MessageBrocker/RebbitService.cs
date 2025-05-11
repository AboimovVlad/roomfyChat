using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace roomfyChat.MessageBrocker
{
    class RebbitService : IRebbitService
    {
        private readonly IChannel _channel;
        private readonly string exchange = "RoomfyQueue";
        private readonly string type = ExchangeType.Topic;

        public RebbitService(IChannel channel)
        {
            _channel = channel;
        }

        public async Task ErrorQueue()
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                               type: type);

            var queueDeclareOk = await _channel.QueueDeclareAsync();
            string queueName = queueDeclareOk.QueueName;
            await _channel.QueueBindAsync(queue: queueName,
                                          exchange: exchange,
                                          routingKey: "Roomfy.*.error");

            var consumer = new AsyncEventingBasicConsumer(channel: _channel);
            consumer.ReceivedAsync += (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error from {message}");
                Console.ResetColor();

                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queue: queueName,
                                       autoAck: true,
                                       consumer: consumer);
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
        public Task CnsumeMessageLetterByLetter(string routingKey, string message)
        {
            throw new NotImplementedException();
        }

        public Task CnsumeMessageRockPaperSikers(string routingKey, string message)
        {
            throw new NotImplementedException();
        }

        public Task CnsumeMessageTicTacToe(string routingKey, string message)
        {
            throw new NotImplementedException();
        }
    }
}
