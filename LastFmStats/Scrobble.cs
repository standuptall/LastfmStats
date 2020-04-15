using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LastFmStats
{
    public class Scrobble
    {
        public Scrobble(string artist, string album, string track, string data)
        {
            this.Artist = artist;
            this.Album = album;
            this.Track = track;
            try
            {
                this.Data = DateTime.ParseExact(data, "dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
            }
            catch
            {

            }
        }

        public string Artist { get; private set; }
        public string Album { get; private set; }
        public string Track { get; private set; }
        public DateTime Data { get; private set; }
    }
}
