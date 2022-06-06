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
        private bool isActive;

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
                    //UploadButton.BorderBrush =
                    //    new SolidColorBrush(Colors.LightGray);
                    UploadButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
                }
            }
        }

        internal event EventHandler TrackActionHandler;

        internal event EventHandler OnNameChanged;

        internal event EventHandler OnTrackDeleted;

        public bool HasScoredChanged { get; set; } = false;

        public TrackUserControl(
            Guid? trackNumber = null,
            string trackName = null,
            HubConnection connection = null,
            SignalRCommunicationService service = null)
        {
            this.InitializeComponent();
            this.hubConnection = connection;
            this._service = service;

            TrackId = trackNumber != null
                        && trackNumber != default ?
                                            trackNumber.Value :
                                            Guid.NewGuid();
            TrackName = trackName != null ? trackName : "Track";
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
            }
        }
        public void SetTrackUserControlActive(Guid activeTrackId)
        {
            IsActive = activeTrackId == TrackId;

            var isSymbolActivated = IsPianoRollSymbolActivated();
            SetPianoRollSymbolColor(isSymbolActivated);

            if (IsActive)
            {
                ShowPianoRoll();
            }
        }

        private void SetPianoRollSymbolColor(bool active)
        {
            if (active)
            {
                TrackButtonSymbol.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                TrackButtonSymbol.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private bool IsPianoRollSymbolActivated()
        {
            var showPianoRoll = (App.Current as App).IsPianoRollShown;

            var isCurrentTrackSelected = ((this.Parent as ListBox).SelectedItem as TrackUserControl) != null &&
                        ((this.Parent as ListBox).SelectedItem as TrackUserControl).TrackId == TrackId;

            return showPianoRoll && isCurrentTrackSelected;
        }
        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current as App).IsPianoRollShown)
            {
                (this.Parent as ListBox).SelectedItem = this;
            }

            var isCurrentTrackSelected = ((this.Parent as ListBox).SelectedItem as TrackUserControl) != null &&
                        ((this.Parent as ListBox).SelectedItem as TrackUserControl).TrackId == TrackId;

            if (isCurrentTrackSelected)
            {
                (App.Current as App).IsPianoRollShown = !(App.Current as App).IsPianoRollShown;
                var isSymbolActivated = IsPianoRollSymbolActivated();
                SetPianoRollSymbolColor(isSymbolActivated);
                ShowPianoRoll();
            }
            else
            {
                (this.Parent as ListBox).SelectedItem = this;
            }
        }

        private void PianoControl_ScoreChanged(object sender, EventArgs e)
        {
            HasScoredChanged = true;

            IsPushButtonActive = hubConnection.State == HubConnectionState.Connected;

            if (IsPushButtonActive && HasScoredChanged)
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

            var track = this.GetTrack(TrackId);
            if (track != null)
            {
                var listBox = Parent as ListBox;
                if (listBox != null)
                {
                    var selectedTrack = listBox.SelectedItem;
                    
                    if (selectedTrack != null && (selectedTrack as TrackUserControl) != null)
                    {
                        if ((selectedTrack as TrackUserControl).TrackId == TrackId)
                        {
                            var grid = GetGrid();
                            grid.Children.Clear();
                        }
                    }

                    listBox.Items.Remove(this);
                }

                this.GetCurrentSong().Tracks.Remove(track); // song.Tracks.Remove(track);

                OnTrackDeleted?.Invoke(sender, EventArgs.Empty);
            }
        }

        private Grid GetGrid()
        {
            const string PianoGrid = "Grid1";

            var parentGrid = (Parent as Control).Parent as Grid;
            var rootGrid = (parentGrid.Parent as Grid);

            var gridControl = rootGrid
                                .Children
                                .FirstOrDefault(
                                        x => x.GetType() == typeof(Grid) &&
                                        (x as Grid).Name == PianoGrid);

            return gridControl as Grid;
        }

        private void PushDataButton_Click(object sender, RoutedEventArgs e)
        {
            TrackActionHandler?.Invoke(this, EventArgs.Empty);

            UploadButton.IsEnabled = false;
            UploadButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
        }

        private void TrackNameTextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
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

        internal void ShowDownloadIcon()
        {
            DownloadDataButton.IsEnabled = true;
            DownloadDataButton.BorderBrush = new SolidColorBrush(Colors.DarkGreen);

        }

        private void RetrieveDataButton_Click(object sender, RoutedEventArgs e)
        {
            var receivedTracks = this.GetReceivedTracks();

            if (receivedTracks == null || !receivedTracks.Any())
            {
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
            DownloadDataButton.IsEnabled = false;
            DownloadDataButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
        }

        private void ShowPianoRoll()
        {
            var grid = GetGrid();

            if (grid != null)
            {
                grid.Children.Clear();
            }

            var showPianoRoll = (Application.Current as App).IsPianoRollShown;

            if (showPianoRoll)
            {
                var pianoControl = new PianoRollUserControl(TrackId);
                pianoControl.ScoreChanged += PianoControl_ScoreChanged;
                grid.Children.Add(pianoControl);
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            await _service.SendTrack(this.GetTrack(TrackId));
            HasScoredChanged = false;
            UploadButton.IsEnabled = false;
            UploadButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
            UploadButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));

            (((((this.Parent as ListBox).Parent as Grid).Parent as Grid).Parent as Grid).Parent as MainPage).CleanUploadAllSongIfNeeded();
        }

        internal void CleanUploadTrackButton()
        {
            UploadButton.IsEnabled = false;
            UploadButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
            HasScoredChanged = false;
        }

        internal void CleanDownloadTrackButton()
        {
            DownloadDataButton.IsEnabled = false;
            DownloadDataButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
        }
    }
}

