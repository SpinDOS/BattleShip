﻿using System;
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
        /// <summary>
        /// Initialize field with random ships
        /// </summary>
        public SimplePlayerSimulator() :
            this(new MyBattleField(BattlefieldExtensions.RandomizeSquares())) { }

        /// <summary>
        /// Initialize field with param squares
        /// </summary>
        public SimplePlayerSimulator(MyBattleField myField) : base(myField) { }

        /// <summary>
        /// Generate next shot by the most simple algorithm
        /// </summary>
        public override Square GetNextShot()
        {
            Thread.Sleep(800);
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                    if (EnemyField[new Square(i, j)] == SquareStatus.Empty)
                        return new Square(i, j);
            throw new GameStateException("No empty enemy squares");
        }
    }
}
