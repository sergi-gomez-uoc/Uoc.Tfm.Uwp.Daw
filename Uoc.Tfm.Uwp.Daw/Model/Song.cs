using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uoc.Tfm.Uwp.Daw.Model
{
    [Serializable]
    public class Song
    {
        public Guid SongId { get; set; }
        public IList<Track> Tracks { get; set; }

    }
}
