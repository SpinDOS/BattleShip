using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Contains humanlike logic for next shot
    /// </summary>
    public sealed class LogicalPlayerSimulator : SimulatedPlayer
    {
        // square of last hurt
        private Square? LastHurt = null;

        /// <summary>
        /// Initialize field with random ships
        /// </summary>
        public LogicalPlayerSimulator() :
            this(new MyBattleField(BattlefieldExtensions.RandomizeSquares())) { }

        /// <summary>
        /// Initialize field with param squares
        /// </summary>
        /// <param name="myField">squares of ships</param>
        public LogicalPlayerSimulator(MyBattleField myField) : base(myField)
        { }

        /// <summary>
        /// Marks last shot with status
        /// </summary>
        public override void GetReportOfMyShot(Square square, SquareStatus status)
        { // save last hurt to LastHurt
            switch (status)
            {
                case SquareStatus.Dead:
                    LastHurt = null;
                    break;
                case SquareStatus.Hurt:
                    LastHurt = square;
                    break;
            }

            // anyway change state of field
            base.GetReportOfMyShot(square, status);
        }

        /// <summary>
        /// Generate next shot by logic
        /// </summary>
        public override Square GetNextShot()
        {
            // if not my turn
            if (!myTurn.HasValue || !myTurn.Value)
                throw new AggregateException("Can not shot now");

            Thread.Sleep(800);
            Random rnd = new Random();
            Square square;

            // if no info of any ship i will just random next shot
            if (LastHurt == null)
                while (true)
                {
                    square = new Square((byte)rnd.Next(0, 10), (byte)rnd.Next(0, 10));
                    if (EnemyField[square] == SquareStatus.Empty)
                        return square;
                }

            // find ship of last hurt
            Ship ship = EnemyField.FindShipBySquare(LastHurt.Value);
            int direction;

            // if length == 1 then direction is any
            if (ship.Length == 1)
                // random direction while cant move by it
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

            direction = rnd.Next(2); // will be move to start or end

            // detect direciotn of ship
            if (ship.Start.Y == ship.End.Y) // vertical
            {
                // 0 - up, 1 - down
                switch (direction)
                {
                    case 0: // move up
                        if (ship.Start.X == 0)
                            goto case 1; // if cant go up then go down
                        square = new Square((byte)(ship.Start.X - 1), ship.Start.Y);
                        if (EnemyField[square] != SquareStatus.Empty)
                            goto case 1; // if cant go up then go down
                        return square;
                    case 1: // move down
                        if (ship.End.X == 9)
                            goto case 0; // if cant go down then go up
                        square = new Square((byte)(ship.End.X + 1), ship.End.Y);
                        if (EnemyField[square] != SquareStatus.Empty)
                            goto case 0; // if cant go down then go up
                        return square;
                }
            }

            // horizontal
            // 0 - left, 1 - right
            switch (direction)
            {
                case 0: // move left
                    if (ship.Start.Y == 0)
                        goto default; // if cant go left then go right
                    square = new Square(ship.Start.X, (byte)(ship.Start.Y - 1));
                    if (EnemyField[square] != SquareStatus.Empty)
                        goto default; // if cant go left then go right
                    return square;
                default: // case 1: move right
                    if (ship.Start.Y == 9)
                        goto case 0; // if cant go right then go left
                    square = new Square(ship.End.X, (byte)(ship.End.Y + 1));
                    if (EnemyField[square] != SquareStatus.Empty)
                        goto case 0; // if cant go right then go left
                    return square;
            }
        }
    }
}
