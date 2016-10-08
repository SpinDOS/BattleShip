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
            for(int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                {
                    var button = new ButtonWithSquareStatus {Name = "Btn_" + i + j};
                    Buttons.Children.Add(button);
                }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = e.OriginalSource as Button;
            if (button == null)
                return;
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


        public ButtonWithSquareStatus this[string btnName]
        {
            get
            {
                var children = Buttons.Children;
                foreach (var child in children)
                {
                    ButtonWithSquareStatus button = child as ButtonWithSquareStatus;
                    if (button != null && button.Name == btnName)
                        return button;
                }
                throw new AggregateException("Unknow problem with GraphicBattleField");
            }
        }
    }
}
