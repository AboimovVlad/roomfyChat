using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace roomfyChat
{
    class AnonimChatLogic
    {
        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
        private static IDatabase dbRedis = redis.GetDatabase();

        private async Task AddUserInWaitingRoom(Message message, ITelegramBotClient botClient)
        {
            dbRedis.ListLeftPush("waitingRoom", message.Chat.Id);

            await botClient.SendMessage(message.Chat.Id, "Ви в кімнаті очікування");
            Console.WriteLine($"пользователь в комнате ожидания: {message.Chat.Id}");
        }

        public bool IsUserWiting(Message message)
        {
            var list = dbRedis.ListRange("waitingRoom");

            return list.Any(x => x == message.Chat.Id);
        }

        public async Task AddNewChat(ITelegramBotClient botClient, Message message)
        {
            if(dbRedis.ListLength("waitingRoom") == 0)
            {
                await AddUserInWaitingRoom(message, botClient);
            }
            else
            {
                var waitingUserId = dbRedis.ListLeftPop("waitingRoom");
                var userId = message.Chat.Id;

                if (waitingUserId.IsNullOrEmpty)
                {
                    await AddUserInWaitingRoom(message, botClient);
                    return;
                }

                dbRedis.HashSet("anonimChats", new HashEntry[]
                {
                    new HashEntry(userId, waitingUserId),
                    new HashEntry(waitingUserId, userId)
                });

                await botClient.SendMessage(userId, "Ви підключидися до чату, починайте розмовляти або грати в ігри");
                await botClient.SendMessage((long)waitingUserId, "Ви залишили кімнату очікування та доєналися до чату," +
                                                                    " починайте розмовляти або грати в ігри");
            }
        }
    }
}
