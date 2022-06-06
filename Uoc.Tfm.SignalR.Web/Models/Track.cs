namespace Uoc.Tfm.SignalR.Web.Models
{
    public class Track
    {
        public Guid TrackId { get; set; }

        public string? Name { get; set; }

        public IList<Score>? Scores { get; set; }
    }
}