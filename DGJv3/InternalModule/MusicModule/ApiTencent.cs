using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGJv3.InternalModule
{
    internal sealed class ApiTencent : ApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "u.y.qq.com";
        private const string API_PATH = "/cgi-bin";

        internal ApiTencent()
        {
            SetServiceName("tencent");
            SetInfo("QQ音乐[暂不支持登录]", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索QQ音乐的歌曲");
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            try
            {
                string cfg = $"/musicu.fcg?data=%7b%22req_0%22%3a%7b%22module%22%3a%22vkey.GetVkeyServer%22%2c%22method%22%3a%22CgiGetVkey%22%2c%22param%22%3a%7b%22guid%22%3a%220%22%2c%22songmid%22%3a%5b%22{songItem.SongId}%22%5d%2c%22songtype%22%3a%5b0%5d%2c%22uin%22%3a%220%22%2c%22loginflag%22%3a1%2c%22platform%22%3a%2220%22%7d%7d%7d";
                JObject dlurlobj = JObject.Parse(Fetch(API_PROTOCOL, API_HOST, API_PATH + cfg));
                //string filename = "C400" + songInfo.SongId + ".m4a";
                string purl = dlurlobj.SelectToken("req_0.data.midurlinfo[0].purl")?.ToString();
                string host = dlurlobj.SelectToken("req_0.data.sip[0]")?.ToString();
                return host + purl;
            }
            catch (Exception ex)
            {
                Log($"歌曲不能下载：{songItem.SongName}(ex:{ex.Message})");
                return null;
            }
        }

        protected override string GetLyric(SongItem songItem)
        {
            return GetLyric(songItem.Info);
        }

        protected string GetLyric(SongInfo songInfo)
        {
            try
            {
                var response = Fetch(API_PROTOCOL,
                    "c.y.qq.com",
                    $"/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={songInfo.Id}&format=json&nobase64=1",
                    null,
                    "https://y.qq.com/portal/player.html");
                var json = JObject.Parse(response);

                return json["lyric"]?.ToString();
            }
            catch (Exception ex)
            {
                Log($"歌词下载错误：{songInfo.Name}(ex:{ex.Message})");
                return null;
            }
        }

        protected override List<SongInfo> GetPlaylist(string keyword)
        {
            try
            {
                var response = Fetch(API_PROTOCOL, "c.y.qq.com",
                    $"/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg?type=1&json=1&utf8=1&onlysong=0&disstid={keyword}&format=jsonp&hostUin=0&format=jsonp&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0",
                    null,
                    $"https://y.qq.com/n/yqq/playlist/{keyword}.html");

                response = response.Substring("jsonCallback(".Length, response.Length - "jsonCallback()".Length);
                var json = JObject.Parse(response);
                return (json.SelectToken("cdlist[0].songlist") as JArray)?.Select(song =>
                {
                    SongInfo songInfo;

                    try
                    {
                        songInfo = new SongInfo(
                            this,
                            song["songmid"].ToString(),
                            song["songname"].ToString(),
                            (song["singer"] as JArray).Select(x => x["name"].ToString()).ToArray()
                        );
                    }
                    catch (Exception ex)
                    { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }

                    return songInfo;
                }).ToList();
            }
            catch (Exception ex)
            {
                Log($"歌单下载错误(ex:{ex.Message})");
                return null;
            }
        }

        protected override SongInfo Search(string keyword)
        {
            try
            {
                var response = Fetch("https://", "c.y.qq.com",
                      $"/splcloud/fcgi-bin/smartbox_new.fcg?key={keyword}&format=json");
                var json = JObject.Parse(response);
                var song = json.SelectToken("data.song.itemlist[0]");
                SongInfo songInfo;
                songInfo = new SongInfo(
                    this,
                    song["mid"].ToString(),
                    song["name"].ToString(),
                    //(song["singer"] as JArray).Select(x => x["name"].ToString()).ToArray()
                    song["singer"].ToString().Split('/')
                );
                songInfo.Lyric = GetLyric(songInfo);

                return songInfo;
            }
            catch (Exception ex)
            { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }
        }

        public override void SongDownloadFail(string songid, Exception ex)
        {

        }
    }
}