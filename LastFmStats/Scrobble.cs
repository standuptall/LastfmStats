using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LastFmStats
{
    public class Scrobble
    {
        public Scrobble()
        {

        }
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
        public Scrobble(string artist, string album, string track, DateTime data)
        {
            this.Artist = artist;
            this.Album = album;
            this.Track = track;
            try
            {
                this.Data = data;
            }
            catch
            {

            }
        }

        public string Artist { get; set; }
        public string Album { get; set; }
        public string Track { get; set; }
        public DateTime Data { get; set; }
    }
}
