using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace roomfyChat
{
    class Program
    {
        private static Dictionary<long, string> registrationState = new Dictionary<long, string>();
        private static DataBaseContext dbContext = new DataBaseContext();

        internal static void Main(string[] args)
        {
            var client = new TelegramBotClient("8068346833:AAGLjXMkf6BJ3lMannkocLTasMTGIOCiy8M");
            client.StartReceiving(update,error);
            Console.WriteLine("Bot Starting");

            Console.ReadLine();
        }

        private static async Task update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;

            if (registrationState.ContainsKey(message.Chat.Id))
            {
                await Registration(botClient, message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery);
            }
            else
            {
                if (message?.Text != null)
                {
                    Console.WriteLine($"user: {message.Chat.Id} {message.Chat.FirstName ?? "No Name"} : {message.Text}");

                    if (message.Text.ToLower().Contains("start"))
                    {
                        await botClient.SendMessage(message.Chat.Id, "Привіт👋\nЯ Roomfy Chat і я готовий вам допомгти знайти нових друзів");

                        dbContext.SearchUserRegistration(message.Chat.Id.ToString());

                        var keyBord = new ReplyKeyboardMarkup(new[]
                        {
                            new KeyboardButton[] {"Так!"}
                        })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = false
                        };

                        await botClient.SendMessage(message.Chat.Id, "Готовий розпочати реєстрацію?", replyMarkup: keyBord);
                    }
                    else if (message.Text.Contains("Так!") && dbContext.searchResult)
                    {
                        await StartRegistration(botClient, message);

                        Console.WriteLine("реєстрація");
                    }
                }
            }
        }

        private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            try
            {
                // Получаем данные из кнопки
                var callbackData = callbackQuery.Data;

                // Обрабатываем выбор области
                string regionName = callbackData switch
                {
                    "poltava" => "Полтавська область",
                    "kyiv" => "Київська область",
                    "kharkiv" => "Харківська область",
                    "lviv" => "Львівська область",
                    "vinnytsia" => "Вінницька область",
                    _ => "Невідома область"
                };

                // Отправляем ответ пользователю
                await botClient.SendMessage(callbackQuery.Message.Chat.Id, $"Ви обрали {regionName}!");

                // Отвечаем на callbackQuery (новый способ)
                await botClient.AnswerCallbackQuery(callbackQuery.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при обробці callbackQuery: {ex.Message}");
            }
        }


        private static async Task error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private static async Task StartRegistration(ITelegramBotClient botClient, Message message)
        {
            registrationState[message.Chat.Id] = "oblast";

            await botClient.SendMessage(message.Chat.Id, "Реєстарція розпочата");
        }

        private static async Task Registration(ITelegramBotClient botClient, Message message)
        {
            if (registrationState.TryGetValue(message.Chat.Id, out string value))
            {
                if (value == "oblast")
                {
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Полтавська", "poltava"),
                            InlineKeyboardButton.WithCallbackData("Київська", "kyiv")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Харківська", "kharkiv"),
                            InlineKeyboardButton.WithCallbackData("Львівська", "lviv")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Вінницька", "vinnytsia")
                        }
                    });

                    await botClient.SendMessage(message.Chat.Id,
                        "Оберіть область у якій хочеш позпочати анонімні знайомства: ",
                        replyMarkup: inlineKeyboard);
                }
            }
        }
    }
}