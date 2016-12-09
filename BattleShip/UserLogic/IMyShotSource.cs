using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    public interface IMyShotSource
    {
        /// <summary>
        /// Get shot from user
        /// </summary>
        Square GetMyShot();
    }
}
