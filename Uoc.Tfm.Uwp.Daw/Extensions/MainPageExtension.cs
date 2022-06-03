using System;
using System.Collections.Generic;
using System.Linq;
using Uoc.Tfm.Uwp.Daw.Controls;
using Uoc.Tfm.Uwp.Daw.Interfaces;
using Uoc.Tfm.Uwp.Daw.Model;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uoc.Tfm.Uwp.Daw.Extensions
{
    public static class MainPageExtension
    {
        public static Song GetCurrentSong<T>(this T page) where T : ControlInterface
        {
            return (App.Current as App)._song;
        }
        
        public static void SetCurrentSong<T>(this T page, Song song) where T : ControlInterface
        {
            (App.Current as App)._song = song;
        }


        public static Track GetTrack<T>(this T page, Guid trackId) where T : ControlInterface
        {
            return GetCurrentSong(page).Tracks.FirstOrDefault(x => x.TrackId == trackId);
        }

        public static IEnumerable<Track> GetReceivedTracks(this TrackUserControl trackControl)
        {
            return (App.Current as App).receivedTracks.Where(x => x.TrackId == trackControl.TrackId);
        }

        public static void AddTrack(this Track track)
        {
            (App.Current as App).receivedTracks.Add(track);
        }

        public static void RemoveTrack(this Track track)
        {
            (App.Current as App).receivedTracks.Remove(track);
        }

        public static void RemoveTracks(this IEnumerable<Track> tracks)
        {
            //foreach(var tr in tracks)
            //{
            //    (App.Current as App).receivedTracks.Remove(tr);
            //}

            tracks.ToList().ForEach(x => (App.Current as App).receivedTracks.Remove(x));
        }
    }
}
