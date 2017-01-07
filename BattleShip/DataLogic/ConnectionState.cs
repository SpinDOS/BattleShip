using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.DataLogic
{
    public enum ConnectionState
    {
        GettingMyPublicIp,
        GettingInfoFromServer, 
        WaitingForOpponent, 
        WaitingForOpponentsIP,
        TryingToConnectP2P,
        Ready,
    }
}
