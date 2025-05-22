using roomfyChat.MessageBrocker;
using roomfyChat.Models;
using StackExchange.Redis;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace roomfyChat.Games
{
    class TicTacToe
    {
        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
        private static IDatabase dbRedis = redis.GetDatabase();

        private readonly RebbitService? _rebbitService;

        private static InlineKeyboardMarkup playingField = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("▪️", "0,0"),
                InlineKeyboardButton.WithCallbackData("▪️", "0,1"),
                InlineKeyboardButton.WithCallbackData("▪️", "0,2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("▪️", "1,0"),
                InlineKeyboardButton.WithCallbackData("▪️", "1,1"),
                InlineKeyboardButton.WithCallbackData("▪️", "1,2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("▪️", "2,0"),
                InlineKeyboardButton.WithCallbackData("▪️", "2,1"),
                InlineKeyboardButton.WithCallbackData("▪️", "2,2")
            }
        });

        public TicTacToe(RebbitService? rebbitService = null)
        {
            _rebbitService = rebbitService;
        }

        public bool IsUserInGame(long userId)
        {
            return dbRedis.HashExists(key: "TicTacToe",
                                      hashField: userId);
        }

        public async Task QwestionForStartGame(ITelegramBotClient botClient, Message message)
        {
            var userId = message.Chat.Id;
            var partnerId = (long)dbRedis.HashGet("anonimChats", userId);

            if (IsUserInGame(partnerId) && IsUserInGame(userId))
            {
                await botClient.SendMessage(userId, "Ви та ваш партнер, вже граєте в цю гру");
            }
            else
            {
                var inLineKebord = new InlineKeyboardMarkup(new[]
                {
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Розпочати гру!", "startTicTacToe"),
                            InlineKeyboardButton.WithCallbackData("Не зараз", "dontTicTacToe")
                        }
                });

                await botClient.SendMessage(partnerId, "ващ партнер пропонує зіграти в хрестики нулеки", replyMarkup: inLineKebord);
            }
        }

        public async Task RefusalForGame(ITelegramBotClient botClient, long userId)
        {
            var partnerId = (long)dbRedis.HashGet("anonimChats", userId);

            await botClient.SendMessage(partnerId, "Ваш партнер відказався, брати участь у цій грі.\n" +
                                                   "Оберіть іншу гру або просто продовжуйте розмовляти😉");
        }

        public async Task StartTicTacToe(ITelegramBotClient botClient, Message message)
        {
            var userId = message.Chat.Id;
            var partnerId = (long)dbRedis.HashGet("anonimChats", userId);

            dbRedis.HashSet("TicTacToe", new HashEntry[]
            {
                new HashEntry(userId, "walk"),
                new HashEntry(partnerId, "wait")
            });
            dbRedis.HashSet("TicTacToeRole", new HashEntry[]
            {
                new HashEntry(userId, "❌"),
                new HashEntry(partnerId, "⭕️")
            });

            await botClient.SendMessage(userId, "Гра розпочата! 🎮  \r\n" +
                                                "Ходить: ❌  \r\n" +
                                                "Ваш символ: ❌\r\n",
                                                replyMarkup: playingField);

            await botClient.SendMessage(partnerId, "Гра розпочата! 🎮  \r\n" +
                                                "Ходить: ❌  \r\n" +
                                                "Ваш символ: ⭕\r\n",
                                                replyMarkup: playingField);
        }

        public async Task HandleWalk(ITelegramBotClient botClient, string indexArray, long userId)
        {
            if (_rebbitService == null)
                throw new InvalidOperationException("Rabbit service not initialized");


            if (dbRedis.HashGet("TicTacToe", userId).ToString() == "wait")
            {
                await botClient.SendMessage(userId, "Зараз не ваш хід, зачекайте будьласка");
                return;
            }
            else
            {
                var message = new DTOTicTacToeSend
                {
                    UserId = userId,
                    Index = indexArray
                };

                var json = JsonSerializer.Serialize(message);

                await _rebbitService.SendMessage("Roomfy.TicTacToe.SendToSevice", json);
            }
        }

        public async Task ProcesingMessage(string message)
        {

        }
    }
}
