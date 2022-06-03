using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Linq;
using Uoc.Tfm.Uwp.Daw.Extensions;
using Uoc.Tfm.Uwp.Daw.Interfaces;
using Uoc.Tfm.Uwp.Daw.Services;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Uoc.Tfm.Uwp.Daw.Controls
{
    public sealed partial class TrackUserControl : UserControl, ControlInterface
    {
        private string trackName;

        private readonly HubConnection hubConnection;
        private readonly SignalRCommunicationService _service;
        internal Guid TrackId { get; private set; }
        internal string TrackName
        {
            get
            {
                return trackName;
            }
            set
            {
                trackName = value;
                TrackNameTextBlock.Text = trackName;
            }
        }
        internal bool IsPushButtonActive
        {
            get
            {
                return UploadButton.IsEnabled;
            }
            private set
            {

                UploadButton.IsEnabled = value;
                if (value)
                {
                    UploadButton.BorderBrush = 
                        new SolidColorBrush(Colors.DarkGreen);
                }
                else
                {
                    UploadButton.BorderBrush = 
                        new SolidColorBrush(Colors.LightGray);
                }
            }
        }

        internal event EventHandler TrackActionHandler;

        internal event EventHandler OnNameChanged;

        internal event EventHandler OnTrackDeleted;

        public TrackUserControl(
            Guid? trackNumber = null,
            string trackName = null,
            HubConnection connection = null,
            SignalRCommunicationService service = null)
        {
            this.InitializeComponent();
            this.hubConnection = connection;
            this._service = service;

            TrackId = trackNumber != null && trackNumber != default ? trackNumber.Value : Guid.NewGuid();
            TrackName = trackName != null ? trackName : "Track";
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {

            ShowPianoRoll();

            //////var ss = new MessageDialog("Test" + this.Parent.ToString(), "Testing");

            //////_ = ss.ShowAsync();


            ////var pianoControl = new PianoRollUserControl(TrackId);
            ////pianoControl.ScoreChanged += PianoControl_ScoreChanged;
            //////var gridControl = ((this.Parent as Control).Parent as Grid).Children.Where(x => x.GetType() == typeof(Grid));

            //////if (gridControl != null && gridControl.Any())
            //////{
            //////    if (gridControl.First() is Grid grid)
            //////    {
            //////        if (grid.Children.Count > 0)
            //////        {
            //////            grid.Children.RemoveAt(0);
            //////        }
            //////        grid.Children.Add(pianoControl);
            //////    }
            //////}
            //////;

            ////var grid = GetGrid();
            ////if (grid != null)
            ////{
            ////    if (grid.Children.Count > 0)
            ////    {
            ////        grid.Children.RemoveAt(0);
            ////    }
            ////    grid.Children.Add(pianoControl);
            ////}
        }

        private void PianoControl_ScoreChanged(object sender, EventArgs e)
        {
            IsPushButtonActive = hubConnection.State == HubConnectionState.Connected;

            if (IsPushButtonActive)
            {
                TrackActionHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("Do you want to remove this track?", "Delete track");

            dialog.Commands.Add(new UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new UICommand("No") { Id = 1 });

            dialog.DefaultCommandIndex = 1;
            dialog.CancelCommandIndex = 1;


            var result = await dialog.ShowAsync();

            if ((int)result.Id != 0)
            {
                return;
            }

            //var items = (App.Current as App)._items;
            //items.Remove(TrackId);
            //var song = (App.Current as App)._song;
            //var track = song.Tracks.FirstOrDefault(x => x.TrackId == TrackId);
            var track = this.GetTrack(TrackId);
            if (track != null)
            {
                this.GetCurrentSong().Tracks.Remove(track); // song.Tracks.Remove(track);

                OnTrackDeleted?.Invoke(sender, EventArgs.Empty);
            }

            var listBox = this.Parent as ListBox;
            var grid = GetGrid();
            grid.Children.Clear();
            listBox.Items.Remove(this);
        }

        private Grid GetGrid()
        {
            var parentGrid = (this.Parent as Control).Parent as Grid;
            var gridControl = parentGrid.Children.Where(x => x.GetType() == typeof(Grid));

            if (gridControl != null && gridControl.Any())
            {
                if (gridControl.First() is Grid grid)
                {
                    return grid;
                }
            }

            return null;
        }

        private void PushDataButton_Click(object sender, RoutedEventArgs e)
        {
            TrackActionHandler?.Invoke(this, EventArgs.Empty);

            UploadButton.IsEnabled = false;
        }

        private void TrackNameTextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //var textBlock = sender as TextBlock;

            //var grid = textBlock.Parent as Grid;

            //var textBox = grid.Children.FirstOrDefault(x => x.GetType() == typeof(TextBox)) as TextBox;


            //var tt = ((sender as TextBlock).Parent as Grid).Children.FirstOrDefault(x => x.GetType() == typeof(TextBox));
            //if (textBox != null)
            //{
                TrackNameTextBox.Visibility = Visibility.Visible;
                TrackNameTextBox.Text = TrackName;
                TrackNameTextBlock.Visibility = Visibility.Collapsed;
                TrackNameTextBox.Focus(FocusState.Keyboard);
                TrackNameTextBox.SelectAll();

        }

        private void TrackNameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                TrackName = TrackNameTextBox.Text;
                OnNameChanged?.Invoke(this, EventArgs.Empty);
                TrackNameTextBlock.Text = TrackName;
                TrackNameTextBlock.Visibility = Visibility.Visible;
                TrackNameTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void TrackNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TrackName = TrackNameTextBox.Text;
            OnNameChanged?.Invoke(this, EventArgs.Empty);
            TrackNameTextBlock.Text = TrackName;
            TrackNameTextBlock.Visibility = Visibility.Visible;
            TrackNameTextBox.Visibility = Visibility.Collapsed;
        }

        internal void TurnOnReceiveIcon()
        {
            RetrieveDataButton.IsEnabled = true;
            RetrieveDataButton.BorderBrush = new SolidColorBrush(Colors.DarkGreen);

        }

        private void RetrieveDataButton_Click(object sender, RoutedEventArgs e)
        {
            var receivedTracks = this.GetReceivedTracks();

            if (receivedTracks == null || !receivedTracks.Any())
            {
                //RetrieveDataButton.IsEnabled = false;
                return;

            }

            var lastReceived = receivedTracks.Last();

            var currentTrack = this.GetTrack(lastReceived.TrackId);

            if (currentTrack != null)
            {
                currentTrack.Name = lastReceived.Name;
                currentTrack.Scores = lastReceived.Scores;
                this.trackName = currentTrack.Name;

                ShowPianoRoll();
            }

            receivedTracks.RemoveTracks();
            RetrieveDataButton.IsEnabled = false;
            RetrieveDataButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
        }

        private void ShowPianoRoll()
        {
            var pianoControl = new PianoRollUserControl(TrackId);
            pianoControl.ScoreChanged += PianoControl_ScoreChanged;

            var grid = GetGrid();
            if (grid != null)
            {
                if (grid.Children.Count > 0)
                {
                    grid.Children.RemoveAt(0);
                }
                grid.Children.Add(pianoControl);
            }
        }

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            //TrackActionHandler?.Invoke(this, EventArgs.Empty);

            await _service.SendTrack(this.GetTrack(TrackId));

            UploadButton.IsEnabled = false;
            UploadButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
        }
    }
}

