using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class LogicalPlayerSimulator : SimulatedPlayer
    {
        public LogicalPlayerSimulator() : base(ClearField.RandomizeSquares()) { }
        protected override Square GenerateNextShot()
        {
            Thread.Sleep(800);
            throw new NotImplementedException();
        }
    }
}
