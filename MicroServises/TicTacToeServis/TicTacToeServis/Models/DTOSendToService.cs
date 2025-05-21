using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServis.Models
{
    class DTOSendToService
    {
        public long UserId { get; set; }
        public long? PartnerId { get; set; }
        public string[][]? ArrayPlayFild { get; set; }
        public string GameState { get; set; }
        public string? Message { get; set; }
        public int? WinerrSymbol { get; set; }
    }
}
