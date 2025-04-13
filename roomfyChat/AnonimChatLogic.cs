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

        public bool IsUserWaiting(Message message)
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

                Console.WriteLine("чат создан");
            }
        }

        public bool IsUserInChat(Message message)
        {
            var userId = message.Chat.Id;

            return dbRedis.HashExists("anonimChats", userId);
        }

        public async Task SendMessagePartner(ITelegramBotClient botClient, Message message)
        {
            var userId = message.Chat.Id;
            var partnerId = dbRedis.HashGet("anonimChats", userId);

            try
            {
                if (message.Type == MessageType.Text && message.Text.ToLower().Contains("/leavroom"))
                {
                    await LeavTheChat(botClient, message);
                    return;
                }

                switch (message.Type)
                {
                    case MessageType.Text:
                        await botClient.SendMessage((long)partnerId, message.Text ?? "Exeption");
                        Console.WriteLine($"сообщение отравлено от {message.Chat.LastName}: {message.Text}");

                        break;

                    case MessageType.Photo:
                        var photo = message.Photo[^1];

                        await botClient.SendPhoto((long)partnerId, InputFile.FromFileId(photo.FileId.ToString()));
                        Console.WriteLine($"сообщение отравлено от {message.Chat.LastName}: Photo");

                        break;

                    case MessageType.Voice:
                        var voice = message.Voice;

                        await botClient.SendVoice((long)partnerId, InputFile.FromFileId(voice.FileId.ToString()));
                        Console.WriteLine($"сообщение отравлено от {message.Chat.LastName}: Voice");

                        break;

                    case MessageType.Video:
                        var video = message.Video;

                        await botClient.SendVideo((long)partnerId, InputFile.FromFileId(video.FileId.ToString()));
                        Console.WriteLine($"сообщение отравлено от {message.Chat.LastName}: Video");

                        break;

                    case MessageType.Sticker:
                        var sticker = message.Sticker;

                        await botClient.SendSticker((long)partnerId, InputFile.FromFileId(sticker.FileId.ToString()));
                        Console.WriteLine($"сообщение отравлено от {message.Chat.LastName}: Sticker");

                        break;

                    case MessageType.Animation:
                        var gif = message.Animation;

                        await botClient.SendAnimation((long)partnerId, InputFile.FromFileId(gif.FileId.ToString()));
                        Console.WriteLine($"сообщение отравлено от {message.Chat.LastName}: GIF");

                        break;

                    default:
                        await botClient.SendMessage(userId, "Я не можу передавати такий тип файлу\n" +
                                                            "можу передати тільки: 1) фото;\n" +
                                                            "2) видео;\n" +
                                                            "3) голосові\n;" +
                                                            "4) стікери\n" +
                                                            "5) гифки");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private async Task LeavTheChat(ITelegramBotClient botClient, Message message)
        {
            var userId = message.Chat.Id;
            var partnerId = dbRedis.HashGet("anonimChats", userId);

            dbRedis.HashDelete("anonimChats", userId);
            dbRedis.HashDelete("anonimChats", partnerId);

            await botClient.SendMessage(userId, "Ви закінчили співрозмову");
            await botClient.SendMessage((long)partnerId, "Ваш співрозмовник закінчив розмову за вами");
        }
    }
}
