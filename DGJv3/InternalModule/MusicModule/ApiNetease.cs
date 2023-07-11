using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGJv3.InternalModule
{
    internal sealed class ApiNetease : ApiBaseModule
    {
        private const string API_PROTOCOL = "http://";
        private const string API_HOST = "music.163.com";
        private const string API_PATH = "/api";

        internal ApiNetease()
        {
            SetServiceName("netease");
            SetInfo("网易云音乐[暂不支持登录]", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索网易云音乐的歌曲");
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            try
            {
                return $"https://music.163.com/song/media/outer/url?id={songItem.SongId}.mp3";
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
                var response = Fetch(API_PROTOCOL, API_HOST,
                    API_PATH +
                    $"/song/lyric?id={songInfo.Id}&lv=1&kv=1&tv=-1");
                var json = JObject.Parse(response);

                return json.SelectToken("lrc.lyric")?.ToString();
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
                var response = Fetch(API_PROTOCOL, API_HOST,
                    API_PATH +
                    $"/v6/playlist/detail?id={id}");
                var json = JObject.Parse(response);
                return (json.SelectToken("playlist.tracks") as JArray)?.Select(song =>
                {
                    SongInfo songInfo;
                    try
                    {
                        songInfo = new SongInfo(
                            this,
                            song["id"].ToString(),
                            song["name"].ToString(),
                            (song["ar"] as JArray).Select(x => x["name"].ToString()).ToArray()
                        );
                    }
                    catch (Exception ex)
                    { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }
                    songInfo.Lyric = null;//在之后再获取Lyric

                    //songInfo.Lyric = GetLyricById(songInfo.Id);
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
                var response = Fetch(API_PROTOCOL, API_HOST,
                    API_PATH +
                    $"/search/pc?s={keyword}&limit=20&offset=0&type=1");
                var json = JObject.Parse(response);
                var song = (json.SelectToken("result.songs") as JArray)?[0] as JObject;

                SongInfo songInfo;
                songInfo = new SongInfo(
                    this,
                    song["id"].ToString(),
                    song["name"].ToString(),
                    (song["artists"] as JArray).Select(x => x["name"].ToString()).ToArray()
                );
                songInfo.Lyric = GetLyric(songInfo);
                return songInfo;
            }
            catch (Exception ex)
            { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }
        }
    }
}