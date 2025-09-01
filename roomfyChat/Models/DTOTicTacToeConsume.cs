using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roomfyChat.Models
{
    class DTOTicTacToeConsume
    {
        public long UserId { get; set; }
        public long? PartnerId { get; set; }
        public string[][]? ArrayPlayFild { get; set; }
        public string GameState { get; set; }
        public string? Message { get; set; }
        public string? WinerrSymbol { get; set; }
    }
}
