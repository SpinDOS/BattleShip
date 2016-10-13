using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class MyRandomPlayerSimulator : SimulatedPlayer
    {
        public MyRandomPlayerSimulator() : base(ClearField.RandomizeSquares()) { }

        protected override Square GenerateNextShot()
        {
            Thread.Sleep(800);
            Random rnd = new Random();
            while (true)
            {
                Square square = new Square((byte) rnd.Next(0,10), (byte) rnd.Next(0, 10));
                if (EnemyField[square] == SquareStatus.Empty)
                    return square;
            }
        }
    }
}
