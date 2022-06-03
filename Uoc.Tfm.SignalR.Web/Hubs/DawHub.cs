using Microsoft.AspNetCore.SignalR;
using Uoc.Tfm.Infra.Score;

namespace Uoc.Tfm.SignalR.Web.Hubs
{
    public class DawHub : Hub
    {
        public async Task SendTest(string test)
        {
            await Clients.All.SendAsync("ReceiveTest", test);
        }

        public async Task SendSong (Song song)
        {
            await Clients.Others.SendAsync("WriteScore", song);
        }

        public async Task SendTrack(Track track)
        {
            await Clients.Others.SendAsync("WriteTrack", track);


        }
    }
}
