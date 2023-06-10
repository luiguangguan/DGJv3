using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3
{
    class Config
    {

        [JsonProperty("admcmdale")]
        public bool AdminCmdEnable { get; set; }=true;
        [JsonProperty("fmtcfg")]
        public bool FormatConfig { get; set; }=false;
        [JsonProperty("ckeudp")]
        public bool CheckUpdate { get; set; }=true;
        [JsonProperty("ptyp")]
        public PlayerType PlayerType { get; set; } = PlayerType.DirectSound;

        [JsonProperty("pdsd")]
        public Guid DirectSoundDevice { get; set; } = Guid.Empty;

        [JsonProperty("pwed")]
        public int WaveoutEventDevice { get; set; } = -1;

        [JsonProperty("pvol")]
        public float Volume { get; set; } = 0.5f;
        [JsonProperty("pvol2")]
        public float Volume2 { get; set; } = 0.5f;

        [JsonProperty("pple")]
        public bool IsPlaylistEnabled { get; set; } = true;

        [JsonProperty("mpid")]
        public string PrimaryModuleId { get; set; }

        [JsonProperty("msid")]
        public string SecondaryModuleId { get; set; }

        [JsonProperty("dmts")]
        public uint MaxTotalSongNum { get; set; } = 10;

        [JsonProperty("dmps")]
        public uint MaxPersonSongNum { get; set; } = 2;

        [JsonProperty("up")]
        public bool IsUserPrior { get; set; } = true;

        [JsonProperty("lrd")]
        public bool IsLogRedirectDanmaku { get; set; } = false;

        [JsonProperty("ldll")]
        public int LogDanmakuLengthLimit { get; set; } = 20;

        [JsonProperty("blst")]
        public BlackListItem[] Blacklist { get; set; } = new BlackListItem[0];
        [JsonProperty("cpm")]
        public PlayMode CurrentPlayMode { get; set; } = PlayMode.LooptListPlay;

        [JsonProperty("lsid")]
        public string LastSongId { get; set; }

        [JsonProperty("eabqmsg")]
        public bool EnableQueueMsg { get; set; } = true;

        [JsonProperty("qmsgmxstt")]
        public int QueueMsgMaxStayTime { get; set; } = 5;

        [JsonProperty("eabkpqmsg")]
        public bool EnableKeepLastQueueMsg { get; set; } = false;

        [JsonProperty("kpqmsgct")]
        public int KeepQueueMsgCount { get; set; } = 1;

        [JsonProperty("msgctnmxsz")]
        public int MsgContainerMaxSize { get; set; } = 5;
        [JsonProperty("msglilen")]
        public int MsgLineLength { get; set; } = 10;

        [JsonProperty("skpsgvot")]
        public int SkipSongVote { get; set; } = 3;

        [JsonProperty("ift")]
        public Dictionary<string, OutputInfo> InfoTemplates { get; set; } = new Dictionary<string, OutputInfo>();

        [JsonProperty("plst")]
        public SongInfo[] Playlist { get; set; } = new SongInfo[0];

        //public SongItem[] BiliUserSongs { get; set; } = new SongItem[0];

        [JsonProperty("sbtp")]
        public string ScribanTemplate { get; set; } = "正在播放：{{ 当前播放 }}-{{ 当前歌手 }}-{{ 当前点歌用户 }}-{{ 当前模块 }}\r\n" +
            "当前歌词：{{当前歌词}}\r\n" +
            "下句歌词：{{下句歌词}}\r\n" +
            "播放模式：{{播放模式}}\r\n" +
            "播放模式名称：{{播放模式名称}}\r\n" +
            "当前音量：{{当前音量}}\r\n" +
            "切歌票数：{{切歌票数}}\r\n" +
            "当前票数：{{当前票数}}\r\n" +
            "下一首播放：{{ 下一首播放 }}-{{ 下一首歌手 }}-{{ 下一首点歌用户 }}-{{ 下一首模块 }}\r\n" +
            "播放进度 {{当前播放时间}}/{{当前总时间}}\r\n" +
            "当前列表中有 {{ 歌曲数量 }} 首歌\r\n" +
            "还可以再点 {{ 总共最大点歌数量 - 歌曲数量 }} 首歌\r\n" +
            "每个人可以点 {{ 单人最大点歌数量 }} 首歌\r\n" +
            "\r\n" +
            "歌名 - 点歌人 - 歌手 - 歌曲平台\r\n{{~ for 歌曲 in 播放列表 ~}}\r\n" +
            "{{ 歌曲.歌名 }} - {{  歌曲.点歌人 }} - {{ 歌曲.歌手 }} - {{ 歌曲.搜索模块 }}\r\n" +
            "{{~ end ~}}\r\n" +
            "\r\n" +
            "消息队列:\r\n" +
            "{{~ for 消息 in 消息队列 ~}}\r\n" +
            "{{消息.信息}}\r\n" +
            "{{消息.时间}}\r\n" +
            "{{~ end ~}}\r\n" +
            "\r\n" +
            "投票用户列表:\r\n" +
            "{{~ for 用户 in 投票用户列表 ~}}\r\n" +
            "{{用户}}\r\n" +
            "{{~ end ~}}\r\n" +
            "\r\n" +
            "空闲歌单：\r\n" +
            "{{~ for 歌曲 in 空闲歌单 ~}}\r\n" +
            "{{歌曲.播放状态}}-{{歌曲.歌名}}-{{歌曲.歌手}}-{{歌曲.歌曲id}}-{{歌曲.搜索模块}}\r\n" +
            "{{~ end ~}}";

        public Config()
        {
        }

#pragma warning disable CS0168 // 声明了变量，但从未使用过
        internal static Config Load(bool reset = false)
        {
            Config config = new Config();
            if (!reset)
            {
                try
                {
                    var str = File.ReadAllText(Utilities.ConfigFilePath, Encoding.UTF8);
                    config = JsonConvert.DeserializeObject<Config>(str);
                }

                catch (Exception ex)
                {
                }
            }
            return config;
        }

        internal static void Write(Config config)
        {
            try
            {
                Formatting fmt= Formatting.None;
                if(config.FormatConfig)
                {
                    fmt = Formatting.Indented;
                }
                File.WriteAllText(Utilities.ConfigFilePath, JsonConvert.SerializeObject(config, fmt), Encoding.UTF8);
            }
            catch (Exception ex)
            {
            }
        }
#pragma warning restore CS0168 // 声明了变量，但从未使用过
    }
}
