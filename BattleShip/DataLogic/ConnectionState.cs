﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.DataLogic
{
    public enum ConnectionState : byte
    {
        GettingMyPublicIp,
        GettingInfoFromServer, 
        WaitingForOpponent, 
        WaitingForOpponentsIp,
        TryingToConnectP2P,
        Ready,
    }
}
