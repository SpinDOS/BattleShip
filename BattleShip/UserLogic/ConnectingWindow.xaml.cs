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

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for ConnectingWindow.xaml
    /// </summary>
    public partial class ConnectingWindow : Window
    {
        private bool changeByUser = true;
        private State CurrentState;
        public ConnectingWindow()
        {
            InitializeComponent();
        }

        // prevent not-digit input
        private void TxtLobbyId_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txtbox = sender as TextBox;
            if (txtbox == null)
                throw new AggregateException("This method must be used only for textbox");

            // prevent handling textchange of this method
            if (!changeByUser)
                return;


            var badChanges = new List<TextChange>();
            // get bad changes
            foreach (var textChange in e.Changes)
            {
                // handle only adding text
                if (textChange.AddedLength <= 0)
                    continue;
                // part of text that has been changed
                var change = txtbox.Text.Substring(textChange.Offset, textChange.AddedLength);

                // check every char in string
                foreach (var ch in change)
                {
                    if (!char.IsDigit(ch))
                    {
                        badChanges.Add(textChange);
                        break;
                    }
                }
            }

            // if only good changes, return
            if (!badChanges.Any())
                return;

            // to prevent handling change in next strings
            changeByUser = false;
            foreach (var badChange in badChanges)
            {
                // revert bad changes
                txtbox.Text = txtbox.Text.Remove(badChange.Offset, badChange.AddedLength);
            }
            // allow handling text change
            changeByUser = true;

            // get last change to set CaretIndex
            var last = e.Changes.Last();
            // if lastChange is good, set caretindex to end of change
            // if not - to start of change
            int maxIndex = badChanges.Contains(last) ? last.Offset : last.Offset + last.AddedLength;

            // if new text is shorter, set caterindex to end of text
            txtbox.CaretIndex = maxIndex > txtbox.Text.Length ? txtbox.Text.Length : maxIndex;
        }

        private void RandomOpponent_Checked(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Find opponent";
            TxtLobbyId.IsEnabled = false;
            TxtPassword.IsEnabled = false;
        }

        private void CreateLobby_Checked(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Create lobby";
            TxtLobbyId.IsEnabled = true;
            TxtPassword.IsEnabled = true;
        }

        private void ConnectLobby_Checked(object sender, RoutedEventArgs e)
        {
            MainButton.Content = "Connect lobby";
            TxtLobbyId.IsEnabled = true;
            TxtPassword.IsEnabled = true;
        }

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState == State.SearchNotStart)
            {
                MainButton.Content = "Cancel";
                IndeterminateProgressBar.Visibility = Visibility.Visible;
                this.Height = 270;
                RandomOpponent.IsEnabled = CreateLobby.IsEnabled = ConnectLobby.IsEnabled =
                    TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = false;
                if (RandomOpponent.IsChecked.Value)
                {
                    LabelInfo.Content = "Searching for opponent";
                    CurrentState = State.SearchingOpponent;
                }
                else if (CreateLobby.IsChecked.Value)
                {
                    this.Height = 290;
                    LabelInfo.Content = "Lobby created" + Environment.NewLine + "Waiting for opponent";
                    CurrentState = State.LobbyCreated;
                }
                else
                {
                    LabelInfo.Content = "Trying to connect to lobby";
                    CurrentState = State.TryingToConnectLobby;
                }
                return;
            }
            IndeterminateProgressBar.Visibility = Visibility.Hidden;
            this.Height = 210;
            CurrentState = State.SearchNotStart;
            RandomOpponent.IsEnabled = CreateLobby.IsEnabled = ConnectLobby.IsEnabled =
                    TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = true;
            if (RandomOpponent.IsChecked.Value)
                RandomOpponent_Checked(null, e);
            else if (CreateLobby.IsChecked.Value)
                CreateLobby_Checked(null, e);
            else 
                ConnectLobby_Checked(null, e);
        }

        private enum State
        {
            SearchNotStart,
            SearchingOpponent,
            LobbyCreated,
            TryingToConnectLobby
        }
    }
}
