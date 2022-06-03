using System;
using System.Collections.Generic;

namespace Uoc.Tfm.Infra.Score.Uni
{
    public class Track
    {
        public Guid TrackId { get; set; }

        public string Name { get; set; }

        public IList<Score> Scores { get; set; }

        //public Track(Guid? trackId = null, string trackName = null)
        //{
        //    TrackId = trackId != null && trackId != Guid.Empty ? trackId.Value : Guid.NewGuid();
        //    Name = trackName != null ? trackName : "Track";
        //}
    }
}