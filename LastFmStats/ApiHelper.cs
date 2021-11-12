using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LastFmStats
{
    public class ApiHelper
    {
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
                var uts = (Int32)(from.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                var url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={token}&format=json&from={uts}&limit={limit}";
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
            catch (Exception ex)
            {
                return null;
            }
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
        public UserInfo user {get;set;}
    }
}
