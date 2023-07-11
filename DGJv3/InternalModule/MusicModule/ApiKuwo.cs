using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGJv3.InternalModule
{
    internal sealed class ApiKuwo : ApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "kuwo.cn";
        private const string API_PATH = "";

        internal ApiKuwo()
        {
            SetServiceName("kuwo");
            SetInfo("酷我音乐[暂不支持登录]", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索酷我音乐的歌曲");
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            try
            {
                return Fetch(API_PROTOCOL, "antiserver.kuwo.cn", API_PATH + $"/anti.s?uesless=/resource/&format=mp3&rid=MUSIC_{songItem.SongId}&response=url&type=convert_url");
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
                var response = Fetch(API_PROTOCOL, "m.kuwo.cn",
                    API_PATH +
                    $"/newh5/singles/songinfoandlrc?musicId={songInfo.Id}");
                var json = JObject.Parse(response);
                return string.Join(Environment.NewLine,
                    (json.SelectToken("data.lrclist") as JArray)?
                    .Select(x => "[" + TimeSpan.FromSeconds(double.Parse((string)x["time"])).ToString(@"mm\:ss\.ff") + "]" + x["lineLyric"])
                    .ToArray());
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
                var response = Fetch(API_PROTOCOL, "nplserver.kuwo.cn",
                    API_PATH +
                    $"/pl.svc?op=getlistinfo&pn=0&rn=200&encode=utf-8&keyset=pl2012&pid={id}");
                var json = JObject.Parse(response);
                return (json.SelectToken("musiclist") as JArray)?.Select(song =>
                {
                    SongInfo songInfo;
                    try
                    {
                        songInfo = new SongInfo(
                            this,
                            song["id"].ToString(),
                            song["name"].ToString(),
                            song["artist"].ToString().Split('&')
                        );
                    }
                    catch (Exception ex)
                    { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }
                    songInfo.Lyric = null;
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
                var response = Fetch(API_PROTOCOL, "search.kuwo.cn",
                    API_PATH +
                    $"/r.s?ft=music&itemset=web_2013&client=kt&rformat=json&encoding=utf8&all={keyword}&pn=0&rn=20");
                var json = JObject.Parse(response);
                var song = (json.SelectToken("abslist") as JArray)?[0] as JObject;

                SongInfo songInfo;
                songInfo = new SongInfo(
                    this,
                    ((string)song["MUSICRID"]).Split('_')[1],
                    song["NAME"].ToString(),
                    song["ARTIST"].ToString().Split('&')
                );
                songInfo.Lyric = GetLyric(songInfo);
                return songInfo;
            }
            catch (Exception ex)
            { Log($"歌曲信息获取结果错误(ex:{ex.Message})"); return null; }
        }
    }
}