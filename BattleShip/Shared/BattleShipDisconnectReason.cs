using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Shared
{
    public enum BattleShipDisconnectReason : byte
    {
        NetworkError,
        DisconnectCalled,
        OpponentDisconnectCalled,
        Timeout,
    }
}
