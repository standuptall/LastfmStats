using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LastFmStats
{
    public class ApiHelper
    {
        private List<TrackInfoResponse> _tracks = new List<TrackInfoResponse>();

        public static UserInfo GetUserInfo(string user, string token)
        {
            try
            {
                UserInfo ret = null;
                var urlinfo = $"https://ws.audioscrobbler.com/2.0/?method=user.getinfo&user={user}&api_key={token}&format=json";
                var http = WebRequest.CreateDefault(new Uri(urlinfo));
                http.Method = "GET";
                var response = http.GetResponse();
                Stream s = response.GetResponseStream();
                List<byte> uj = new List<byte>();
                byte[] res = new byte[1024];
                while (s.Read(res, 0, 1024)>0)
                {
                    uj.AddRange(res);
                }
                var st = Encoding.UTF8.GetString(uj.ToArray());
                var ress = JsonConvert.DeserializeObject<UserInfoResponse>(st);
                return ress.user;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        public int GetSongDuration(string artist, string track,string token)
        {
            var trova = this._tracks.Where(c=>c.track.artist.name.Trim().ToLower().Equals(artist.Trim().ToLower())
            && c.track.name.Trim().ToLower().Equals(track.Trim().ToLower())
            ).FirstOrDefault();
            if (trova != null)
                return int.Parse(trova.track.duration);
            try
            {
                int ret = 0;
                artist = artist.Replace(" ", "+");
                track = track.Replace(" ", "+");
                var urlinfo = $"https://ws.audioscrobbler.com/2.0/?method=track.getinfo&artist={artist}&api_key={token}&track={track}&format=json";
                var http = WebRequest.CreateDefault(new Uri(urlinfo));
                http.Method = "GET";
                var response = http.GetResponse();
                Stream s = response.GetResponseStream();
                List<byte> uj = new List<byte>();
                byte[] res = new byte[1024];
                while (s.Read(res, 0, 1024) > 0)
                {
                    uj.AddRange(res);
                }
                var st = Encoding.UTF8.GetString(uj.ToArray());
                var ress = JsonConvert.DeserializeObject<TrackInfoResponse>(st);
                if (ress!=null && ress.track != null)
                {
                    this._tracks.Add(ress);
                    return int.Parse(ress.track.duration);
                }
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        internal async static Task<List<Track>> GetTracks(string token, string user, int limit, int page)
        {
            try
            {
                var url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={token}&format=json&page={page}&limit={limit}";
                using (var cc = new HttpClient())
                {
                    var res = await cc.GetAsync(url);
                    var content = res.Content;
                    var rsip = await content.ReadAsStringAsync();
                    rsip = rsip.Replace("#text", "text");
                    var ret = JsonConvert.DeserializeObject<TracksResponse>(rsip);
                    return ret.recenttracks.track;
                }
                //    var http = WebRequest.CreateDefault(new Uri(url));
                //http.Method = "GET";
                //var response = http.GetResponse();
                //Stream s = response.GetResponseStream();
               
                //byte[] res = new byte[response.ContentLength];
                //var st = Encoding.UTF8.GetString(res);
                //st = st.Replace("#text", "text");
                //var ret = JsonConvert.DeserializeObject<TracksResponse>(st);
                //return ret.recenttracks.track;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        internal static async Task<List<Track>> GetTracksFromDate(string token, string user, int limit, DateTime from)
        {
            try
            {
                var ttoot = new DateTime(from.Year, from.Month, DateTime.DaysInMonth(from.Year, from.Month), 23, 59, 59, 999);
                var uts = (Int32)(from.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                var utsto = (Int32)(ttoot.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                var track = new List<Track>();

                var url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={token}&format=json&from={uts}&to={utsto}&limit={limit}";
                while (true)
                {
                    using (var cc = new HttpClient())
                    {
                        var res = await cc.GetAsync(url);
                        var content = res.Content;
                        var rsip = await content.ReadAsStringAsync();
                        rsip = rsip.Replace("#text", "text");
                        var ret = JsonConvert.DeserializeObject<TracksResponse>(rsip);
                        if (ret.recenttracks != null)
                        {
                            track.AddRange(ret.recenttracks.track);
                            break;
                        }
                    }
                }
                if (track.Count >= 1000)
                    return await GetTracksFromDatePartitioned(token, user, limit, uts,utsto);
                return track;
            }
            catch
            {
                return new List<Track>();
            }
        }
        internal static async Task<List<Track>> GetTracksFromDatePartitioned(string token, string user, int limit, int uts,int utsto)
        {
            try
            {
                var range = utsto - uts;
                var step = range / 2;
                var step1da = uts;
                var step1a = uts + step;
                var step2da = uts + step + 1;
                var step2a = utsto;
                
                var track = new List<Track>();

                var url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={token}&format=json&from={step1da}&to={step1a}&limit={limit}";
                while (true)
                {
                    using (var cc = new HttpClient())
                    {
                        var res = await cc.GetAsync(url);
                        var content = res.Content;
                        var rsip = await content.ReadAsStringAsync();
                        rsip = rsip.Replace("#text", "text");
                        var ret = JsonConvert.DeserializeObject<TracksResponse>(rsip);
                        if (ret.recenttracks != null)
                        {
                            if (ret.recenttracks.track.Count == 1000)
                                track.AddRange(await GetTracksFromDatePartitioned(token, user, limit, step1da, step1a));
                            else
                                track.AddRange(ret.recenttracks.track);
                            break;
                        }
                    }
                }
                url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={token}&format=json&from={step2da}&to={step2a}&limit={limit}";
                while (true)
                {
                    using (var cc = new HttpClient())
                    {
                        var res = await cc.GetAsync(url);
                        var content = res.Content;
                        var rsip = await content.ReadAsStringAsync();
                        rsip = rsip.Replace("#text", "text");
                        var ret = JsonConvert.DeserializeObject<TracksResponse>(rsip);
                        if (ret.recenttracks != null)
                        {
                            if (ret.recenttracks.track.Count == 1000)
                                track.AddRange(await GetTracksFromDatePartitioned(token, user, limit, step2da, step2a));
                            else
                                track.AddRange(ret.recenttracks.track);
                            break;
                        }
                    }
                }
                return track;
                //    var http = WebRequest.CreateDefault(new Uri(url));
                //http.Method = "GET";
                //var response = http.GetResponse();
                //Stream s = response.GetResponseStream();

                //byte[] res = new byte[response.ContentLength];
                //var st = Encoding.UTF8.GetString(res);
                //st = st.Replace("#text", "text");
                //var ret = JsonConvert.DeserializeObject<TracksResponse>(st);
                //return ret.recenttracks.track;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        internal static async Task<String> GetToken(string apikey)
        {
            try
            {
                var url = $"https://ws.audioscrobbler.com/2.0/?method=auth.gettoken&api_key={apikey}&format=json";
                using (var cc = new HttpClient())
                {
                    var res = await cc.GetAsync(url);
                    var content = res.Content;
                    var rsip = await content.ReadAsStringAsync();
                    var sd = JsonConvert.DeserializeObject(rsip);
                    return "";// sd["token"].toString();
                }
                //    var http = WebRequest.CreateDefault(new Uri(url));
                //http.Method = "GET";
                //var response = http.GetResponse();
                //Stream s = response.GetResponseStream();

                //byte[] res = new byte[response.ContentLength];
                //var st = Encoding.UTF8.GetString(res);
                //st = st.Replace("#text", "text");
                //var ret = JsonConvert.DeserializeObject<TracksResponse>(st);
                //return ret.recenttracks.track;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        internal static async Task<String> GetSession(string apikey,string token)
        {
            return "";
        }

        internal static async Task ScrobbleTrack(string token, string sessionKey, string artist, string track, string album, DateTime date)
        {
            var uts = (Int32)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var parameters = new List<KeyValuePair<String,String>>();
            parameters.Add(new KeyValuePair<string, string>("album", album.Trim()));
            parameters.Add(new KeyValuePair<string, string>("api_key", token.Trim()));
            parameters.Add(new KeyValuePair<string, string>("artist", artist.Trim()));
            parameters.Add(new KeyValuePair<string, string>("method", "track.scrobble"));
            parameters.Add(new KeyValuePair<string, string>("sk", sessionKey.Trim()));
            parameters.Add(new KeyValuePair<string, string>("timestamp", uts.ToString()));
            parameters.Add(new KeyValuePair<string, string>("track", track.Trim()));
            var url = $"https://ws.audioscrobbler.com/2.0/";
            using (var cc = new HttpClient())
            {
                var content = new MultipartFormDataContent();
                var sig = "";

                foreach (var ccc in parameters)
                {
                    content.Add(new StringContent(ccc.Value),ccc.Key);
                    sig += ccc.Key + ccc.Value;
                }
                sig = MD5Hash(sig);

                content.Add(new StringContent($"api_sig"),sig);
                var res = await cc.PostAsync(url, content);
                //var rsip = await content.ReadAsStringAsync();
                //rsip = rsip.Replace("#text", "text");
                //var ret = JsonConvert.DeserializeObject<TracksResponse>(rsip);
                //ret.recenttracks.track;
            }
        }
        public static string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text  
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));


            //get hash result after compute it  
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits  
                //for each byte  
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }
    }
    
    public class TracksResponseInner
    {
        public List<Track> track { get; set; }
    }
    public class TracksResponse
    {
        public TracksResponseInner recenttracks { get; set; }
    }
    public class Track
    {
        public StructData artist { get; set; }
        public string name { get; set; }
        public StructData album { get; set; }
        public DateTS date { get; set; }
        public int duration { get; set; }

    }
    
    public class DateTS
    {
        public long uts { get; set; }
        [JsonIgnore]
        public DateTime date { get {
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dateTime = dateTime.AddSeconds(uts).ToLocalTime();
                return dateTime;
            } }
    }
    public class StructData
    {
        public string mbid { get; set; }
        [JsonProperty("text")]
        public string name { get; set; }
    }
    public class UserInfo
    {
        public string country { get; set; }
        public int age { get; set; }
        public long playcount { get; set; }
        public int subscriber { get; set; }
        public string realname { get; set; }
        public string playlists { get; set; }
        public string bootstrap { get; set; }
    }
    public class UserInfoResponse
    {
        public UserInfo user { get; set; }
    }
    public class TrackInfoResponse
    {
        public TrackInfoInner track { get; set; }
    }
    public class TrackInfoInner
    {
        public string name { get; set; }
        public TrackArtist artist { get; set; }
        public  string duration { get; set; }
    }
    public class TrackArtist
    {
        public string name { get; set; }
    }
}
