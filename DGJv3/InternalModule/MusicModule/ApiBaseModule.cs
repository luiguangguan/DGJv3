using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DGJv3.InternalModule
{
    internal class ApiBaseModule : SearchModule
    {
        private string ServiceName;


        protected void SetServiceName(string name) => ServiceName = name;

        protected const string INFO_PREFIX = "";
        protected const string INFO_AUTHOR = "Genteure & perichr";
        protected const string INFO_EMAIL = "dgj@genteure.com;i@perichr.org";
        protected const string INFO_VERSION = "2.0";

        internal static int RoomId = 0;

        internal ApiBaseModule()
        {
            IsPlaylistSupported = true;
        }

        protected override DownloadStatus Download(SongItem songItem)
        {
            throw new NotImplementedException();
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            throw new NotImplementedException();
        }

        protected override string GetLyric(SongItem songItem)
        {
            return GetLyricById(songItem.SongId);
        }

        protected override string GetLyricById(string Id)
        {
            throw new NotImplementedException();
        }

        protected override List<SongInfo> GetPlaylist(string keyword)
        {
            throw new NotImplementedException();
        }

        protected override SongInfo Search(string keyword)
        {
            throw new NotImplementedException();
        }

        protected class FetchConfig
        {
            public string prot { get; set; } = "https://";
            public string host { get; set; }
            public string path { get; set; }
            public string data { get; set; } = null;
            public string referer { get; set; } = null;
            public string cookie { get; set; } = null;
            public bool gzip { get; set; } = false;

            private Regex reg = new Regex(@"(?imn)(?<prot>http[s]?://)(?<host>[^\/]+)(?<path>.*$)");

            public FetchConfig()
            {
            }

            public FetchConfig(string url)
            {
                Match m = reg.Match(url);
                this.prot = m.Groups["prot"].Value;
                this.host = m.Groups["host"].Value;
                this.path = m.Groups["path"].Value;
            }

            public FetchConfig(string prot, string host, string path, string data = null, string referer = null)
            {
                this.prot = prot;
                this.host = host;
                this.path = path;
                this.data = data;
                this.referer = referer;
            }
        }

        protected static string Fetch(string prot, string host, string path, string data = null, string referer = null)
        {
            return Fetch_exec(new FetchConfig(prot, host, path, data, referer));
        }

        protected static string Fetch(FetchConfig fc)
        {
            for (int retryCount = 0; retryCount < 4; retryCount++)
            {
                try
                {
                    return Fetch_exec(fc);
                }
                catch (WebException)
                {
                    if (retryCount >= 3)
                    {
                        throw;
                    }

                    continue;
                }
            }

            return null;
        }

        private static string Fetch_exec(string prot, string host, string path, string data = null, string referer = null)
        {
            return Fetch_exec(new FetchConfig(prot, host, path, data, referer));
        }

        private static string Fetch_exec(FetchConfig fc)
        {
            string address;
            if (GetDNSResult(fc.host, out string ip))
            {
                address = fc.prot + ip + fc.path;
            }
            else
            {
                address = fc.prot + fc.host + fc.path;
            }

            var request = (HttpWebRequest)WebRequest.Create(address);

            request.Timeout = 4000;
            request.Host = fc.host;
            request.UserAgent = "DMPlugin_DGJ/" + (BuildInfo.Appveyor ? BuildInfo.Version : "local") + " RoomId/" + RoomId.ToString();
            if (fc.referer != null)
            {
                request.Referer = fc.referer;
            }
            if (fc.cookie != null)
            {
                request.Headers.Add("Cookie", fc.cookie);
            }
            if (fc.data != null)
            {
                var postData = Encoding.UTF8.GetBytes(fc.data);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postData.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                }
            }
            var response = (HttpWebResponse)request.GetResponse();
            var stm = fc.gzip ? new GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress) : response.GetResponseStream();
            var responseString = new StreamReader(stm, Encoding.UTF8).ReadToEnd();
            return responseString;
        }

        private static string Fetch(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 10000;
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            return responseString;
        }

        private static bool GetDNSResult(string domain, out string result)
        {
            if (DNSList.TryGetValue(domain, out DNSResult result_from_d))
            {
                if (result_from_d.TTLTime > DateTime.Now)
                {
                    result = result_from_d.IP;
                    return true;
                }
                else
                {
                    DNSList.Remove(domain);
                    if (RequestDNSResult(domain, out DNSResult? result_from_api, out Exception exception))
                    {
                        DNSList.Add(domain, result_from_api.Value);
                        result = result_from_api.Value.IP;
                        return true;
                    }
                    else
                    {
                        result = null;
                        return false;
                    }
                }
            }
            else
            {
                if (RequestDNSResult(domain, out DNSResult? result_from_api, out Exception exception))
                {
                    DNSList.Add(domain, result_from_api.Value);
                    result = result_from_api.Value.IP;
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }

        private static bool RequestDNSResult(string domain, out DNSResult? dnsResult, out Exception exception)
        {
            dnsResult = null;
            exception = null;

            try
            {
                var http_result = Fetch("http://119.29.29.29/d?ttl=1&dn=" + domain);
                if (http_result == string.Empty)
                {
                    return false;
                }

                var m = regex.Match(http_result);
                if (!m.Success)
                {
                    exception = new Exception("HTTPDNS 返回结果不正确");
                    return false;
                }

                dnsResult = new DNSResult()
                {
                    IP = m.Groups[1].Value,
                    TTLTime = DateTime.Now + TimeSpan.FromSeconds(double.Parse(m.Groups[2].Value))
                };
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        public override void SongDownloadFail(string songid, Exception ex)
        {
            throw new NotImplementedException();
        }

        private static readonly Dictionary<string, DNSResult> DNSList = new Dictionary<string, DNSResult>();
        private static readonly Regex regex = new Regex(@"((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))\,(\d+)", RegexOptions.Compiled);

        private struct DNSResult
        {
            internal string IP;
            internal DateTime TTLTime;
        }
    }
}