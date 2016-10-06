using System;
using System.Collections.Generic;
using System.Windows;
using BattleShip.BusinessLogic;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

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
            IEnumerable<Square> squares = Utils.RandomizeSquares();
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                    GraphicField[i, j].SquareStatus = SquareStatus.Empty;
            foreach (var square in squares)
                GraphicField[square].SquareStatus = SquareStatus.Full;
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Player player = null;
            try
            {
                player = new Player(GetActiveSquares());
            }
            catch (ArgumentException)
            {
                string message = "You must place these ships: " + Environment.NewLine +
                                 '\u2022' + " one 4-square ship" + Environment.NewLine +
                                 '\u2022' + " two 3-square ships" + Environment.NewLine +
                                 '\u2022' + " three 2-square ships" + Environment.NewLine +
                                 '\u2022' + " four 1-square ships" + Environment.NewLine + 
                                 "Thay must not stay close to each other";
                MessageBox.Show(message, "Can not create field", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("Ok");

            //throw new NotImplementedException();
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

    }
}
