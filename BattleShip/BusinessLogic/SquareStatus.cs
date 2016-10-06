using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.BusinessLogic
{
    public enum SquareStatus : byte
    {
        Empty = 0, //обязательно для инициализации Field
        Miss,
        Full,
        Hurt,
        Dead,
    }
}
