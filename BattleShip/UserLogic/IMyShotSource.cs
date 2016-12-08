using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    interface IMyShotSource
    {
        Square GetMyShot();
    }
}
