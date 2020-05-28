using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFmStats
{
    public class Periodo
    {
        public DateTime Inizio { get; set; }
        public DateTime Fine { get; set; }
        public int NumeroAscolti { get; set; }
        public override string ToString()
        {
            return Inizio.ToString("dd/MM/yyyy HH:mm:ss") + " - " + Fine.ToString("dd/MM/yyyy HH:mm:ss") + " " + NumeroAscolti.ToString();
        }
    }
}
