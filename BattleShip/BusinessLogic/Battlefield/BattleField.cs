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
        // main field
        private readonly SquareStatus[,] Squares = new SquareStatus[10, 10];
        // id for identify owner
        private Identifier _ownerId = null;

        /// <summary>
        /// Trigger when any square status changes
        /// </summary>
        public event EventHandler<Shot> SquareStatusChanged;

        /// <param name="shipSquares">squares of active ships of field</param>
        protected BattleField(IEnumerable<Square> shipSquares)
        {
            if (shipSquares == null)
                throw new ArgumentNullException(nameof(shipSquares));
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                    Squares[i, j] = SquareStatus.Empty;
            foreach (var square in shipSquares)
                this[square] = SquareStatus.Full;

            // change status by event
            SquareStatusChanged += (sender, args) => this[args.Square] = args.SquareStatus;
        }

        public SquareStatus this[Square square]
        {
            get { return Squares[square.X, square.Y]; }
            private set { Squares[square.X, square.Y] = value; }
        }

        public byte ShipsAlive { get; private set; } = 10;

        public Player Owner { get; private set; }

        /// <summary>
        /// Set owner player before using
        /// </summary>
        /// <param name="player">Owner</param>
        /// <param name="id">His id</param>
        public void SetOwner(Player player, Identifier id)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (Owner != null)
                throw new AggregateException("This field already has owner");
            if (!player.ConfirmId(id)) // check if this id belongs to player
                throw new InvalidOperationException("This id does not belong to this player");
            Owner = player;
            _ownerId = id;
        }

        /// <summary>
        /// Change status of square (or ship if dead)
        /// </summary>
        /// <param name="square">square to change status</param>
        /// <param name="status">new status</param>
        /// <param name="ownerId">id to identify owner</param>
        protected void SetStatus(Square square, SquareStatus status, Identifier ownerId)
        {
            if (ownerId == null)
                throw new ArgumentNullException(nameof(ownerId));
            if (!ReferenceEquals(ownerId, _ownerId))
                throw new InvalidOperationException("Failed to verify ownerId");

            SquareStatus oldStatus = this[square];

            if (status == SquareStatus.Empty)
                throw new ArgumentException("Can't mark square with Empty");
            if (status == SquareStatus.Full)
                throw new ArgumentException("Can't mark square with Full");
            // Miss only for empty
            if (status == SquareStatus.Miss && oldStatus != SquareStatus.Empty)
                throw new ArgumentException("This square is not Empty");
            // Hurt only for full and empty
            if (status == SquareStatus.Hurt && oldStatus != SquareStatus.Full && oldStatus != SquareStatus.Empty)
                throw new ArgumentException("This square is already has status");
            // Dead for any except miss
            if (status == SquareStatus.Dead && oldStatus == SquareStatus.Miss)
                throw new ArgumentException("This square has status Miss");

            // if miss - mark and return
            if (status == SquareStatus.Miss)
            {
                SquareStatusChanged(this, new Shot(square, status));
                return;
            }

            if (ShipsAlive == 0)
                throw new AggregateException("No ships alive");

            // try to mark and validate
            Squares[square.X, square.Y] = SquareStatus.Hurt;
            bool success = Validate(false);
            Squares[square.X, square.Y] = oldStatus;

            // mark as hurt (and next dead if need)
            if (!success)
                throw new ArgumentException("Cannot set this status of this square");

            SquareStatusChanged(this, new Shot(square, SquareStatus.Hurt));
            if (status == SquareStatus.Dead)
                MarkShipAsDead(square);
        }

        /// <summary>
        /// Find ship by square that belong to id
        /// </summary>
        /// <param name="square">square of target ship</param>
        /// <returns></returns>
        public Ship FindShipBySquare(Square square)
        {
            // check for existance of ship
            SquareStatus[] notEmpty = new[] {SquareStatus.Full, SquareStatus.Hurt, SquareStatus.Dead};
            if (!notEmpty.Contains(Squares[square.X, square.Y]))
                throw new ArgumentException("This square is not a part of ship");

            Square start, end;
            int i = square.X, j = square.Y;

            // look for ship start and end
            if ((square.X > 0 && notEmpty.Contains(Squares[square.X - 1, square.Y])) ||
                (square.X < 9 && notEmpty.Contains(Squares[square.X + 1, square.Y]))) // vertical
            {
                for (i = square.X - 1; i >= 0 && notEmpty.Contains(Squares[i, j]); i--) 
                { // set start to prev of top ship square
                }
                start = new Square((byte) (i + 1), (byte) j);

                for (i = square.X + 1; i <= 9 && notEmpty.Contains(Squares[i, j]); i++) 
                { // set end to next of bottom ship square
                }
                end = new Square((byte) (i - 1), (byte) j);
            }
            else // horizontal or one-square
            {
                for (j = square.Y - 1; j >= 0 && notEmpty.Contains(Squares[i, j]); j--) 
                { // set start to prev of left ship square
                }
                start = new Square((byte) i, (byte) (j + 1));

                for (j = square.Y + 1; j <= 9 && notEmpty.Contains(Squares[i, j]); j++) 
                { // set end to next of right ship square
                }
                end = new Square((byte) i, (byte) (j - 1));
            }
            return new Ship(start, end);
        }

        /// <summary>
        /// Mark ship containing this square as dead and near squares
        /// </summary>
        /// <param name="square">square of target ship</param>
        private void MarkShipAsDead(Square square)
        {
            // target ship
            Ship ship = FindShipBySquare(square);
            // mark dead
            foreach (Square sq in ship.InnerSquares())
                SquareStatusChanged(this, new Shot(sq, SquareStatus.Dead));
            // mark miss
            foreach (Square sq in ship.NearSquares().Where(s => this[s] != SquareStatus.Miss))
                SquareStatusChanged(this, new Shot(sq, SquareStatus.Miss));

            ShipsAlive--;
        }

        /// <summary>
        /// Validate state of field for ship crossing and its count
        /// </summary>
        /// <param name="strict">true, if need check for 10 only ships (not less)</param>
        /// <returns></returns>
        protected bool Validate(bool strict)
        {
            SquareStatus[] notShip = {SquareStatus.Empty, SquareStatus.Miss, };
            List<Ship> ships = new List<Ship>(10);

            // add squares to ships
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                {
                    // is square is ship part
                    if (notShip.Contains(Squares[i,j]))
                        continue;

                    Square square = new Square(i, j);
                    // check if this square belongs to some ship
                    if (ships.Any(s => s.IsShipContainsSquare(square)))
                        continue;

                    Ship ship = FindShipBySquare(square);

                    // check ships for crossing
                    if (ships.Any(s => s.IsNearShip(ship)))
                        return false;
                    ships.Add(ship);
                }


            if (ships.Count > 10)
                return false;

            // need is strict
            if (strict && ships.Count != 10)
                return false;

            // check ship length count
            byte[] counts = new byte[4];
            foreach (var ship in ships)
                counts[ship.Length - 1]++;

            if (strict)
            {
                for (int i = 0; i < 4; i++)
                    if (counts[i] != 4 - i)
                        return false;
            }
            else
            {
                if (counts[3] > 1 || // 4-squared ship
                    counts[2] > 1 + 2 || //4sq ship + 2 3sq ships
                    counts[1] > 1 + 2 + 3 || //4sq ship + 2 3sq ships + 3 2sq ships
                    counts[0] > 1 + 2 + 3 + 4)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Empty class for identifying owner of battlefield
        /// </summary>
        public class Identifier
        { /* empty class */ }
        
    }
}
