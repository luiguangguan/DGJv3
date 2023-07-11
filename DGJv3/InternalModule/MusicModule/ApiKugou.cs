using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DGJv3.InternalModule
{
    internal sealed class ApiKugou : ApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "www.kugou.com";
        private const string API_PATH = "/yy";
        private const string COOKIES = "kg_mid=355721c2749fe30472161adf09b5748d";

        internal ApiKugou()
        {
            SetServiceName("kugou");
            SetInfo("酷狗音乐[暂不支持登录]", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索酷狗音乐的歌曲");
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            try
            {
                FetchConfig fc = new FetchConfig
                {
                    host = "wwwapi.kugou.com",
                    path = API_PATH + $"/index.php?r=play/getdata&hash={songItem.SongId}&album_id={songItem.GetInfo("albumid")}",
                    referer = "https://www.kugou.com/",
                    cookie = COOKIES
                };
                string resualt = Fetch(fc);
                JObject dlurlobj = JObject.Parse(resualt);
                string url = dlurlobj.SelectToken("data.play_url")?.ToString();
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
            try
            {
                FetchConfig fc = new FetchConfig
                {
                    host = API_HOST,
                    path = API_PATH + $"/index.php?r=play/getdata&hash={songInfo.Id}&album_id={songInfo.GetInfo("albumid")}",
                    referer = "https://www.kugou.com/",
                    cookie = COOKIES
                };
                var response = Fetch(fc);

                var json = JObject.Parse(response);
                var lyric = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(json.SelectToken("data.lyrics")?.ToString()));
                return lyric;
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
                    prot = "http://",
                    host = "m.kugou.com",
                    path = $"/plist/list/{id}?json=true",
                    referer = $"http://m.kugou.com/plist/list/{id}",
                    cookie = COOKIES
                };
                var response = Fetch(fc);

                var json = JObject.Parse(response);
                return (json.SelectToken("list.list.info") as JArray)?.Select(song =>
                {
                    SongInfo songInfo;
                    try
                    {
                        string[] filename = song["filename"].ToString().Split(new char[3] { ' ', '-', ' ' });
                        songInfo = new SongInfo(
                            this,
                            song["hash"].ToString(),
                            filename[0],
                            filename[1].Split('\u3001')
                        );
                        songInfo.SetInfo("albumid", song["album_id"].ToString());
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
                var response = Fetch(API_PROTOCOL, "songsearch.kugou.com",
                    $"/song_search_v2?keyword={keyword}&pagesize=1");
                var json = JObject.Parse(response);
                var song = json.SelectToken("data.lists[0]");
                SongInfo songInfo;
                songInfo = new SongInfo(
                    this,
                    song["FileHash"].ToString(),
                    song["SongName"].ToString(),
                    song["SingerName"].ToString().Split('\u3001')
                );
                songInfo.SetInfo("albumid", song["AlbumID"].ToString());
                songInfo.Lyric = GetLyric(songInfo);

                return songInfo;
            }
            catch (Exception ex)
            { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }
        }
    }
}