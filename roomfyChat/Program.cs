using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace roomfyChat
{
    class Program
    {
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

            if(message?.Text != null)
            {
                Console.WriteLine($"user: {message.Chat.Id} {message.Chat.FirstName ?? "No Name"} : {message.Text}");

                if (message.Text.ToLower().Contains("start"))
                {
                    await botClient.SendMessage(message.Chat.Id, "Привіт👋\nЯ Roomfy Chat і я готовий вам допомгти знайти нових друзів");

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
                else if (message.Text.ToLower().Contains("так!"))
                {
                    await botClient.SendMessage(message.Chat.Id, "Введіть місто у якому хочете шукати людей анонімно", replyMarkup: new ReplyKeyboardRemove());
                    Console.WriteLine("реєстрація");
                }
            }
        }

        private static async Task error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}