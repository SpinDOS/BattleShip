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
        protected BattleField.Identifier _id = new BattleField.Identifier();
        protected MyBattleField MyField { get; }
        protected EnemyBattleField EnemyField { get; }

        protected Player(MyBattleField myField)
        {
            if (myField == null)
                throw new ArgumentNullException(nameof(myField));
            myField.SetOwner(this, _id);
            MyField = MyField;
            EnemyField = new EnemyBattleField();
        }

        public bool IsGameEnded { get; protected set; } = false;

        public bool ConfirmId(BattleField.Identifier id) => ReferenceEquals(id, _id);
    }


    //public abstract class Player
    //{
    //    #region Properties

    //    public event EventHandler<ShotEventArgs> MyShot;
    //    public event EventHandler<ShotEventArgs> EnemysShot;
    //    public event EventHandler<ShotEventArgs> MySquareStatusChanged;
    //    public event EventHandler<ShotEventArgs> EnemySquareStatusChanged;
    //    public event EventHandler<Ship> MyShipDead;
    //    public event EventHandler<Ship> EnemyShipDead;
    //    public event EventHandler<bool> GameEnded;

    //    protected MyBattleField MyField = null;
    //    protected BattleField EnemyField = null;

    //    public byte MyShipsAlive { get; private set; } = 10;
    //    public byte EnemyShipsAlive { get; private set; } = 10;

    //    private volatile bool _isGameEnded = false;

    //    public bool IsGameEnded
    //    {
    //        get { return _isGameEnded; }
    //        private set { _isGameEnded = value; }
    //    }

    //    public bool? MyTurn { get; private set; }

    //    public bool Initialized { get; private set; }

    //    #endregion

    //    #region Initialization

    //    protected Player(ClearField clearField)
    //    {
    //        MyField = new MyBattleField(clearField);
    //        EnemyField = new BattleField();
    //        MySquareStatusChanged += (sender, args) =>
    //            MyField.SetStatusOfSquare(args.Square, args.SquareStatus);
    //        EnemySquareStatusChanged += (sender, args) =>
    //            EnemyField.SetStatusOfSquare(args.Square, args.SquareStatus);
    //        MyShot += (sender, args) => EnemySquareStatusChanged?.Invoke(sender, args);
    //        EnemysShot += (sender, args) => MySquareStatusChanged?.Invoke(sender, args);
    //        EnemyShipDead += (sender, ship) =>
    //        {
    //            foreach (var innerSquare in ship.InnerSquares()
    //                .Where(innerSquare => EnemyField[innerSquare] != SquareStatus.Dead))
    //                EnemySquareStatusChanged?.Invoke(this, new ShotEventArgs(innerSquare, SquareStatus.Dead));
    //            foreach (var nearSquare in ship.NearSquares()
    //                .Where(nearSquare => EnemyField[nearSquare] != SquareStatus.Miss))
    //                EnemySquareStatusChanged?.Invoke(this, new ShotEventArgs(nearSquare, SquareStatus.Miss));
    //            if (--EnemyShipsAlive == 0)
    //                EndGame(true);
    //        };
    //        MyShipDead += (sender, ship) =>
    //        {
    //            foreach (var innerSquare in ship.InnerSquares()
    //                .Where(innerSquare => MyField[innerSquare] != SquareStatus.Dead))
    //                MySquareStatusChanged?.Invoke(this, new ShotEventArgs(innerSquare, SquareStatus.Dead));
    //            foreach (var nearSquare in ship.NearSquares()
    //                .Where(nearSquare => MyField[nearSquare] != SquareStatus.Miss))
    //                MySquareStatusChanged?.Invoke(this, new ShotEventArgs(nearSquare, SquareStatus.Miss));
    //            if (--MyShipsAlive == 0)
    //                EndGame(false);
    //        };
    //        GameEnded += (sender, b) => 
    //        {
    //            MyTurn = null;
    //            IsGameEnded = true;
    //        };
    //    }

    //    protected void SetMeShotFirst(bool meFirst)
    //    {
    //        if (Initialized)
    //            throw new AggregateException("This method must be called only once");
    //        MyTurn = meFirst;
    //        Initialized = true;
    //    }

    //    #endregion

    //    #region My shot

    //    public Square GetMyNextShot()
    //    {
    //        if (IsGameEnded)
    //            throw new GameEndedException();
    //        if (!Initialized)
    //            throw new NotInitializedException();
    //        if (MyTurn == null || !MyTurn.Value)
    //            throw new AggregateException("I cannot shot now");
    //        Square square = GenerateNextShot();
    //        MyTurn = null;
    //        return square;
    //    }

    //    protected abstract Square GenerateNextShot();

    //    public void SetStatusOfMyShot(Square square, SquareStatus result)
    //    {
    //        if (IsGameEnded)
    //            throw new GameEndedException();
    //        if (!Initialized)
    //            throw new NotInitializedException();
    //        if (MyTurn != null)
    //            throw new AggregateException("I cannot receive result of my shot now!");
    //        if (result == SquareStatus.Empty || result == SquareStatus.Full)
    //            throw new ArgumentException(nameof(result) + " must be Miss or Hurt or Dead");

    //        MyTurn = result != SquareStatus.Miss;
    //        MyShot?.Invoke(this, new ShotEventArgs(square, result));
    //        if (result == SquareStatus.Dead)
    //            EnemyShipDead?.Invoke(this, EnemyField.FindShipBySquare(square));
    //    }

    //    #endregion

    //    #region Enemy shot

    //    public SquareStatus ShotFromEnemy(Square square)
    //    {
    //        if (IsGameEnded)
    //            throw new GameEndedException();
    //        if (!Initialized)
    //            throw new NotInitializedException();
    //        if (MyTurn == null || MyTurn.Value)
    //            throw new AggregateException("I cannot receive shot now");

    //        SquareStatus newStatus = MyField.GetResultOfShot(square);
    //        MyTurn = newStatus == SquareStatus.Miss;
    //        EnemysShot?.Invoke(this, new ShotEventArgs(square, newStatus));
    //        if (newStatus == SquareStatus.Dead)
    //            MyShipDead?.Invoke(this, MyField.FindShipBySquare(square));
    //        return newStatus;
    //    }

    //    #endregion

    //    protected void EndGame(bool win)
    //    {
    //        if (IsGameEnded)
    //            return;
    //        GameEnded?.Invoke(this, win);
    //    }

    //    public class GameEndedException : AggregateException
    //    {
    //        public GameEndedException() : base("Game ended")
    //        {
    //        }
    //    }

    //    public class NotInitializedException : AggregateException
    //    {
    //        public NotInitializedException() : base("You must initialize object by calling SetMeShotFirst")
    //        {
    //        }
    //    }

    //}
}
