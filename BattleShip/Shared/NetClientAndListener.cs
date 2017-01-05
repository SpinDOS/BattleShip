using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace BattleShip.Shared
{
    /// <summary>
    /// Incaplulate NetClient and EventBasedNetListener
    /// </summary>
    public class NetClientAndListener
    {
        public NetClient Client { get; }
        public EventBasedNetListener Listener { get; }

        public NetClientAndListener(NetClient client, EventBasedNetListener listener)
        {
            Client = client;
            Listener = listener;
        }
    }
}
