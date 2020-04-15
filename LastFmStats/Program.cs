using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Threading;

namespace LastFmStats
{
    class Program
    {
        static List<Scrobble> Scrobbles = new List<Scrobble>();
        static void Main(string[] args)
        {
            Console.WriteLine("Inserisci il file");
            var file = GetInput();
            var righe = File.ReadAllLines(file);
            foreach (var riga in righe)
            {
                var campi = riga.Split(',');
                if (campi.Length >= 4)
                {
                    Scrobbles.Add(new Scrobble(campi[0], campi[1], campi[2], campi[3]));
                }
            }
            while (true)
            {
                Console.WriteLine("S - Statistiche Generali");
                Console.WriteLine("R - Ricerca per traccia");
                Console.WriteLine("RA - Ricerca per album");
                var scelta = Console.ReadLine();
                switch (scelta.ToUpper())
                {
                    case "S":
                        MenuStatisticheGenerali();
                        break;
                    case "R":
                        MenuRicercaPerTraccia();
                        break;
                    case "RA":
                        MenuRicercaPerAlbum();
                        break;
                }
            }
        }

        private static void MenuRicercaPerAlbum()
        {
            Console.Write("Inserisci query di ricerca: ");
            var query = Console.ReadLine();
            var trova = Scrobbles.Where(c => c.Album.ToUpper().Contains(query.ToUpper())).ToList();
            Console.WriteLine("Trovati {0} scrobbles", trova.Count);
            foreach (var s in trova)
                Console.WriteLine("{0}   {1} - {2}", s.Data.ToString("dd/MM/yyyy HH:mm:ss"), s.Artist, s.Track);
        }

        private static void MenuRicercaPerTraccia()
        {
            Console.Write("Inserisci query di ricerca: ");
            var query = Console.ReadLine();
            var trova = Scrobbles.Where(c => c.Track.ToUpper().Contains(query.ToUpper())).ToList();
            Console.WriteLine("Trovati {0} scrobbles", trova.Count);
            foreach (var s in trova)
                Console.WriteLine("{0}   {1} - {2}", s.Data.ToString("dd/MM/yyyy HH:mm:ss"), s.Artist, s.Track);
        }

        private static void MenuStatisticheGenerali()
        {
            Console.WriteLine("Inserisci prima data del range (GG/MM/AAAA)");
            var partenza = GetInput();
            var datapartenza = DateTime.Parse(partenza);
            Console.WriteLine("Inserisci seconda data del range (GG/MM/AAAA)");
            var arrivo = GetInput();
            var dataarrivo = DateTime.Parse(arrivo);
            var scrobbleintorange = Scrobbles.Where(c => c.Data <= dataarrivo && c.Data >= datapartenza).ToList();
            Console.WriteLine("Numero di canzoni :" + scrobbleintorange.Count);
            var numgiorni = (dataarrivo - datapartenza).Days;
            Console.WriteLine("Scrobbling al giorno: " + scrobbleintorange.Count / numgiorni);
            var artisti = scrobbleintorange.Select(c => c.Artist).Distinct().Count();
            var album = scrobbleintorange.Select(c => new { c.Artist, c.Album }).Distinct().Count();
            var tracce = scrobbleintorange.Select(c => new { c.Artist, c.Album, c.Track }).Distinct().Count();
            var artista = scrobbleintorange.GroupBy(c => c.Artist).OrderByDescending(c => c.Count()).FirstOrDefault().Key;
            var albumpiasc = scrobbleintorange.GroupBy(c => new { c.Artist, c.Album }).OrderByDescending(c => c.Count()).FirstOrDefault().Key.Album;
            var tracciapiasc = scrobbleintorange.GroupBy(c => new { c.Artist, c.Album, c.Track }).OrderByDescending(c => c.Count()).FirstOrDefault().Key.Track;
            Console.WriteLine("Artisti: " + artisti);
            Console.WriteLine("Album: " + album);
            Console.WriteLine("Canzoni: " + tracce);
            Console.WriteLine("Artista più ascoltato: " + artista);
            Console.WriteLine("Album più ascoltato: " + albumpiasc);
            Console.WriteLine("Traccia più ascoltata: " + tracciapiasc);

            //giorno virtuoso
            var IGiorno = scrobbleintorange.GroupBy(c => c.Data.Date).OrderByDescending(c => c.Count()).FirstOrDefault();
            if (IGiorno != null)
            {
                var giorno = IGiorno.Key;
                Console.WriteLine("Giorno più virtuoso: {0} ({1} ascolti)", giorno.ToString("dd/MM/yyyy"), IGiorno.Count());
            }
            var tracceascoltatedinotte = GetTracceAscoltateDiNotte(scrobbleintorange);
            foreach (var t in tracceascoltatedinotte)
            {
                Console.WriteLine("{0} {1} - {2}", (NormalizzaData(t.Data)).ToString("dd/MM/yyyy HH:mm:ss"), t.Artist, t.Track);
            }
            var lc = GetListeningClock(scrobbleintorange);
            var lcheader = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            var lwheader = new string[] { "Dom", "Lun", "Mar", "Mer", "Gio", "Ven", "Sab" };
            var lw = GetListeningWeek(scrobbleintorange);
            PrintArray(lcheader, lc);
            Console.WriteLine(Environment.NewLine);
            PrintArray(lwheader, lw);
            string[] apsheader;
            string[] apmheader;
            var aps = GetArtistiPerSettimana(scrobbleintorange, out apsheader);
            var apm = GetArtistiPerMese(scrobbleintorange, out apmheader);
            PrintArrayString(apsheader, aps);
            Console.WriteLine(Environment.NewLine);
            PrintArrayString(apmheader, apm);
            Console.ReadLine();
        }

        private static string[] GetArtistiPerMese(List<Scrobble> scrobbles, out string[] apsheader)
        {
            var max = scrobbles.Select(c => c.Data).Max();
            var min = scrobbles.Select(c => c.Data).Min();
            var primogiornomese = min;
            while ((int)primogiornomese.Day != 1)
                primogiornomese = primogiornomese.AddDays(-1);
            var ultimogiornomese = primogiornomese.AddMonths(1);
            var getnummesi = (max - min).Days / 30 + 1;
            var ret = new string[getnummesi];
            apsheader = new string[getnummesi];
            for (int i = 0; i < getnummesi; i++)
            {
                apsheader[i] = primogiornomese.ToString("dd/MM/yyyy") + "-" + ultimogiornomese.ToString("dd/MM/yyyy");
                var a = scrobbles.Where(c => c.Data >= primogiornomese && c.Data < ultimogiornomese)
                    .GroupBy(c => c.Artist)
                    .OrderByDescending(c => c.Count())
                    .FirstOrDefault();
                if (a != null)
                    ret[i] = a.Select(c => c.Artist).FirstOrDefault();
                primogiornomese = primogiornomese.AddDays(7);
                ultimogiornomese = ultimogiornomese.AddDays(7);
            }

            return ret;
        }

        private static List<Scrobble> GetTracceAscoltateDiNotte(List<Scrobble> scrobbleintorange)
        {
            var ret = new List<Scrobble>();
            foreach (var s in scrobbleintorange)
            {
                var dt = NormalizzaData(s.Data);
                if (dt.Hour == 3 || dt.Hour == 4 || dt.Hour == 5)
                    ret.Add(s);
            }
            return ret;

        }
        private static DateTime NormalizzaData(DateTime s)
        {
            var valore = s.Hour + GetOraFusoOrario(s);
            if (valore >= 24)
                valore = valore - 24;
            return new DateTime(s.Year, s.Month, s.Day, valore, s.Minute, s.Second);
        }

        private static string[] GetArtistiPerSettimana(IEnumerable<Scrobble> scrobbles, out string[] apsheader)
        {
            var max = scrobbles.Select(c => c.Data).Max();
            var min = scrobbles.Select(c => c.Data).Min();
            var primogiornosettimana = min;
            while ((int)primogiornosettimana.DayOfWeek != 0)
                primogiornosettimana = primogiornosettimana.AddDays(-1);
            var ultimogiornosettimana = primogiornosettimana.AddDays(7);
            var getnumsettimane = (max - min).Days / 7 + 1;
            var ret = new string[getnumsettimane];
            apsheader = new string[getnumsettimane];
            for(int i = 0; i < getnumsettimane; i++)
            {
                apsheader[i] = primogiornosettimana.ToString("dd/MM/yyyy")+"-"+ ultimogiornosettimana.ToString("dd/MM/yyyy");
                var a = scrobbles.Where(c => c.Data >= primogiornosettimana && c.Data < ultimogiornosettimana)
                    .GroupBy(c => c.Artist)
                    .OrderByDescending(c => c.Count())
                    .FirstOrDefault();
                if (a != null)
                    ret[i] = a.Select(c => c.Artist).FirstOrDefault();
                primogiornosettimana = primogiornosettimana.AddDays(7);
                ultimogiornosettimana = ultimogiornosettimana.AddDays(7);
            }

            return ret;
        }

        static string GetInput()
        {
            var inp = Console.ReadLine();
            if (inp == "exit")
                Environment.Exit(0);
            return inp;
        }
        private static void PrintArrayString(string[] header, string[] value)
        {
            for (int i = 0; i < header.Length; i++)
            {
                Console.WriteLine(header[i] + "   " + value[i]);
            }
        }
        private static void PrintArray(string[] lcheader, int[] lc)
        {
            var max = lc.Max();
            var lunghezza = 60;
            //max sta a lunghezza come val sta a nuovalunghezza
            //nuova lunghezza = val*lun/max
            var ratio = (double)lunghezza / max;
            for(int i = 0; i < lcheader.Length; i++)
            {
                Console.Write("{0,-10}", lcheader[i]);
                var nuovalunghezza = (int)Math.Round((double)lc[i] * ratio,0);
                for (int j=0;j< nuovalunghezza;j++)
                {
                    Console.Write("*");
                }
                Console.Write("{0,5}", lc[i]);
                Console.Write(Environment.NewLine);
            }
        }

        private static int[] GetListeningClock(IEnumerable<Scrobble> scrobbles)
        {
            var ret = new int[24];
            foreach (var s in scrobbles)
            {
                var valore = s.Data.Hour + GetOraFusoOrario(s.Data);
                if (valore >= 24)
                    valore = valore - 24;
                ret[valore] += 1;
            }
            return ret;
        }
        private static int[] GetListeningWeek(IEnumerable<Scrobble> scrobbles)
        {
            var ret = new int[7];
            
            foreach (var s in scrobbles)
            {
                ret[(int)s.Data.DayOfWeek] += 1;
            }
            return ret;
        }
        static int GetOraFusoOrario(DateTime data)
        {
            var anno = data.Year;
            //ultima domenica di marzo
            var d1 = new DateTime(anno, 3, 24);
            while ((int)d1.DayOfWeek != 0)
            {
                d1 = d1.AddDays(1);
            }
            //ultima domenica di ottobre
            var d2 = new DateTime(anno, 10, 24);
            while (d2.DayOfWeek != 0)
            {
                d2 = d2.AddDays(1);
            }
            if (data >= d1 && data <= d2)
                return 2;
            return 1;
        }

    }
}
