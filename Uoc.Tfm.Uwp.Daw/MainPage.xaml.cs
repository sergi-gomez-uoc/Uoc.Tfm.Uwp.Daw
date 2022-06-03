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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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

        public MainPage( AppConfig config)
        {
            this.InitializeComponent();

            _config = config;

            var hubUrlBase = _config.GetKey("UrlBase");

            connection = new HubConnectionBuilder()
                            .WithUrl(hubUrlBase)
                            .Build();
            connection.Closed += Connection_Closed;

            _service = new SignalRCommunicationService(connection);
            _service.MessageEvent += Service_MessageEvent;
            _service.ReceiveSong += Service_RecieveSong;
            _service.ReceiveTrack += _service_ReceiveTrack; ;
        }

        private async Task Connection_Closed(Exception arg)
        {
            await Task.Run(
                () => this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => ConnectedWorldIcon.Foreground = new SolidColorBrush(Colors.Red)));
        }

        private void _service_ReceiveTrack(Track track)
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
                control.TurnOnReceiveIcon();
            }
        }

        Song receivedSong;
        private void Service_RecieveSong(Song obj)
        {
            ReceiveAllButton.IsEnabled = true;
            receivedSong = obj;

        }

        private void Service_MessageEvent(string obj)
        {
            //AnimationButton.Content = obj.ToString();

        }


        private void AddTrackButton_Click(object sender, RoutedEventArgs e)
        {

            var track = new Track();

            track.TrackId = Guid.NewGuid();
            track.Name = "Track";

            //(App.Current as App)._song.Tracks.Add(track);
            this.GetCurrentSong().Tracks.Add(track);

            //var trackControl = new TrackUserControl(track.TrackId, track.Name, connection, _service);
            //trackControl.Margin = new Thickness(0, 0, 0, 0);
            //trackControl.Padding = new Thickness(0, 0, 0, 0);

            //trackControl.Width = TrackListBox.ActualWidth;
            //trackControl.TrackActionHandler += TrackControl_TrackActionHandler;
            //trackControl.OnNameChanged += TrackControl_OnNameChanged;
            //trackControl.OnTrackDeleted += TrackControl_OnTrackDeleted;
            var trackControl = CreateTrackUserControl(track.TrackId, track.Name);
            TrackListBox.Items.Add(trackControl);

            if (connection.State == HubConnectionState.Connected)
            {
                PushAllButton.IsEnabled = true;
            }
        }

        private void TrackControl_OnTrackDeleted(object sender, EventArgs e)
        {
            PushAllButton.IsEnabled = true;
        }

        private void TrackControl_OnNameChanged(object sender, EventArgs e)
        {
            var trackId = (sender as TrackUserControl).TrackId;
            var trackName = (sender as TrackUserControl).TrackName;

            var track = (App.Current as App)._song.Tracks.FirstOrDefault(x => x.TrackId == trackId);

            if (track != null)
            {
                track.Name = trackName;
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
        }

        private async void ConnectToSignalRButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (connection.State != HubConnectionState.Connected)
                {
                    _service = new SignalRCommunicationService(connection);

                    _service.MessageEvent += Service_MessageEvent;
                    _service.ReceiveSong += Service_RecieveSong;
                    _service.ReceiveTrack += _service_ReceiveTrack;
                    await _service.Connect();

                    ConnectedWorldIcon.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    ConnectedWorldIcon.Foreground = new SolidColorBrush(Colors.Red);
                    PushAllButton.IsEnabled = false;
                    ReceiveAllButton.IsEnabled = false;

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
                if (_service == null || connection == null || connection.State != HubConnectionState.Connected)
                {
                    return;
                }

                var song = this.GetCurrentSong();

                await _service.SendSong(song);

                PushAllButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Error: {ex.Message}");
                await dialog.ShowAsync();
            }
        }

        private async void ReceiveAllButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSong(receivedSong);

            ReceiveAllButton.IsEnabled = false;

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
                        //var trackControl = new TrackUserControl(track.TrackId, track.Name, connection, _service);
                        //trackControl.Margin = new Thickness(0, 0, 0, 0);
                        //trackControl.Padding = new Thickness(0, 0, 0, 0);

                        //trackControl.Width = TrackListBox.ActualWidth;
                        //trackControl.TrackActionHandler += TrackControl_TrackActionHandler;
                        //trackControl.OnNameChanged += TrackControl_OnNameChanged;
                        //trackControl.OnTrackDeleted += TrackControl_OnTrackDeleted;
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
                        var trackUserControl = TrackListBox.Items.FirstOrDefault(x => (x as TrackUserControl).TrackId == track.TrackId);

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
    }
}
