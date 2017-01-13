using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace BattleShip.DataLogic
{
    public enum BattleShipConnectionDisconnectReason
    {
        MeDisconnected,
        EnemyDisconnected,
        NetworkError,
    }

    public static class BattleShipConnectionDisconnectReasonExtension
    {
        public static BattleShipConnectionDisconnectReason ToBattleShipDisconnectReason(this DisconnectReason reason)
        {
            switch (reason)
            {
                case DisconnectReason.DisconnectCalled:
                case DisconnectReason.DisconnectPeerCalled:
                    return BattleShipConnectionDisconnectReason.MeDisconnected;
                case DisconnectReason.RemoteConnectionClose:
                    return BattleShipConnectionDisconnectReason.EnemyDisconnected;
                case DisconnectReason.ConnectionFailed:
                case DisconnectReason.SocketReceiveError:
                case DisconnectReason.SocketSendError:
                case DisconnectReason.Timeout:
                    return BattleShipConnectionDisconnectReason.NetworkError;
                default:
                    throw new AggregateException("Unknown disconnect reason of LitenetLib");
            }
        }
    }
}
