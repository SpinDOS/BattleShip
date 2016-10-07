using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class PVEGame : Game
    {
        private Player enemy = new MyRandomPlayerSimulator();
        public PVEGame(Field field, IGameInterface userGameInterface)
            : base(field, userGameInterface) { }
    }
}
