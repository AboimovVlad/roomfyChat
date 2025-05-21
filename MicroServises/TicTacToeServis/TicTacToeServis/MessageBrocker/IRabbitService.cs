using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServis.MessageBrocker
{
    interface IRabbitService
    {
        Task ConsumeMessage(string routingKey);
        Task SendMessage(string routingKey, string message);
        Task SendError(string errorMessage);
    }
}
