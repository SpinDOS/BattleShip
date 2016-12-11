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
        /// <summary>
        /// Initialize field with random ships
        /// </summary>
        public MyRandomPlayerSimulator() : 
            this(new MyBattleField(BattlefieldExtensions.RandomizeSquares())) { }

        /// <summary>
        /// Initialize field with param squares
        /// </summary>
        public MyRandomPlayerSimulator(MyBattleField myField) : base(myField) { }

        /// <summary>
        /// Generate next shot by random
        /// </summary>
        public override Square GetNextShot()
        {
            Thread.Sleep(800);

            var squares = EnemyField.GetEmptySquares().ToArray();

            // if empty
            if (squares.Length == 0)
                throw new AggregateException("No empty squares");

            // return random empty square
            return squares[new Random().Next(squares.Length)];
        }
    }
}
