using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roomfyChat.MessageBrocker
{
    interface IRebbitService
    {
        Task ErrorQueue();
        Task SendMessage(string routingKey, string message);
        Task CnsumeMessageTicTacToe(string routingKey, string message);
        Task CnsumeMessageRockPaperSikers(string routingKey, string message);
        Task CnsumeMessageLetterByLetter(string routingKey, string message);
    }
}
