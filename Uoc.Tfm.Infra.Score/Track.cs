using System;
using System.Collections.Generic;

namespace Uoc.Tfm.Infra.Score
{
    public class Track
    {
        public Guid TrackId { get; set; }

        public string Name { get; set; }

        public IList<Score> Scores { get; set; }
    }
}