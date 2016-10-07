using System;
using System.Windows;
using System.Windows.Controls;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for BattleField.xaml
    /// </summary>
    public partial class GraphicBattleField : UserControl
    {
        public event EventHandler<SquareEventArgs> Square_Clicked;
        public GraphicBattleField()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = e.OriginalSource as Button;
            if (button == null)
                return;
            button.IsEnabled = false;
            string s = button.Name; //Btn_XY
            byte x = (byte)(s[4] - '0');
            byte y = (byte)(s[5] - '0');
            e.Handled = true;
            Square_Clicked?.Invoke(this, new SquareEventArgs(new Square(x, y), e));
        }

        public ButtonWithSquareStatus this[byte x, byte y] => this[new Square(x, y)];
        public ButtonWithSquareStatus this[Square square]
        {
            get
            {
                string name = "Btn_" + square.X + square.Y;
                return this[name];
            }
        }


        public ButtonWithSquareStatus this[string btnName] => (ButtonWithSquareStatus)this.FindName(btnName);
    }
}
