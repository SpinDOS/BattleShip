using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.BusinessLogic
{
    public sealed class MyRandomPlayerSimulator : Player
    {
        public MyRandomPlayerSimulator() : base(Field.RandomizeSquares())
        {
        }
    }
}
