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
        private static Dictionary<long, int> userSlideIndex = new();
        private static string[] slides =
        {
            "Roomfy Chat - це місце, де ти можеш спілкуватися анонімно або знаходити нових друзів поруч, грати в ігри " +
                "та отримувати нагороди за активності",
            "Тепер давай поговоримо про систему накопичення балів: " +
                "у кожного користувача є дві гри, одна за замовчуванням, друга даєсться випадково. Граючи в ігри буде фіксованний " +
                "рахунок, який узгоджують гравці. Кількість перемог кожного гравця = кількості балів." +
                "Приклад: гравці грали до 5 перемог. Гравець 1: переміг 2 рази, гравець 2: переміг 3 рази, таким чином гравець 1 отримав 2 бали," +
                " гравець 2 отримав 3 бали за свої перемоги",
            "Також поговоримо про те як накопичуються бонуси:" +
                "Бонуси накопичуються так само як і бали, тільки бали даються за ігри, а бонуси нараховуються за час проведений у розмові." +
                "Тобто коли гравець починає розмову з новим користувачем, програма запускає таймер. За кожну хвилину розмови користувачам " +
                "буде нараховуватись 0,2 бонуси, таким чином якщо користувачи пророзмовляють 5 хвилин, вони отримають оддин бонус.",
            "Бонуси та бали потібні для того щоб було можна придбати додаткові ігри, яких нема у гравця",
            "Ти пройшов весь шлях! Дякую за увагу! Та сподіваюсь що вам стало більш зрозуміло, як користуватися моїм інтерфейсом"
        };

        private static RegistrationData registrationData = new RegistrationData();
        
        private static Message? newMessageId;

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

            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery);
                return;
            }

            if (registrationState.ContainsKey(message.Chat.Id))
            {
                await Registration(botClient, message);
            }
            else
            {
                var dbContext = new DataBaseContext();

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

                        if (message.Text.ToLower().Contains("так") && dbContext.searchResult)
                        {
                            await botClient.SendMessage(message.Chat.Id,
                                                        "Ти вже зареєстрований",
                                                        replyMarkup: new ReplyKeyboardRemove());

                            dbContext.CloseConection();

                            return;
                        }

                        await botClient.SendMessage(message.Chat.Id, "Готовий розпочати реєстрацію?", replyMarkup: keyBord);
                    }
                    else if (message.Text.ToLower().Contains("так") && dbContext.searchResult == false)
                    {
                        await StartRegistration(botClient, message);

                        dbContext.CloseConection();

                        Console.WriteLine("started registration");
                    }
                }
            }
        }

        private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            try
            {
                if (callbackQuery?.Message == null) return;

                var messageId = callbackQuery.Message.MessageId;
                var chatId = callbackQuery.Message.Chat.Id;
                var callbackData = callbackQuery.Data;
                var dbContext = new DataBaseContext();

                Console.WriteLine(callbackData);

                if (registrationState.TryGetValue(chatId, out string value))
                {
                    if (value == "oblast")
                    {
                        string regionName = callbackData switch
                        {
                            "poltava" => "Полтавська область",
                            "kyiv" => "Київська область",
                            "kharkiv" => "Харківська область",
                            "lviv" => "Львівська область",
                            "vinnytsia" => "Вінницька область",
                            _ => "Невідома область"
                        };

                        registrationState[chatId] = "readInfo";
                        registrationData.oblast = callbackData ?? "none";

                        await botClient.SendMessage(chatId, $"Ви обрали {regionName}!");

                        await botClient.DeleteMessage(chatId, messageId);

                        await Registration(botClient, callbackQuery.Message);
                    }
                    else if (value == "readInfo")
                    {
                        if (callbackData == "next")
                        {
                            if (!userSlideIndex.ContainsKey(chatId))
                                userSlideIndex[chatId] = 0;

                            userSlideIndex[chatId]++;

                            await SendSlides(botClient, chatId, newMessageId.MessageId);
                        }
                        else if (callbackData == "true")
                        {
                            registrationData.infoReaded = true;

                            Console.WriteLine($"user id: {registrationData.userId}" +
                                                $" oblast: {registrationData.oblast}" +
                                                $" info: {registrationData.infoReaded}");

                            dbContext.AddNewUser(registrationData);
                            dbContext.CloseConection();

                            await botClient.DeleteMessage(chatId, newMessageId.MessageId);
                            await botClient.SendMessage(chatId, "Дякую що прочитав мої правила та ідею");
                        }
                        else if (callbackData == "false")
                        {
                            registrationData.infoReaded = false;

                            await botClient.SendMessage(chatId, "Я вам через деякий час напам'ятаю прочитати мої правила та ідею в цілому");
                        }
                    }
                }

                await botClient.AnswerCallbackQuery(callbackQuery.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при обробці callbackQuery: {ex.Message}");
            }
        }

        private static async Task SendSlides(ITelegramBotClient botClient, long chatId, int messageId)
        {
            int index = userSlideIndex[chatId];

            if (index == 4)
            {
                var inlineKeyboard2 = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("дякую, що ознайомив мене з ціею інформаціею!", "true")
                    }
                });

                await botClient.EditMessageText(chatId, messageId, slides[index], replyMarkup: inlineKeyboard2);
            }
            else
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("далі", "next")
                    }
                });

                await botClient.EditMessageText(chatId, messageId, slides[index], replyMarkup: inlineKeyboard);
            }
        }

        private static async Task error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private static async Task StartRegistration(ITelegramBotClient botClient, Message message)
        {
            registrationState[message.Chat.Id] = "oblast";
            registrationData.userId = message.Chat.Id;

            await botClient.SendMessage(message.Chat.Id, "Реєстарція розпочата",  replyMarkup: new ReplyKeyboardRemove());

            await Registration(botClient, message);

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
                else if (value == "readInfo")
                {
                    userSlideIndex[message.Chat.Id] = 0;

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("далі", "next"),
                            InlineKeyboardButton.WithCallbackData("Я прочитаю піздніше", "false")
                        }
                    });

                    newMessageId = await botClient.SendMessage(message.Chat.Id,
                                                                slides[0],
                                                                replyMarkup: inlineKeyboard);
                }
            }
        }
    }
}