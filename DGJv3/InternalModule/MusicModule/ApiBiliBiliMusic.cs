using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGJv3.InternalModule
{
    internal sealed class ApiBiliBiliMusic : ApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "www.bilibili.com";
        private const string API_PATH = "/audio";

        internal ApiBiliBiliMusic()
        {
            SetServiceName("kugou");
            SetInfo("Bilibili音乐[暂不支持登录]", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索Bilibili音乐的歌曲");
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            try
            {
                FetchConfig fc = new FetchConfig
                {
                    host = API_HOST,
                    path = API_PATH + $"/music-service-c/web/url?sid={songItem.SongId}",
                    referer = API_PROTOCOL + API_HOST + API_PATH,
                    gzip = true
                };
                string resualt = Fetch(fc);

                JObject dlurlobj = JObject.Parse(resualt);
                string url = dlurlobj.SelectToken("data.cdns[0]")?.ToString();
                return url;
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

        protected override string GetLyric(SongInfo songInfo)
        {
            //Bilibi]li音乐频道目前不支持动态歌词格式，暂时先堆一起再说。
            try
            {
                FetchConfig fc = new FetchConfig
                {
                    host = API_HOST,
                    path = API_PATH + $"/music-service-c/web/song/info?sid={songInfo.Id}",
                    referer = API_PROTOCOL + API_HOST + API_PATH,
                    gzip = true
                };
                string url = JObject.Parse(Fetch(fc))
                        .SelectToken("data.lyric")
                    ?.ToString();
                if (String.IsNullOrWhiteSpace(url)) return String.Empty;
                return "[00:00]" + Fetch(new FetchConfig(url) { referer = API_PROTOCOL + API_HOST + API_PATH })
                    .Replace("\n", "    ");
            }
            catch (Exception ex)
            {
                Log($"歌词下载错误：{songInfo.Name}(ex:{ex.Message})");
                return null;
            }
        }

        protected override List<SongInfo> GetPlaylist(string id)
        {
            try
            {
                FetchConfig fc = new FetchConfig
                {
                    host = API_HOST,
                    path = API_PATH + $"/music-service-c/web/song/of-menu?pn=1&ps=100&sid={id}",
                    referer = API_PROTOCOL + API_HOST + API_PATH,
                    gzip = true
                };
                var json = JObject.Parse(Fetch(fc));
                return (json.SelectToken("data.data") as JArray)?.Select(song =>
                {
                    SongInfo songInfo;
                    try
                    {
                        songInfo = new SongInfo(
                            this,
                            song["id"].ToString(),
                            song["title"].ToString(),
                            song["author"].ToString().Split(new char[3] { ' ', '·', ' ' })
                        );
                        songInfo.SetInfo("referer", API_PROTOCOL + API_HOST + API_PATH);
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
                var response = Fetch(API_PROTOCOL, "api.bilibili.com",
                    API_PATH + $"/music-service-c/s?search_type=audio&page=0&pagesize=1&keyword={keyword}");
                var json = JObject.Parse(response);
                var song = json.SelectToken("data.result[0]");
                SongInfo songInfo;
                songInfo = new SongInfo(
                    this,
                    song["id"].ToString(),
                    song["title"].ToString(),
                    song["author"].ToString().Split(new char[3] { ' ', '·', ' ' })
                );
                songInfo.Lyric = GetLyric(songInfo);
                songInfo.SetInfo("referer", API_PROTOCOL + API_HOST + API_PATH);
                return songInfo;
            }
            catch (Exception ex)
            { Log($"歌曲信息获取结果错误：{keyword}(ex:{ex.Message})" ); return null; }
        }
    }
}