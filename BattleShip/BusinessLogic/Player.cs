using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class Player
    {
        #region Properties

        public event EventHandler<ShotEventArgs> MyShot;
        public event EventHandler<ShotEventArgs> EnemysShot;
        public event EventHandler<Ship> MyShipDead;
        public event EventHandler<Ship> EnemyShipDead;
        public event EventHandler<bool> GameEnded;

        protected MyBattleField MyField = null;
        protected BattleField EnemyField = null;

        public byte MyShipsAlive { get; private set; } = 10;
        public byte EnemyShipsAlive { get; private set; } = 10;

        private volatile bool _isGameEnded = false;

        public bool IsGameEnded {
            get { return _isGameEnded; }
            private set { _isGameEnded = value; }
        }

        public bool? MyTurn { get; private set; }

        public bool Initialized { get; private set; }

        #endregion

        #region Initialization

        protected Player(ClearField clearField)
        {
            MyField = new MyBattleField(clearField);
            EnemyField = new BattleField();
            MyShot += (sender, args) =>
            {
                EnemyField.SetStatusOfSquare(args.Square, args.SquareStatus);
            };
            EnemyShipDead += (sender, ship) =>
            {
                foreach (var innerSquare in ship.InnerSquares())
                    MyShot?.Invoke(this, new ShotEventArgs(innerSquare, SquareStatus.Dead));
                foreach (var nearSquare in ship.NearSquares()
                .Where(nearSquare => EnemyField[nearSquare] != SquareStatus.Miss))
                    MyShot?.Invoke(this, new ShotEventArgs(nearSquare, SquareStatus.Miss));
                if (--EnemyShipsAlive == 0)
                    EndGame(true);
            };
            EnemysShot += (sender, args) =>
            {
                MyField.SetStatusOfSquare(args.Square, args.SquareStatus);
            };
            MyShipDead += (sender, ship) =>
            {
                foreach (var innerSquare in ship.InnerSquares())
                    EnemysShot?.Invoke(this, new ShotEventArgs(innerSquare, SquareStatus.Dead));
                foreach (var nearSquare in ship.NearSquares()
                .Where(nearSquare => MyField[nearSquare] != SquareStatus.Miss))
                    EnemysShot?.Invoke(this, new ShotEventArgs(nearSquare, SquareStatus.Miss));
                if (--MyShipsAlive == 0)
                    EndGame(false);
            };
            GameEnded += (sender, b) =>
            {
                MyTurn = null;
                IsGameEnded = true;
            };
        }

        protected void SetMeShotFirst(bool meFirst)
        {
            if (Initialized)
                throw new AggregateException("This method must be called only once");
            MyTurn = meFirst;
            Initialized = true;
        }

        #endregion

        #region My shot

        public Square GetMyNextShot()
        {
            if (IsGameEnded)
                throw new GameEndedException();
            if (!Initialized)
                throw new NotInitializedException();
            if (MyTurn == null || !MyTurn.Value)
                throw new AggregateException("I cannot shot now");
            Square square = GenerateNextShot();
            MyTurn = null;
            return square;
        }

        protected abstract Square GenerateNextShot();

        public void SetStatusOfMyShot(Square square, SquareStatus result)
        {
            if (IsGameEnded)
                throw new GameEndedException();
            if (!Initialized)
                throw new NotInitializedException();
            if (MyTurn != null)
                throw new AggregateException("I cannot receive result of my shot now!");
            if (result == SquareStatus.Empty || result == SquareStatus.Full)
                throw new ArgumentException(nameof(result) + " must be Miss or Hurt or Dead");

            MyTurn = result != SquareStatus.Miss;
            if (result == SquareStatus.Dead)
            {
                MyShot?.Invoke(this, new ShotEventArgs(square, SquareStatus.Hurt));
                EnemyShipDead?.Invoke(this, EnemyField.FindShipBySquare(square));
            }
            else
                MyShot?.Invoke(this, new ShotEventArgs(square, result));
        }

        #endregion

        #region Enemy shot

        public SquareStatus ShotFromEnemy(Square square)
        {
            if (IsGameEnded)
                throw new GameEndedException();
            if (!Initialized)
                throw new NotInitializedException();
            if (MyTurn == null || MyTurn.Value)
                throw new AggregateException("I cannot receive shot now");

            SquareStatus newStatus = MyField.GetResultOfShot(square);
            MyTurn = newStatus == SquareStatus.Miss;
            if (newStatus == SquareStatus.Dead)
            {
                EnemysShot?.Invoke(this, new ShotEventArgs(square, SquareStatus.Hurt));
                MyShipDead?.Invoke(this, MyField.FindShipBySquare(square));
            }
            else
                EnemysShot?.Invoke(this, new ShotEventArgs(square, newStatus));
            return newStatus;
        }

        #endregion

        protected void EndGame(bool win)
        {
            if (IsGameEnded)
                return;
            GameEnded?.Invoke(this, win);
        }

        public class GameEndedException : AggregateException
        { public GameEndedException() : base("Game ended") { } }
        public class NotInitializedException : AggregateException
        { public NotInitializedException() : base("You must initialize object by calling SetMeShotFirst") { } }

    }
}
