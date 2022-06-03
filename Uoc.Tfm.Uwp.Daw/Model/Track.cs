using System;
using System.Collections.Generic;

namespace Uoc.Tfm.Uwp.Daw.Model
{
    public class Track
    {
        public Guid TrackId { get; set; }

        public string Name { get; set; }

        public IList<Score> Scores { get; set; }
    }
}