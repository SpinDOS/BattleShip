using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window, IGameInterface
    {
        public GameWindow()
        {
            InitializeComponent();
        }
        public event EventHandler<MessageEventArgs> MessageSend;
        public void MessageReceived(string message) { }
        public void EnemyDisconnect() { }
        public bool EnemyWantsToRestart()
        {
            return true;
        }
        public SquareStatus EnemyShot(Square square) { return SquareStatus.Dead;}
        public event EventHandler<Square> YourShot;
        public void ChangeStatusOfEnemySquare(Square square, SquareStatus status) { }
    }
}
