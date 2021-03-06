using System;
using System.Collections.Generic;
using System.Windows;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for CreatingWindow.xaml
    /// </summary>
    public partial class CreatingWindow : Window
    {
        public CreatingWindow()
        {
            InitializeComponent();
            GraphicField.Numbers.MouseEnter += (sender, args) => btnClear.Visibility = Visibility.Visible;
            GraphicField.Numbers.MouseLeave += (sender, args) =>
            {
                if (!btnClear.IsMouseOver)
                    btnClear.Visibility = Visibility.Collapsed;
            };
        }

        public event EventHandler<StartGameEventArgs> StartGameEvent;

        private void Field_Square_Clicked(object sender, SquareEventArgs e)
        {
            e.Handled = true;
            ButtonWithSquareStatus button = e.OriginalSource as ButtonWithSquareStatus;
            button.IsEnabled = true;
            button.SquareStatus = button.SquareStatus == SquareStatus.Empty
                ? SquareStatus.Full
                : SquareStatus.Empty;
        }

        private void btnRandom_Click(object sender, RoutedEventArgs e)
        {
            btnClear_Click(null, null);
            IEnumerable<Square> squares = BattlefieldExtensions.RandomizeSquares();
            foreach (var square in squares)
                GraphicField[square].SquareStatus = SquareStatus.Full;
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            MyBattleField field;
            try
            {
                field = new MyBattleField(GetActiveSquares());
            }
            catch (ArgumentException)
            {
                string message = "You must place these ships: " + Environment.NewLine +
                                 '\u2022' + " one 4-square ship" + Environment.NewLine +
                                 '\u2022' + " two 3-square ships" + Environment.NewLine +
                                 '\u2022' + " three 2-square ships" + Environment.NewLine +
                                 '\u2022' + " four 1-square ships" + Environment.NewLine +
                                 "Thay must not stay close to each other";
                MessageBox.Show(this, message, "Can not create field", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            StartGameEvent?.Invoke(this, new StartGameEventArgs(
                radioButtonPVP.IsChecked??false, field));
        }

        private IEnumerable<Square> GetActiveSquares()
        {
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                {
                    Square square = new Square(i, j);
                    if (GraphicField[square].SquareStatus == SquareStatus.Full)
                        yield return square;
                }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                    GraphicField[i, j].SquareStatus = SquareStatus.Empty;
        }
    }
}
