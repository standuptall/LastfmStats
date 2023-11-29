using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Threading;
using System.CodeDom;
using System.Globalization;
using Newtonsoft.Json;
using System.Collections;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System.Runtime.Remoting.Contexts;
using Microsoft.ML.Transforms.Text;

namespace LastFmStats
{
    class Program
    {
        static List<Scrobble> Scrobbles = new List<Scrobble>();
        static void Main(string[] args)
        {
            //Console.WriteLine("Inserisci il file");
            //var file = GetInput();
            //var righe = File.ReadAllLines(file);
            //foreach (var riga in righe)
            //{
            //    var campi = riga.Split(',');
            //    if (campi.Length >= 4)
            //    {
            //        Scrobbles.Add(new Scrobble(campi[0], campi[1], campi[2], campi[3]));
            //    }
            //}
            if (File.Exists("data.json"))
            {

                Scrobbles = JsonConvert.DeserializeObject<List<Scrobble>>(File.ReadAllText("data.json"));
                Console.WriteLine("Scrobbles caricati correttamente");
            }
            Scrobbles.RemoveAll(c => c.Data == new DateTime(1, 1, 1));
            while (true)
            {
                Console.WriteLine("AG - Aggiorna");
                Console.WriteLine("AD - Aggiorna Differenziale");
                Console.WriteLine("S - Statistiche Generali");
                Console.WriteLine("P - Classifica periodi di 6 mesi");
                Console.WriteLine("R - Ricerca per traccia");
                Console.WriteLine("A - Ricerca per artista");
                Console.WriteLine("RA - Ricerca per album");
                Console.WriteLine("SA - Statistiche per Artista");
                Console.WriteLine("AS - Trova artisti simili");
                var scelta = Console.ReadLine();
                switch (scelta.ToUpper())
                {
                    case "S":
                        MenuStatisticheGenerali();
                        break;
                    case "AG":
                        Aggiorna();
                        break;
                    case "AD":
                        AggiornaDifferenziale();
                        break;
                    case "P":
                        ClassificaPeriodiSeiMesi();
                        break;
                    case "R":
                        MenuRicercaPerTraccia();
                        break;
                    case "RA":
                        MenuRicercaPerAlbum();
                        break;
                    case "A":
                        MenuRicercaPerArtista();
                        break;
                    case "SA":
                        StatistichePerArtista();
                        break;
                    case "AS":
                        TrovaArtistiSimili();
                        break;
                }
            }
        }

        private static void TrovaArtistiSimili()
        {
            var trova = Scrobbles.Select(d=>d.Artist).Distinct().ToList();
            HierarchicalClustering.elaborate(trova);
        }

        private static void StatistichePerArtista()
        {
            Console.Write("Inserisci nome artista: ");
            var query = Console.ReadLine();
            var trova = Scrobbles.Where(c => c.Artist.ToUpper().Contains(query.ToUpper())).ToList();
            Console.WriteLine("Trovati {0} scrobbles", trova.Count);
            var eccoli = trova.GroupBy(d => d.Track.ToLower()).OrderByDescending(d=>d.Count());
            foreach (var s in eccoli)
                Console.WriteLine("{0} ({1})", s.Key,s.Count());
            PrintYearStatistics(trova);
            
             

        }

        private static void PrintYearStatistics(List<Scrobble> trova)
        {
            var starty = trova.Select(c => c.Data.Year).Min();
            List<KeyValuePair<int, int>> anni = new List<KeyValuePair<int, int>>();
            for (var anno = starty; anno <= DateTime.Now.Year; anno++)
            {
                var scobbly = trova.Where(d => d.Data.Year.Equals(anno)).Count();
                anni.Add(new KeyValuePair<int, int>(anno, scobbly));
            }
            var max = anni.Select(d => d.Value).Max();
            foreach (var kk in anni)
            {
                Console.Write("{0} ", kk.Key);
                var numm = kk.Value * 100 / max;
                for (var i = 0; i < numm; i++)
                {
                    Console.Write("▮");
                }
                Console.WriteLine(" "+ kk.Value);
            }
        }

        private static void Aggiorna()
        {
            var token = "974a5ebc077564f72bd639d122479d4b";
            var user = "otrebla86";
            ApiHelper apiHelper = new ApiHelper();
            var info = ApiHelper.GetUserInfo(user, token);
            var limit = 25;
            var total_pages = info.playcount / limit + 1;
            List<Scrobble> newList = new List<Scrobble>();
            long oldperc = 0;
            int start = 1;
            if (File.Exists("aggiorna.json"))
            {
                start = JsonConvert.DeserializeObject<int>(File.ReadAllText("aggiorna.json"));
                newList = JsonConvert.DeserializeObject<List<Scrobble>>(File.ReadAllText("data.json"));
            }
            for (int page = start; page <= total_pages; page++)
            {
                List<Track> data = null;
                while (data == null)
                {
                    data = ApiHelper.GetTracks(token, user, limit, page).Result;
                    foreach(var song in data)
                    {
                        song.duration = apiHelper.GetSongDuration(song.artist.name, song.name, token);
                    }
                    data.RemoveAll(c => c.date == null);
                    if (data == null) Console.Write("retry...");
                }
                newList.AddRange(data.Select(c => new Scrobble(c.artist.name, c.album.name, c.name, (c.date?.date ?? new DateTime(0, 0, 0)),c.duration)));
                File.WriteAllText("data.json", JsonConvert.SerializeObject(newList, Formatting.Indented));
                File.WriteAllText("aggiorna.json", JsonConvert.SerializeObject(page));
                var perc = page * 100 / total_pages;
                if (oldperc != perc)
                {
                    Console.Write(perc + "%...");
                    oldperc = perc;
                }
            }
            Scrobbles = newList;
        }
        private static void AggiornaDifferenziale()
        {
            var token = "974a5ebc077564f72bd639d122479d4b";
            var user = "otrebla86";
            var info = ApiHelper.GetUserInfo(user, token);
            var limit = 1000;
            var from = Scrobbles.OrderByDescending(c => c.Data).Take(1).FirstOrDefault().Data.AddSeconds(1);


            List<Track> data = ApiHelper.GetTracksFromDate(token, user, limit, from).Result;
            if (data == null)
                return;
            data.RemoveAll(c => c.date == null);
            Scrobbles.AddRange(data.Select(c => new Scrobble(c.artist.name, c.album.name, c.name, (c.date?.date ?? new DateTime(0, 0, 0)),c.duration)));
            Scrobbles = Scrobbles.OrderByDescending(c => c.Data).ToList();
            File.WriteAllText("data.json", JsonConvert.SerializeObject(Scrobbles,Formatting.Indented));
            Console.WriteLine("Aggiunte " + data.Count + " nuove tracce");
        }
        private static void ClassificaPeriodiSeiMesi()
        {
            var inizio = Scrobbles.Select(c => c.Data).Min();
            var fine = Scrobbles.Select(c => c.Data).Max();
            var numgiorni = (fine - inizio).Days;
            var listperiodi = new List<Periodo>();
            var perc = 0;
            var percold = 0;
            for (int i = 0; i < numgiorni; i++)
            {
                perc = i * 100 / numgiorni;
                if (perc!=percold)
                {
                    percold = perc;
                    Console.WriteLine(perc);
                }
                var datainizio = inizio.AddDays(i);
                var datafine = datainizio.AddMonths(1);
                var numscrobbl = Scrobbles.Where(c => c.Data >= datainizio && c.Data < datafine).Count();
                listperiodi.Add(new Periodo
                {
                     Inizio = datainizio,
                     Fine = datafine,
                     NumeroAscolti = numscrobbl
                });
            }
            var primoasc = listperiodi.Select(c => c.NumeroAscolti).Max();
            var primo = listperiodi.Where(c => c.NumeroAscolti == primoasc).FirstOrDefault();
            listperiodi.Remove(primo);
            var secondoasc = listperiodi.Select(c => c.NumeroAscolti).Max();
            var secondo = listperiodi.Where(c => c.NumeroAscolti == secondoasc).FirstOrDefault();
            listperiodi.Remove(secondo);
            var terzoasc = listperiodi.Select(c => c.NumeroAscolti).Max();
            var terzo = listperiodi.Where(c => c.NumeroAscolti == terzoasc).FirstOrDefault();
            listperiodi.Remove(terzo);
            var quartoasc = listperiodi.Select(c => c.NumeroAscolti).Max();
            var quarto = listperiodi.Where(c => c.NumeroAscolti == quartoasc).FirstOrDefault();
            listperiodi.Remove(quarto);
            var quintoasc = listperiodi.Select(c => c.NumeroAscolti).Max();
            var quinto = listperiodi.Where(c => c.NumeroAscolti == quintoasc).FirstOrDefault();
            listperiodi.Remove(quinto);
            var sestoasc = listperiodi.Select(c => c.NumeroAscolti).Max();
            var sesto = listperiodi.Where(c => c.NumeroAscolti == sestoasc).FirstOrDefault();
            listperiodi.Remove(sesto);
            Console.WriteLine(primo);
            Console.WriteLine(secondo);
            Console.WriteLine(terzo);
            Console.WriteLine(quarto);
            Console.WriteLine(quinto);
            Console.WriteLine(sesto);
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
        private static void MenuRicercaPerArtista()
        {
            Console.Write("Inserisci query di ricerca: ");
            var query = Console.ReadLine();
            var mlContext = new MLContext(seed: 0);
            var options = new TextFeaturizingEstimator.Options
            {
                StopWordsRemoverOptions = null, // Set to null to leave stop words intact
                KeepNumbers = true, // Default == false
                WordFeatureExtractor = new WordBagEstimator.Options { NgramLength = 2 },
                CaseMode = TextNormalizingEstimator.CaseMode.None // Default = Lower
            };
            var featurizer = mlContext.Transforms.Text.FeaturizeText("Features", options, "Text");
            var data = mlContext.Data.LoadFromEnumerable(Scrobbles.Select(d => d.Artist));
            var transformedText = featurizer.Fit(data).Transform(data);
            IDataView trainingData = transformedText;
            // Define trainer options.
            var optionss = new KMeansTrainer.Options
            {
                NumberOfClusters = 3065,
                OptimizationTolerance = 1e-6f,
                NumberOfThreads = 1
            };
            var pipeline = mlContext.Clustering.Trainers.KMeans(optionss);

            // Train the model.
            var model = pipeline.Fit(trainingData);
            VBuffer<float>[] centroids = default;

            var modelParams = model.Model;
            modelParams.GetClusterCentroids(ref centroids, out int k);
            //Console.WriteLine("Trovati {0} scrobbles", trova.Count);
            //foreach (var s in trova)
            //    Console.WriteLine("{0}   {1} - {2}", s.Data.ToString("dd/MM/yyyy HH:mm:ss"), s.Artist, s.Track);
        }

        private static void MenuRicercaPerTraccia()
        {
            Console.Write("Inserisci query di ricerca: ");
            var query = Console.ReadLine();
            var trova = Scrobbles.Where(c => c.Track.ToUpper().Contains(query.ToUpper())).ToList();
            Console.WriteLine("Trovati {0} scrobbles", trova.Count);
            foreach (var s in trova)
                Console.WriteLine("{0}   {1} - {2}", s.Data.ToString("dd/MM/yyyy HH:mm:ss"), s.Artist, s.Track);
            PrintYearStatistics(trova);
        }

        private static void MenuStatisticheGenerali()
        {
            Console.WriteLine("Inserisci prima data del range (GG/MM/AAAA)");
            var partenza = GetInput();
            DateTime datapartenza;
            if (string.IsNullOrEmpty(partenza))
                datapartenza = Scrobbles.Select(c => c.Data).Min();
            else
                datapartenza = DateTime.Parse(partenza);
            Console.WriteLine("Inserisci seconda data del range (GG/MM/AAAA)");
            var arrivo = GetInput();
            DateTime dataarrivo;
            if (string.IsNullOrEmpty(arrivo))
                dataarrivo = Scrobbles.Select(c => c.Data).Max();
            else
                dataarrivo = DateTime.Parse(arrivo);
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
            Console.WriteLine("******************************************************");
            Console.WriteLine("Artista più ascoltato: " + artista);
            Console.WriteLine("Album più ascoltato: " + albumpiasc);
            Console.WriteLine("Traccia più ascoltata: " + tracciapiasc);
            PrintYearStatistics(scrobbleintorange);
            Console.WriteLine("******************YEAR CHART*********************");
            var anni = scrobbleintorange.Select(c => c.Data.Year).Distinct();
            foreach(var anno in anni)
                Console.WriteLine("{0} -> {1}", anno, scrobbleintorange.Where(c=>c.Data.Year == anno).Count());
            Console.WriteLine("******************GIORNI VIRTUOSI*********************");

            //giorni virtuoso
            var IGiorni = scrobbleintorange.GroupBy(c => c.Data.Date).OrderByDescending(c => c.Count()).Take(10);
            int count = 1;
            foreach (var IGiorno in IGiorni)
            {
                if (IGiorno != null)
                {
                    var giorno = IGiorno.Key;
                    Console.WriteLine("Giorno più virtuoso #{2}: {0} ({1} ascolti)", giorno.ToString("dd/MM/yyyy"), IGiorno.Count(), count.ToString());
                    count++;
                }
            }
            Console.WriteLine("******************SETTIMANE VIRTUOSI*********************");

            //giorni virtuoso
            var ISettimane = scrobbleintorange.GroupBy(c => c.Data.Date.Year.ToString() + CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(c.Data,CalendarWeekRule.FirstDay,DayOfWeek.Friday).ToString("00")).OrderByDescending(c => c.Count()).Take(10);
            count = 1;
            foreach (var ISettimana in ISettimane)
            {
                if (ISettimana != null)
                {
                    var settimana = ISettimana.Key;
                    Console.WriteLine("Settimana più virtuosa #{2}: {0} ({1} ascolti)", settimana, ISettimana.Count(), count.ToString());
                    count++;
                }
            }
            Console.WriteLine("******************MESI VIRTUOSI*********************");

            //giorni virtuoso
            var IMESI = scrobbleintorange.GroupBy(c => c.Data.Date.Year.ToString() + c.Data.Date.Month.ToString("00")).OrderByDescending(c => c.Count()).Take(10);
            count = 1;
            foreach (var IMese in IMESI)
            {
                if (IMese != null)
                {
                    var mese = IMese.Key;
                    Console.WriteLine("Mese più virtuoso #{2}: {0} ({1} ascolti)", mese, IMese.Count(), count.ToString());
                    count++;
                }
            }
            Console.WriteLine("******************TRACCE NOTTURNE*********************");
            var tracceascoltatedinotte = GetTracceAscoltateDiNotte(scrobbleintorange);
            foreach (var t in tracceascoltatedinotte)
            {
                Console.WriteLine("{0} {1} - {2}", (NormalizzaData(t.Data)).ToString("dd/MM/yyyy HH:mm:ss"), t.Artist, t.Track);
            }
            Console.WriteLine("******************LISTENING CLOCK*********************");
            var lc = GetListeningClock(scrobbleintorange);
            var lcheader = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            var lwheader = new string[] { "Dom", "Lun", "Mar", "Mer", "Gio", "Ven", "Sab" };
            var lw = GetListeningWeek(scrobbleintorange);
            PrintArray(lcheader, lc);
            Console.WriteLine("******************LISTENING WEEK**********************");
            PrintArray(lwheader, lw);
            string[] apsheader;
            string[] apmheader;
            var aps = GetArtistiPerSettimana(scrobbleintorange, out apsheader);
            var apm = GetArtistiPerMese(scrobbleintorange);
            Console.WriteLine("******************WEEKLY ARTISTS**********************");
            foreach (var el in aps)
                Console.WriteLine(el);
            Console.WriteLine("******************MONTHLY ARTISTS*********************");
            foreach (var el in apm)
                Console.WriteLine(el);
            Console.WriteLine("******************YEAR AGO*********************");
            var year = DateTime.Now.Year;
            var now = DateTime.Now;
            var starty = scrobbleintorange.Select(c => c.Data.Year).Min();
            for (int i = starty; i <= year; i++)
            {
                Console.WriteLine("====================="+i+ "=====================:");
                string[] tya = GetYearsAgo(scrobbleintorange,year - i, now, true); //tracce
                string[] aya = GetYearsAgo(scrobbleintorange,year - i, now, false); //artisti
                foreach (var t in tya)
                    Console.WriteLine(t);
                foreach (var ay in aya)
                    Console.Write(ay + ",");
                Console.WriteLine("");
            }
            Console.ReadLine();
        }

        private static List<Chartelement> GetArtistiPerMese(List<Scrobble> scrobbles)
        {
            var max = scrobbles.Select(c => c.Data).Max();
            var min = scrobbles.Select(c => c.Data).Min();
            var primogiornomese = min;
            primogiornomese = primogiornomese.AddHours(-primogiornomese.Hour);
            primogiornomese = primogiornomese.AddMinutes(-primogiornomese.Minute);
            primogiornomese = primogiornomese.AddSeconds(-primogiornomese.Second);
            while ((int)primogiornomese.Day != 1)
                primogiornomese = primogiornomese.AddDays(-1);
            var ultimogiornomese = primogiornomese.AddMonths(1);
            var getnummesi = (max - min).Days / 30 + 1;
            var ret = new List<Chartelement>();
            for (int i = 0; i < getnummesi; i++)
            {
                var elem = new Chartelement();
                elem.Header = primogiornomese.ToString("dd/MM/yyyy") + "-" + ultimogiornomese.ToString("dd/MM/yyyy");
                var a = scrobbles.Where(c => c.Data >= primogiornomese && c.Data < ultimogiornomese)
                    .GroupBy(c => c.Artist)
                    .OrderByDescending(c => c.Count())
                    .FirstOrDefault();
                if (a != null)
                {
                    elem.Artist = a.Select(c => c.Artist).FirstOrDefault();
                    elem.TotalArtist = a.Where(c=>c.Artist == elem.Artist).Count();
                }
                elem.TotalListening = scrobbles.Where(c => c.Data >= primogiornomese && c.Data < ultimogiornomese).Count();
                ret.Add(elem);
                primogiornomese = primogiornomese.AddMonths(1);
                ultimogiornomese = ultimogiornomese.AddMonths(1);
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

        private static List<Chartelement> GetArtistiPerSettimana(IEnumerable<Scrobble> scrobbles, out string[] apsheader)
        {
            var max = scrobbles.Select(c => c.Data).Max();
            var min = scrobbles.Select(c => c.Data).Min();
            var primogiornosettimana = min;

            primogiornosettimana = primogiornosettimana.AddHours(-primogiornosettimana.Hour);
            primogiornosettimana = primogiornosettimana.AddMinutes(-primogiornosettimana.Minute);
            primogiornosettimana = primogiornosettimana.AddSeconds(-primogiornosettimana.Second);
            while ((int)primogiornosettimana.DayOfWeek != 0)
                primogiornosettimana = primogiornosettimana.AddDays(-1);
            var ultimogiornosettimana = primogiornosettimana.AddDays(7);
            var getnumsettimane = (max - min).Days / 7 + 1;
            var ret = new List<Chartelement>();
            apsheader = new string[getnumsettimane];
            for(int i = 0; i < getnumsettimane; i++)
            {
                var elem = new Chartelement();
                elem.Header = primogiornosettimana.ToString("dd/MM/yyyy")+"-"+ ultimogiornosettimana.ToString("dd/MM/yyyy");
                var a = scrobbles.Where(c => c.Data >= primogiornosettimana && c.Data < ultimogiornosettimana)
                    .GroupBy(c => c.Artist)
                    .OrderByDescending(c => c.Count())
                    .FirstOrDefault();
                if (a != null)
                {
                    elem.Artist = a.Select(c => c.Artist).FirstOrDefault();
                    elem.TotalArtist = a.Where(c => c.Artist == elem.Artist).Count();
                }
                elem.TotalListening = scrobbles.Where(c => c.Data >= primogiornosettimana && c.Data < ultimogiornosettimana).Count();
                ret.Add(elem);
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

        private static string[] GetYearsAgo(IEnumerable<Scrobble> Scrobbles, int yearago, DateTime now, bool mode)
        {
            var ret = new List<string>();
            var finestratemporale = 0;
            if (mode)
            {
                finestratemporale = 1; //in ore
                var addend = 0;
                while (ret.Count < 10)
                {
                    var finspan = TimeSpan.FromHours(1 + addend);
                    ret = Scrobbles.Where(c => c.Data > now.AddYears(-yearago).AddHours(-finspan.TotalHours) && c.Data < now.AddYears(-yearago).AddHours(finspan.TotalHours)).Select(c => string.Format("{0} {1} - {2}", (NormalizzaData(c.Data)).ToString("dd/MM/yyyy HH:mm:ss"), c.Artist, c.Track)).ToList();
                    addend++;
                }
            }
            else
            {
                finestratemporale = 1; //in ore
                var addend = 0;
                while (ret.Count < 10)
                {
                    var finspan = TimeSpan.FromHours(1 + addend);
                    ret = Scrobbles.Where(c => c.Data > now.AddYears(-yearago).AddHours(-finspan.TotalHours) && c.Data < now.AddYears(-yearago).AddHours(finspan.TotalHours))
                        .Select(c => c.Artist).Distinct().ToList();
                    addend++;
                }
            }
            return ret.ToArray();
            

            
        }

    }
}
