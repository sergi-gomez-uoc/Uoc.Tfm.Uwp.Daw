using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using Uoc.Tfm.Uwp.Daw.Extensions;
using Uoc.Tfm.Uwp.Daw.Model;

namespace Uoc.Tfm.Uwp.Daw.Services
{
    public class SignalRCommunicationService
    {
        private readonly HubConnection _connection;

        public event Action<string> MessageEvent;

        public event Action<Song> ReceiveSong;

        public event Action<Track> ReceiveTrack;

        public SignalRCommunicationService(HubConnection connection)
        {
            _connection = connection;

            _connection.On<string>("ReceiveTest", s =>
            {
                MessageEvent?.Invoke(s);
            });

            _connection.On<Song>("WriteScore", s =>
            {
                
                ReceiveSong?.Invoke(s);
            });

            _connection.On<Track>("WriteTrack", s =>
            {
                s.AddTrack();
                ReceiveTrack?.Invoke(s);
            });
        }

        public async Task Connect()
        {
            await _connection.StartAsync();
        }

        public async Task SendSong(Song song)
        {
            await _connection.SendAsync("SendSong", song);
        }

        public async Task SendTrack(Track track)
        {
            await _connection.SendAsync("SendTrack", track);
        }
    }
}
