using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using roomfyChat.Games;

namespace roomfyChat.MessageBrocker
{
    class RebbitService : IRebbitService
    {
        private static TicTacToe ticTacToe = new TicTacToe();

        private readonly IChannel _channel;
        private readonly string exchange;
        private readonly string type;

        public RebbitService(IChannel channel)
        {
            _channel = channel;
            exchange = "RoomfyQueue";
            type = ExchangeType.Topic;
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
        public async Task CnsumeMessageLetterByLetter(string routingKey)
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                                type: type);

            var declareOk = await _channel.QueueDeclareAsync();
            var queueName = declareOk.QueueName;
            await _channel.QueueBindAsync(queue: queueName,
                                          exchange: exchange,
                                          routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel: _channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                // mehtod for cheking update
                Console.WriteLine("server geting message from Tic Tac Toe");
            };

            await _channel.BasicConsumeAsync(queue: queueName,
                                             autoAck: true,
                                             consumer: consumer);
        }

        public async Task CnsumeMessageRockPaperSikers(string routingKey)
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                                type: type);

            var declareOk = await _channel.QueueDeclareAsync();
            var queueName = declareOk.QueueName;
            await _channel.QueueBindAsync(queue: queueName,
                                          exchange: exchange,
                                          routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel: _channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                // mehtod for cheking update
                Console.WriteLine("server geting message from Tic Tac Toe");
            };

            await _channel.BasicConsumeAsync(queue: queueName,
                                             autoAck: true,
                                             consumer: consumer);
        }

        public async Task CnsumeMessageTicTacToe(string routingKey)
        {
            await _channel.ExchangeDeclareAsync(exchange: exchange,
                                                type: type);

            var declareOk = await _channel.QueueDeclareAsync();
            var queueName = declareOk.QueueName;
            await _channel.QueueBindAsync(queue: queueName,
                                          exchange: exchange,
                                          routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel: _channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await ticTacToe.ProcesingMessage(message);
                Console.WriteLine("server geting message from Tic Tac Toe");
            };

            await _channel.BasicConsumeAsync(queue: queueName,
                                             autoAck: true,
                                             consumer: consumer);
        }
    }
}
