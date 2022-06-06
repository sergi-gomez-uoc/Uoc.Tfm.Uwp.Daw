using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uoc.Tfm.Uwp.Daw.Config;
using Uoc.Tfm.Uwp.Daw.Controls;
using Uoc.Tfm.Uwp.Daw.Extensions;
using Uoc.Tfm.Uwp.Daw.Interfaces;
using Uoc.Tfm.Uwp.Daw.Model;
using Uoc.Tfm.Uwp.Daw.Services;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uoc.Tfm.Uwp.Daw
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, ControlInterface
    {
        internal HubConnection connection;
        SignalRCommunicationService _service;
        private readonly AppConfig _config;

        Song receivedSong;
        public bool TracksModified { get; set; } = false;

        public MainPage(AppConfig config)
        {
            this.InitializeComponent();

            _config = config;

            var hubUrlBase = _config.GetKey("UrlBase");

            connection = new HubConnectionBuilder()
                            .WithUrl(hubUrlBase)
                            .Build();
            connection.Closed += Connection_Closed;

            _service = new SignalRCommunicationService(connection);
            _service.ReceiveSong += Service_RecieveSong;
            _service.ReceiveTrack += Service_ReceiveTrack; ;
        }

        private async Task Connection_Closed(Exception arg)
        {
            await Task.Run(
                () => this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                        {
                            ConnectedWorldIcon.Foreground = new SolidColorBrush(Colors.Red);
                            PushAllButton.IsEnabled = false;
                            UploadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
                            ReceiveAllButton.IsEnabled = false;
                            DownloadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
                            CleanTracksUploadButtons();
                            CleanTracksDownloadButtons();
                            TracksModified = false;
                        })
                );
        }

        private void Service_ReceiveTrack(Track track)
        {
            var currentTrack = this.GetTrack(track.TrackId);

            if (currentTrack == null)
            {
                return;
            }

            var control = TrackListBox
                            .Items
                            .FirstOrDefault(x => (x as TrackUserControl)
                            .TrackId == track.TrackId) as TrackUserControl;

            if (control != null)
            {
                control.ShowDownloadIcon();
            }
        }

        private void Service_RecieveSong(Song obj)
        {
            ReceiveAllButton.IsEnabled = true;
            DownloadIcon.Foreground = new SolidColorBrush(Colors.DarkGreen);
            receivedSong = obj;
        }

        private void AddTrackButton_Click(object sender, RoutedEventArgs e)
        {
            var track = new Track();
            track.TrackId = Guid.NewGuid();
            track.Name = "Track";

            this.GetCurrentSong().Tracks.Add(track);

            var trackControl = CreateTrackUserControl(track.TrackId, track.Name);

            TrackListBox.Items.Add(trackControl);

            if (TrackListBox.Items.Count == 1)
            {
                (App.Current as App).IsPianoRollShown = false;
                TrackListBox.SelectedIndex = 0;
            }

            TracksModified = true;

            if (connection.State == HubConnectionState.Connected)
            {
                PushAllButton.IsEnabled = true;
                UploadIcon.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
        }

        private void TrackControl_OnTrackDeleted(object sender, EventArgs e)
        {
            TracksModified = true;
            if (connection.State == HubConnectionState.Connected)
            {
                PushAllButton.IsEnabled = true;
                UploadIcon.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
        }

        private void TrackControl_OnNameChanged(object sender, EventArgs e)
        {
            var trackId = (sender as TrackUserControl).TrackId;
            var track = this.GetTrack(trackId);

            if (track != null)
            {
                var trackName = (sender as TrackUserControl).TrackName;
                track.Name = trackName;
            }
            TracksModified = true;

            if (connection.State == HubConnectionState.Connected)
            {
                PushAllButton.IsEnabled = true;
                UploadIcon.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
        }

        private void TrackControl_TrackActionHandler(object sender, EventArgs e)
        {
            if (_service == null
                || connection == null
                || connection.State != HubConnectionState.Connected)
            {
                return;
            }

            PushAllButton.IsEnabled = true;
            UploadIcon.Foreground = new SolidColorBrush(Colors.DarkGreen);
        }

        private async void ConnectToSignalRButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (connection.State != HubConnectionState.Connected)
                {
                    _service = new SignalRCommunicationService(connection);
                    _service.ReceiveSong += Service_RecieveSong;
                    _service.ReceiveTrack += Service_ReceiveTrack;
                    await _service.Connect();

                    ConnectedWorldIcon.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    ConnectedWorldIcon.Foreground = new SolidColorBrush(Colors.Red);

                    PushAllButton.IsEnabled = false;
                    UploadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
                    ReceiveAllButton.IsEnabled = false;
                    DownloadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
                    CleanTracksUploadButtons();
                    CleanTracksDownloadButtons();
                    TracksModified = false;

                    var baseUrl = _config.GetKey("UrlBase");
                    connection = new HubConnectionBuilder()
                                    .WithUrl(baseUrl)
                                    .Build();

                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(ex.Message, "Error");

                await dialog.ShowAsync();
            }

        }

        private async void PushAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_service == null ||
                    connection == null ||
                    connection.State != HubConnectionState.Connected)
                {
                    return;
                }

                var song = this.GetCurrentSong();

                await _service.SendSong(song);

                TracksModified = false;
                PushAllButton.IsEnabled = false;
                UploadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));

                foreach (var trackControl in TrackListBox.Items)
                {
                    if (trackControl is TrackUserControl)
                    {
                        (trackControl as TrackUserControl).CleanUploadTrackButton();
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Error: {ex.Message}");
                await dialog.ShowAsync();
            }
        }

        private async void ReceiveAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadSong(receivedSong);

                ReceiveAllButton.IsEnabled = false;
                DownloadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));

                foreach (var trackControl in TrackListBox.Items)
                {
                    if (trackControl is TrackUserControl)
                    {
                        (trackControl as TrackUserControl).CleanDownloadTrackButton();
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Error: {ex.Message}");
                await dialog.ShowAsync();
            }
        }

        private async Task LoadSong(Song song)
        {
            await Task.Run(async () =>
            {
                var currentSong = this.GetCurrentSong();

                if (currentSong == null)
                {
                    var newSong = new Song();
                    newSong.SongId = song.SongId;
                    newSong.Tracks = new List<Track>();

                    this.SetCurrentSong(newSong);
                }
                else
                {
                    foreach (var track in song.Tracks)
                    {
                        await LoadTracksAsync(track);
                    }
                }
            });
        }

        private async Task LoadTracksAsync(Track track)
        {

            await Task.Run(async () =>
            {
                var song = this.GetCurrentSong();

                if (!song.Tracks.Any(x => x.TrackId == track.TrackId))
                {
                    song.Tracks.Add(track);

                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var trackControl = CreateTrackUserControl(track.TrackId, track.Name);
                        TrackListBox.Items.Add(trackControl);
                    });
                }
                else
                {
                    var currentTrack = song.Tracks.FirstOrDefault(x => x.TrackId == track.TrackId);
                    if (currentTrack != null)
                    {
                        currentTrack.Name = track.Name;
                        currentTrack.Scores = track.Scores;
                    }

                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var trackUserControl = TrackListBox
                                                .Items
                                                .FirstOrDefault(x => (x as TrackUserControl).TrackId == track.TrackId);

                        if (trackUserControl != null)
                        {
                            (trackUserControl as TrackUserControl).TrackName = track.Name;
                        }
                    });
                }
            });

        }

        private TrackUserControl CreateTrackUserControl(Guid trackId, string Name)
        {
            var trackControl = new TrackUserControl(trackId, Name, connection, _service);
            trackControl.Margin = new Thickness(0, 0, 0, 0);
            trackControl.Padding = new Thickness(0, 0, 0, 0);

            trackControl.Width = TrackListBox.ActualWidth;
            trackControl.TrackActionHandler += TrackControl_TrackActionHandler;
            trackControl.OnNameChanged += TrackControl_OnNameChanged;
            trackControl.OnTrackDeleted += TrackControl_OnTrackDeleted;

            return trackControl;
        }
        private void TrackListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var track in TrackListBox.Items)
            {
                if (track is TrackUserControl)
                {
                    (track as TrackUserControl).Width = TrackListBox.ActualWidth;
                }
            }
        }
        private void TrackListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = TrackListBox.SelectedItem;

            if (selectedItem == null ||
                selectedItem is TrackUserControl == false)
            {
                return;
            }

            foreach (var item in TrackListBox.Items)
            {
                if (item is TrackUserControl)
                {
                    var selectedTrackId = (selectedItem as TrackUserControl).TrackId;

                    (item as TrackUserControl)
                        .SetTrackUserControlActive(selectedTrackId);
                }
            }
        }
        internal void CleanUploadAllSongIfNeeded()
        {
            var scoresUpdated = false;

            foreach (var track in TrackListBox.Items)
            {
                if (track is TrackUserControl)
                {
                    var trackChanged = (track as TrackUserControl).HasScoredChanged;
                    if (trackChanged)
                    {
                        scoresUpdated = true;
                        break;
                    }
                }
            }

            if (!scoresUpdated && !TracksModified)
            {
                PushAllButton.IsEnabled = false;
                UploadIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 13, 28));
            }
        }
        private void CleanTracksUploadButtons()
        {
            foreach (var trackControl in TrackListBox.Items)
            {
                if (trackControl is TrackUserControl)
                {
                    (trackControl as TrackUserControl).CleanUploadTrackButton();
                }
            }
        }
        private void CleanTracksDownloadButtons()
        {
            foreach (var trackControl in TrackListBox.Items)
            {
                if (trackControl is TrackUserControl)
                {
                    (trackControl as TrackUserControl).CleanUploadTrackButton();
                }
            }
        }
    }
}
