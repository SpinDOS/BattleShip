using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class SimplePlayerSimulator : SimulatedPlayer
    {
        public SimplePlayerSimulator() : base(Field.RandomizeSquares()) { }

        protected override Square GenerateNextShot()
        {
            Thread.Sleep(800);
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                    if (EnemyField[new Square(i, j)] == SquareStatus.Empty)
                        return new Square(i, j);
            throw new AggregateException("No empty enemy squares");
        }
    }
}
