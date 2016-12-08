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
        private Square? LastHurt = null;

        public LogicalPlayerSimulator(MyBattleField myField) :
            base(new MyBattleField(BattlefieldExtensions.RandomizeSquares())) { }

        public override void GetReportOfMyShot(Square square, SquareStatus status)
        {
            switch (status)
            {
                case SquareStatus.Dead:
                    LastHurt = null;
                    break;
                case SquareStatus.Hurt:
                    LastHurt = square;
                    break;
            }

            base.GetReportOfMyShot(square, status);
        }

        public override Square GetNextShot()
        {
            if (!myTurn.HasValue || !myTurn.Value)
                throw new AggregateException("Can not shot now");

            Thread.Sleep(800);
            Random rnd = new Random();
            Square square;

            if (LastHurt == null)
                while (true)
                {
                    square = new Square((byte)rnd.Next(0, 10), (byte)rnd.Next(0, 10));
                    if (EnemyField[square] == SquareStatus.Empty)
                        return square;
                }


            Ship ship = EnemyField.FindShipBySquare(LastHurt.Value);
            int direction;

            if (ship.Length == 1)
                while (true)
                {
                    square = LastHurt.Value;
                    direction = rnd.Next(4);
                    // 0 - up, 1 - down, 2 - left, 3 - right
                    switch (direction)
                    {
                        case 0:
                            if (LastHurt.Value.X > 0)
                                square = new Square((byte) (square.X - 1), square.Y);
                            break;
                        case 1:
                            if (LastHurt.Value.X < 9)
                                square = new Square((byte)(square.X + 1), square.Y);
                            break;
                        case 2:
                            if (LastHurt.Value.Y > 0)
                                square = new Square(square.X, (byte) (square.Y - 1));
                            break;
                        case 3:
                            if (LastHurt.Value.Y < 9)
                                square = new Square(square.X, (byte)(square.Y + 1));
                            break;
                    }
                    if (EnemyField[square] == SquareStatus.Empty)
                        return square;
                }

            direction = rnd.Next(2);

            if (ship.Start.Y == ship.End.Y) // vertical
            {
                // 0 - up, 1 - down
                switch (direction)
                {
                    case 0:
                        if (ship.Start.X == 0)
                            goto case 1;
                        square = new Square((byte)(ship.Start.X - 1), ship.Start.Y);
                        if (EnemyField[square] != SquareStatus.Empty)
                            goto case 1;
                        return square;
                    case 1:
                        if (ship.End.X == 9)
                            goto case 0;
                        square = new Square((byte)(ship.End.X + 1), ship.End.Y);
                        if (EnemyField[square] != SquareStatus.Empty)
                            goto case 0;
                        return square;
                }
            }

            // horizontal
            // 0 - left, 1 - right
            switch (direction)
            {
                case 0:
                    if (ship.Start.Y == 0)
                        goto default;
                    square = new Square(ship.Start.X, (byte)(ship.Start.Y - 1));
                    if (EnemyField[square] != SquareStatus.Empty)
                        goto default;
                    return square;
                default: // case 1: 
                    if (ship.Start.Y == 9)
                        goto case 0;
                    square = new Square(ship.End.X, (byte)(ship.End.Y + 1));
                    if (EnemyField[square] != SquareStatus.Empty)
                        goto case 0;
                    return square;
            }
        }
    }
}
