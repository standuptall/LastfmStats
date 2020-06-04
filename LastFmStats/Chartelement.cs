using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFmStats
{
    public class Chartelement
    {
         public string Artist { get; set; }
         public int TotalArtist { get; set; }
        public int TotalListening { get; set; }
        public string Header { get; set; }
        public override string ToString()
        {
            return Header + "\t" + TotalListening + ", " + Artist + "(" + TotalArtist + ")";
        }
    }
}
