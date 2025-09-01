using roomfyChat.Games;
using roomfyChat.MessageBrocker;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using RabbitMQ.Client;

namespace roomfyChat
{
    class Program
    {
        private static Dictionary<long, string> registrationState = new Dictionary<long, string>();
        private static Dictionary<long, int> userSlideIndex = new();
        private static string[] slides =
        {
            "1️⃣ Roomfy Chat — що це? " +
                "Roomfy Chat — це місце, де ти можеш: • Спілкуватися анонімно • Знаходити нових друзів поруч" +
                " • Грати в ігри 🕹 • Отримувати нагороди за активність 🎁",
            "2️⃣ Система балів (рейтинг гравців)\r\n\r\nТепер поговоримо про систему накопичення балів:\r\n\r\n" +
                "• У кожного гравця є дві гри — одна за замовчуванням, інша — випадкова 🎲\r\n" +
                "• Після гри обидва гравці узгоджують рахунок\r\n• Кількість перемог = кількість балів\r\n\r\n📌" +
                " Приклад:\r\nГравці домовились грати до 5 перемог.\r\n▪️" +
                " Гравець 1: 2 перемоги\r\n▪️ Гравець 2: 3 перемоги\r\n➡️ Гравець 1 отримує 2 бали, гравець 2 — 3 бали",
            "3️⃣ Як працюють бонуси?\r\n\r\nБонуси нараховуються за розмову з новими людьми 🗣\r\n\r\n🕒" +
                " Коли гравці починають спілкування, запускається таймер.\r\nКожна хвилина розмови = 0.2 бонуси\r\nНаприклад," +
                " 5 хвилин → 1 бонус\r\n\r\n🎯 Бонуси та бали можна обмінювати на: • Нові ігри \U0001f9e9\r\n• Ексклюзивний контент",
            "4️⃣ Ти пройшов весь шлях!\r\n\r\nДякую за увагу 🙏\r\nСподіваюсь, тепер тобі більш зрозуміло, як користуватися" +
                " Roomfy Chat 💬\r\nУспіхів та приємного спілкування! ✨\r\n\r\n"
        };
        private static ReplyKeyboardMarkup menuMarkup = new ReplyKeyboardMarkup(new[]
                            {
                                new KeyboardButton[] {"Моя статистика"},
                                new KeyboardButton[] {"Розпочати чат", "Магазин"}
                            })
                            {
                                ResizeKeyboard = true,
                                OneTimeKeyboard = false
                            };

        private static RegistrationData registrationData = new RegistrationData();
        private static AnonimChatLogic anonimChat = new AnonimChatLogic();
        private static TicTacToe ticTacToe;
        private static RebbitService rebbitService;
        
        private static Message? newMessageId;

        internal static async Task Main(string[] args)
        {
            var client = new TelegramBotClient("8068346833:AAGLjXMkf6BJ3lMannkocLTasMTGIOCiy8M");
            client.StartReceiving(update,error);
            Console.WriteLine("Bot Starting");

            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            rebbitService = new RebbitService(channel);
            ticTacToe = new TicTacToe(rebbitService);

            await rebbitService.ErrorQueue();
            await rebbitService.CnsumeMessageTicTacToe(routingKey: "Roomfy.TicTacToe.ConsumeMessage");

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
            else if (anonimChat.IsUserWaiting(message))
            {
                if (message.Text.ToLower().Contains("leavwaiting"))
                {
                    await anonimChat.LeavWaitingRoom(botClient, message);
                    return;
                }
                
                await botClient.SendMessage(message.Chat.Id, "Ти в кімнаті очікування /leavWaiting");
            }
            else if (anonimChat.IsUserInChat(message))
            {
                await anonimChat.SendMessagePartner(botClient, message);
            }
            else
            {
                var dbContext = new DataBaseContext();

                dbContext.SearchUserRegistration(message.Chat.Id.ToString());

                if (message?.Text != null)
                {
                    Console.WriteLine($"user: {message.Chat.Id} {message.Chat.FirstName ?? "No Name"} : {message.Text}");

                    if (message.Text.ToLower().Contains("start"))
                    {
                        await botClient.SendMessage(message.Chat.Id,
                                                    "Привіт👋\nЯ Roomfy Chat і я готовий вам допомгти знайти нових друзів");

                        if (dbContext.searchResult)
                        {
                            await botClient.SendMessage(message.Chat.Id,
                                                        "Ти вже зареєстрований",
                                                        replyMarkup: menuMarkup);

                            dbContext.CloseConection();

                            return;
                        }
                        else
                        {
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
                    }
                    else if (message.Text.ToLower().Contains("так") && dbContext.searchResult == false)
                    {
                        await StartRegistration(botClient, message);

                        dbContext.CloseConection();

                        Console.WriteLine("started registration");
                    }
                    else if (message.Text.ToLower().Contains("розпочати чат"))
                    {
                        await anonimChat.AddNewChat(botClient, message);
                    }
                    else
                    {
                        await botClient.SendMessage(message.Chat.Id, "Я не розумію такої команди");
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

                if (registrationState.TryGetValue(chatId, out string? value))
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

                            registrationState.Remove(chatId);

                            await botClient.DeleteMessage(chatId, newMessageId.MessageId);
                            await botClient.SendMessage(chatId,
                                                        "Дякую що прочитав мої правила та ідею",
                                                        replyMarkup: menuMarkup);
                        }
                        else if (callbackData == "false")
                        {
                            registrationData.infoReaded = false;

                            dbContext.AddNewUser(registrationData);
                            dbContext.CloseConection();

                            registrationState.Remove(chatId);

                            await botClient.DeleteMessage(chatId, newMessageId.MessageId);
                            await botClient.SendMessage(chatId,
                                                        "Я вам через деякий час напам'ятаю прочитати" +
                                                        " мої правила та ідею в цілому",
                                                        replyMarkup: menuMarkup);
                        }
                    }
                }
                else if (callbackData == "dontTicTacToe")
                {
                    await ticTacToe.RefusalForGame(botClient, chatId);
                }
                else if (callbackData == "startTicTacToe")
                {
                    await ticTacToe.StartTicTacToe(botClient, callbackQuery.Message);
                }
                else if (ticTacToe.IsUserInGame(chatId))
                {
                    await ticTacToe.HandleWalk(botClient, callbackData, chatId);
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

            if (index == 3)
            {
                var inlineKeyboard2 = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("дякую, що ознайомив мене", "true")
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

        private static Task error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
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
            if (registrationState.TryGetValue(message.Chat.Id, out string? value))
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