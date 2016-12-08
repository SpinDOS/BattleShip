using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class BattleField
    {
        private readonly SquareStatus[,] Squares = new SquareStatus[10, 10];
        private Identifier _ownerId = null;
        public event EventHandler<ShotEventArgs> SquareStatusChanged;

        protected BattleField(IEnumerable<Square> shipSquares)
        {
            if (shipSquares == null)
                throw new ArgumentNullException(nameof(shipSquares));
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                    Squares[i, j] = SquareStatus.Empty;
            foreach (var square in shipSquares)
                this[square] = SquareStatus.Full;
            if (!Validate(true))
                throw new ArgumentException("Bad squares");
            SquareStatusChanged += (sender, args) => this[args.Square] = args.SquareStatus;
        }

        public SquareStatus this[Square square]
        {
            get { return Squares[square.X, square.Y]; }
            private set { Squares[square.X, square.Y] = value; }
        }

        public byte ShipsAlive { get; private set; } = 10;

        public Player Owner { get; private set; }

        public void SetOwner(Player player, Identifier id)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (Owner != null)
                throw new AggregateException("This field already has owner");
            if (player.ConfirmId(id))
                throw new InvalidOperationException("This id does not belong to this player");
            Owner = player;
            _ownerId = id;
        }

        protected void SetStatus(Square square, SquareStatus status, Identifier ownerId)
        {
            if (ownerId == null)
                throw new ArgumentNullException(nameof(ownerId));
            if (ReferenceEquals(ownerId, _ownerId))
                throw new InvalidOperationException("Failed to verify ownerId");

            SquareStatus oldStatus = this[square];

            if (status == SquareStatus.Empty)
                throw new ArgumentException("Can't mark square with Empty");
            if (status == SquareStatus.Full)
                throw new ArgumentException("Can't mark square with Full");
            if (status == SquareStatus.Miss && oldStatus != SquareStatus.Empty)
                throw new ArgumentException("This square is not Empty");
            if (status == SquareStatus.Hurt && oldStatus != SquareStatus.Full && oldStatus != SquareStatus.Empty)
                throw new ArgumentException("This square is already has status");
            if (status == SquareStatus.Dead && oldStatus == SquareStatus.Miss)
                throw new ArgumentException("This square has status Miss");

            if (status == SquareStatus.Miss)
            {
                SquareStatusChanged(this, new ShotEventArgs(square, status));
                return;
            }
            if (ShipsAlive == 0)
                throw new AggregateException("No ships alive");

            Squares[square.X, square.Y] = SquareStatus.Hurt;
            bool success = Validate(false);
            Squares[square.X, square.Y] = oldStatus;
            if (!success)
                throw new ArgumentException("Cannot set this status of this square");

            SquareStatusChanged(this, new ShotEventArgs(square, SquareStatus.Hurt));
            if (status == SquareStatus.Dead)
                MarkShipAsDead(square);
        }

        public Ship FindShipBySquare(Square square)
        {
            SquareStatus[] notEmpty = new[] {SquareStatus.Full, SquareStatus.Hurt, SquareStatus.Dead};
            if (!notEmpty.Contains(Squares[square.X, square.Y]))
                throw new ArgumentException("This square is not a part of ship");

            Square start, end;
            int i = square.X, j = square.Y;

            if ((square.X > 0 && notEmpty.Contains(Squares[square.X - 1, square.Y])) ||
                (square.X < 9 && notEmpty.Contains(Squares[square.X + 1, square.Y]))) // vertical
            {
                for (i = square.X - 1; i >= 0 && notEmpty.Contains(Squares[i, j]); i--) // set start to top ship square
                {
                }
                start = new Square((byte) (i + 1), (byte) j);

                for (i = square.X + 1; i <= 9 && notEmpty.Contains(Squares[i, j]); i++) // set end to bottom ship square
                {
                }
                end = new Square((byte) (i - 1), (byte) j);
            }
            else // horizontal or one-square
            {
                for (j = square.Y - 1; j >= 0 && notEmpty.Contains(Squares[i, j]); j--) // set start to left ship square
                {
                }
                start = new Square((byte) i, (byte) (j + 1));

                for (j = square.Y + 1; j <= 9 && notEmpty.Contains(Squares[i, j]); j++) // set end to right ship square
                {
                }
                end = new Square((byte) i, (byte) (j - 1));
            }
            return new Ship(start, end);
        }

        private void MarkShipAsDead(Square square)
        {
            Ship ship = FindShipBySquare(square);
            foreach (Square sq in ship.InnerSquares())
                SquareStatusChanged(this, new ShotEventArgs(sq, SquareStatus.Dead));
            foreach (Square sq in ship.NearSquares().Where(s => this[s] != SquareStatus.Miss))
                SquareStatusChanged(this, new ShotEventArgs(sq, SquareStatus.Miss));
            ShipsAlive--;
        }

        private bool Validate(bool strict)
        {
            SquareStatus[] notShip = {SquareStatus.Empty, SquareStatus.Miss, };
            List<Ship> ships = new List<Ship>(10);
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                {
                    if (notShip.Contains(Squares[i,j]))
                        continue;
                    Square square = new Square(i, j);
                    if (ships.Any(s => s.IsShipContainsSquare(square)))
                        continue;
                    Ship ship = FindShipBySquare(square);
                    if (ships.Any(s => s.IsNearShip(ship)))
                        return false;
                    ships.Add(ship);
                }


            if (ships.Count > 10)
                return false;

            if (strict && ships.Count != 10)
                return false;

            int s1 = 0, s2 = 0, s3 = 0, s4 = 0;
            foreach (var ship in ships)
            {
                switch (ship.Length)
                {
                    case 4:
                        if (s4++ == 1)
                            return false;
                        break;
                    case 3:
                        if (s3++ == 2)
                            return false;
                        break;
                    case 2:
                        if (s2++ == 3)
                            return false;
                        break;
                    case 1:
                        if (s1++ == 4)
                            return false;
                        break;
                }
            }
            return true;
        }

        public class Identifier
        { /* empty class */ }
        
    }
}
