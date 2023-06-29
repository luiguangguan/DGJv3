using BilibiliDM_PluginFramework;
using DGJv3.API;
using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web.Security;
using System.Windows.Threading;

namespace DGJv3
{
    class DanmuHandler : INotifyPropertyChanged
    {
        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<BlackListItem> Blacklist;

        private Player Player;

        private Downloader Downloader;

        private SearchModules SearchModules;

        private Dispatcher dispatcher;

        private UIFunction UIFunction;

        private new EventSafeQueue<object> MsgQueue;

        private ObservableCollection<SongInfo> Playlist;

        private TTSPlugin TTSPlugin;

        /// <summary>
        /// 最多点歌数量
        /// </summary>
        public uint MaxTotalSongNum { get => _maxTotalSongCount; set => SetField(ref _maxTotalSongCount, value, nameof(MaxTotalSongNum)); }
        private uint _maxTotalSongCount;

        public bool AdminCmdEnable { get => _adminCmdEnable; set => SetField(ref _adminCmdEnable, value, nameof(AdminCmdEnable)); }
        private bool _adminCmdEnable;

        /// <summary>
        /// 每个人最多点歌数量
        /// </summary>
        public uint MaxPersonSongNum { get => _maxPersonSongNum; set => SetField(ref _maxPersonSongNum, value, nameof(MaxPersonSongNum)); }
        private uint _maxPersonSongNum;

        internal DanmuHandler(ObservableCollection<SongItem> songs, Player player, Downloader downloader, SearchModules searchModules, ObservableCollection<BlackListItem> blacklist, UIFunction uIFunction, EventSafeQueue<object> msgQueue, ObservableCollection<SongInfo> playlist, TTSPlugin ttsPlugin)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            Songs = songs;
            Player = player;
            Downloader = downloader;
            SearchModules = searchModules;
            Blacklist = blacklist;
            UIFunction = uIFunction;
            MsgQueue = msgQueue;
            Playlist = playlist;
            TTSPlugin = ttsPlugin;
        }



        private void Speaking(string text)
        {
            TTSPlugin.Speaking(text);
        }

        public void ProcessCmdResult(string msg)
        {
            Log(msg);
            MsgQueue.Enqueue(msg);
            Speaking(msg);
        }

        /// <summary>
        /// 处理弹幕消息
        /// <para>
        /// 注：调用侧可能会在任意线程
        /// </para>
        /// </summary>
        /// <param name="danmakuModel"></param>
        internal void ProcessDanmu(DanmakuModel danmakuModel)
        {
            if (danmakuModel.MsgType != MsgTypeEnum.Comment || string.IsNullOrWhiteSpace(danmakuModel.CommentText))
                return;
            string[] commands = danmakuModel.CommentText.Split(SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries);
            string rest = string.Join(" ", commands.Skip(1));
            if (danmakuModel.isAdmin)
            {
                if (commands[0] == "开启执行管理员命令" || commands[0] == "開啓執行管理員命令" || commands[0] == "開啟執行管理員命令")
                {
                    AdminCmdEnable = true;
                    ProcessCmdResult("执行管理员命令：开启");
                }
                else if (commands[0] == "关闭执行管理员命令" || commands[0] == "關閉執行管理員命令")
                {
                    AdminCmdEnable = false;
                    ProcessCmdResult("执行管理员命令：关闭");
                }
                if (AdminCmdEnable == true)
                {
                    switch (commands[0]?.Replace(".", ""))//防止敏感词
                    {
                        case "切歌":
                            {
                                // Player.Next();
                                dispatcher.Invoke(() =>
                                {
                                    if (Songs.Count > 0)
                                    {
                                        Songs[0].Remove(Songs, Downloader, Player);
                                        ProcessCmdResult("切歌成功！");
                                    }
                                });
                                /*
                                if (commands.Length >= 2)
                                {
                                    // TODO: 切指定序号的歌曲
                                }
                                */
                            }
                            return;
                        case "暂停":
                        case "暫停":
                            {
                                Player.Pause();
                                ProcessCmdResult("已暂停播放");
                            }
                            return;
                        case "播放":
                            {
                                Player.Play();
                                ProcessCmdResult("开始播放");
                            }
                            return;
                        case "当前音量":
                        case "當前音量":
                            {
                                ProcessCmdResult("当前音量" + Convert.ToInt32(Player.PlayerConfig.Volume * 100));
                            }
                            return;
                        case "TTS音量":
                            {
                                int volume100 = Convert.ToInt32(TTSPlugin.PlayerConfig.Volume * 100f);
                                int v = 0;
                                if (commands.Length > 1 && int.TryParse(commands[1], out v))
                                {
                                    if (commands[1][0] == '+' || commands[1][0] == '-')
                                    {
                                        //在原有基础上加减音量
                                        if (int.TryParse(commands[1], out v))
                                        {
                                            volume100 += v;
                                            if (volume100 > 100)
                                            {
                                                volume100 = 100;
                                            }
                                            else if (volume100 < 0)
                                            {
                                                volume100 = 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //直接给定音量
                                        volume100 = v;
                                    }
                                }
                                if (volume100 >= 0 && volume100 <= 100)
                                {
                                    TTSPlugin.PlayerConfig.Volume = volume100 / 100f;
                                }
                                ProcessCmdResult("当前TTS音量" + Convert.ToInt32(TTSPlugin.PlayerConfig.Volume * 100));
                            }
                            return;
                        case "音量":
                            {
                                int volume100 = Convert.ToInt32(Player.PlayerConfig.Volume * 100f);
                                int v = 0;
                                if (commands.Length > 1 && int.TryParse(commands[1], out v))
                                {
                                    if (commands[1][0] == '+' || commands[1][0] == '-')
                                    {
                                        //在原有基础上加减音量
                                        if (int.TryParse(commands[1], out v))
                                        {
                                            volume100 += v;
                                            if (volume100 > 100)
                                            {
                                                volume100 = 100;
                                            }
                                            else if (volume100 < 0)
                                            {
                                                volume100 = 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //直接给定音量
                                        volume100 = v;
                                    }
                                }
                                if (volume100 >= 0 && volume100 <= 100)
                                {
                                    Player.PlayerConfig.Volume = volume100 / 100f;
                                    ProcessCmdResult("当前音量" + Convert.ToInt32(Player.PlayerConfig.Volume * 100));
                                }
                            }
                            return;
                        case "加歌":
                            {
                                if (commands.Length > 1)
                                {
                                    dispatcher.Invoke(() =>
                                    {
                                        if (UIFunction.AddSongsToPlaylist(rest, danmakuModel.UserName))
                                        {
                                            ProcessCmdResult("房管添加歌曲:" + commands[1] + ",到空闲歌单成功");
                                        }
                                    });
                                }
                            }
                            return;
                        case "加歌单":
                            {
                                if (commands.Length > 1)
                                {
                                    dispatcher.Invoke(() =>
                                    {
                                        if (UIFunction.AddPlaylist(rest, danmakuModel.UserName))
                                        {
                                            ProcessCmdResult("房管添加歌单:" + commands[1] + ",成功");
                                        }
                                    });
                                }
                            }
                            return;
                        case "拉黑歌曲":
                            {
                                if (commands.Length > 1 && int.TryParse(commands[1], out int num))
                                {
                                    num--;
                                    if (num > -1 && num < Songs?.Count)
                                    {
                                        string songName = Songs[num]?.SongName;
                                        dispatcher.Invoke(() =>
                                        {
                                            Blacklist.Add(new BlackListItem(BlackListType.Id, Songs?[num]?.SongId));
                                            Songs?[num]?.Remove(Songs, Downloader, Player);
                                            ProcessCmdResult(songName + ",加入黑名单成功");
                                        });
                                    }
                                }
                            }
                            return;
                        case "歌单移除":
                        case "歌單移除":
                            {
                                if (commands.Length > 1)
                                {
                                    string k1 = commands[1];
                                    string k2 = "";
                                    if (commands.Length > 2)
                                        k2 = commands[2];
                                    if (int.TryParse(k1, out int index))
                                    {
                                        int len = 1;
                                        if (!int.TryParse(k2, out len))
                                        {
                                            len = 1;
                                        }
                                        dispatcher.Invoke(() =>
                                        {
                                            UIFunction.PlaylistRemove(index, len);
                                            ProcessCmdResult("移除歌单曲目" + index + "-" + (len - 1));
                                        });
                                    }
                                    else
                                    {
                                        dispatcher.Invoke(() =>
                                        {
                                            UIFunction.PlaylistRemove(k1, k2);
                                            ProcessCmdResult("移除歌单曲目" + k1 + " " + k2);
                                        });
                                    }
                                }
                            }
                            return;
                        case "清空歌单":
                        case "清空歌單":
                            {
                                Playlist.Clear();
                                ProcessCmdResult($"【{danmakuModel.UserName}】清空歌单");
                            }
                            return;
                        case "清空我的歌单":
                        case "清空我的歌單":
                            {
                                var removes = Playlist.Where(x => x.User == danmakuModel.UserName).ToList();
                                if (removes != null)
                                {
                                    foreach (var item in removes)
                                    {
                                        Playlist.Remove(item);
                                    }
                                }
                                ProcessCmdResult($"【{danmakuModel.UserName}】清空自己的歌单");
                            }
                            return;
                        case "播放模式":
                            {
                                if (commands.Length > 1)
                                {
                                    switch (commands[1])
                                    {
                                        case "列表循环":
                                        case "列表循環":
                                            {
                                                dispatcher.Invoke(() =>
                                                {
                                                    Player.SetPlayMode(PlayMode.LooptListPlay);
                                                    ProcessCmdResult("切換：列表循环");
                                                });
                                            }
                                            return;
                                        case "单曲循环":
                                        case "單曲循環":
                                            {
                                                dispatcher.Invoke(() =>
                                                {
                                                    Player.SetPlayMode(PlayMode.LoopOnetPlay);
                                                    ProcessCmdResult("切換：单曲循环");
                                                });
                                            }
                                            return;
                                        case "随机播放":
                                        case "隨機播放":
                                            {
                                                dispatcher.Invoke(() =>
                                                {
                                                    Player.SetPlayMode(PlayMode.ShufflePlay);
                                                    ProcessCmdResult("切換：随机播放");
                                                });
                                            }
                                            return;
                                        default:
                                            break;
                                    }
                                }
                            }
                            return;
                        case "静音":
                        case "靜音":
                            {
                                if (commands.Length > 1)
                                {
                                    if (commands[1] == "開啓" || commands[1] == "開啟" || commands[1] == "开启")
                                    {
                                        Player.PlayerConfig.IsMute = true;
                                        ProcessCmdResult("静音开启");
                                    }
                                    else if (commands[1] == "關閉" || commands[1] == "关闭")
                                    {
                                        Player.PlayerConfig.IsMute = false;
                                        ProcessCmdResult("静音关闭");
                                    }
                                }
                            }
                            return;
                        case "TTS静音":
                        case "TTS靜音":
                            {
                                if (commands.Length > 1)
                                {
                                    if (commands[1] == "開啓" || commands[1] == "開啟" || commands[1] == "开启")
                                    {
                                        ProcessCmdResult("TTS静音开启");
                                        TTSPlugin.PlayerConfig.IsMute = true;
                                    }
                                    else if (commands[1] == "關閉" || commands[1] == "关闭")
                                    {
                                        TTSPlugin.PlayerConfig.IsMute = false;
                                        ProcessCmdResult("TTS静音关闭");
                                    }
                                }
                            }
                            return;
                        case "最大点歌数":
                        case "最大點歌數":
                            {
                                if (commands.Length > 1 && uint.TryParse(commands[1], out uint num))
                                {
                                    MaxTotalSongNum = num;
                                    ProcessCmdResult("设置最大点歌数:" + num);
                                }
                            }
                            return;
                        case "单人点歌数":
                        case "單人點歌數":
                            {
                                if (commands.Length > 1 && uint.TryParse(commands[1], out uint num))
                                {
                                    MaxPersonSongNum = num;
                                    ProcessCmdResult("设置单人点歌数:" + num);
                                }
                            }
                            return;
                        case "用户点歌优先":
                        case "用戶點歌優先":
                            {
                                if (commands.Length > 1)
                                {
                                    if (commands[1] == "開啓" || commands[1] == "開啟" || commands[1] == "开启")
                                    {
                                        Player.IsUserPrior = true;
                                        ProcessCmdResult("用户点歌优先开启");
                                    }
                                    else if (commands[1] == "關閉" || commands[1] == "关闭")
                                    {
                                        Player.IsUserPrior = false;
                                        ProcessCmdResult("用户点歌优先关闭");
                                    }
                                }
                            }
                            return;
                        case "播放空闲歌单":
                        case "播放空閑歌單":
                            {
                                if (commands.Length > 1)
                                {
                                    if (commands[1] == "開啓" || commands[1] == "開啟" || commands[1] == "开启")
                                    {
                                        Player.IsPlaylistEnabled = true;
                                        ProcessCmdResult("静音开启");
                                    }
                                    else if (commands[1] == "關閉" || commands[1] == "关闭")
                                    {
                                        Player.IsPlaylistEnabled = false;
                                        ProcessCmdResult("静音关闭");
                                    }
                                }
                            }
                            return;
                        case "切歌票数":
                        case "切歌票數":
                            {
                                if (commands.Length > 1)
                                {
                                    if (int.TryParse(commands[1], out int votes))
                                    {
                                        Player.SkipSongVote = votes;
                                        ProcessCmdResult($"切歌票数{votes}");
                                    }
                                }
                            }
                            return;
                        case "开启TTS":
                        case "開啓TTS":
                        case "開啟TTS":
                            {
                                TTSPlugin.TTSPluginEnbale = true;
                                ProcessCmdResult($"TTS已开启");
                            }
                            return;
                        case "关闭TTS":
                        case "關閉TTS":
                            {
                                ProcessCmdResult($"TTS已关闭");
                                TTSPlugin.TTSPluginEnbale = false;
                            }
                            return;
                        case "切換内置TTS":
                        case "切换内置TTS":
                        case "切換內置TTS":
                            {
                                TTSPlugin.TtsType = TTSPluginType.InternalTTS;
                                ProcessCmdResult($"已切换为内置TTS");
                            }
                            return;
                        case "切換插件TTS":
                        case "切换插件TTS":
                            {
                                TTSPlugin.TtsType = TTSPluginType.PluginTTS;
                                if (commands.Length > 1)
                                {
                                    var tts = TTSPlugin.TTSlist?.FirstOrDefault(x => x.ModuleName.IndexOf(commands[1]) > -1);
                                    if (tts == null)
                                    {
                                        ProcessCmdResult($"没有找到TTS插件");
                                        return;
                                    }
                                }
                                else
                                {
                                    if (TTSPlugin.CurrentTTS == null)
                                    {
                                        TTSPlugin.CurrentTTS = TTSPlugin.TTSlist?.FirstOrDefault();
                                    }
                                }
                                var ttsName = TTSPlugin.CurrentTTS?.ModuleName;
                                ProcessCmdResult($"TTS切换到{ttsName}");
                            }
                            return;
                        //case "弹幕长度":
                        //case "彈幕長度":
                        //    {
                        //        if (commands.Length > 1 && uint.TryParse(commands[1], out uint num))
                        //        {
                        //        }
                        //    }
                        //    return;
                        default:
                            break;
                    }
                }
            }
            switch (commands[0]?.Replace(".", ""))//防止敏感词
            {
                case "点歌":
                case "點歌":
                    {
                        DanmuAddSong(danmakuModel, rest);
                    }
                    return;
                case "取消點歌":
                case "取消点歌":
                    {
                        dispatcher.Invoke(() =>
                        {
                            SongItem songItem = null;
                            if (commands.Length > 1)
                            {
                                string songName = commands[1];
                                songItem = Songs.LastOrDefault(x => x.UserName == danmakuModel.UserName && x.Status != SongStatus.Playing && x.SongName.IndexOf(songName) > -1);
                            }
                            else
                            {
                                songItem = Songs.LastOrDefault(x => x.UserName == danmakuModel.UserName && x.Status != SongStatus.Playing);
                            }
                            if (songItem != null)
                            {
                                songItem.Remove(Songs, Downloader, Player);
                                ProcessCmdResult("取消点歌");
                            }
                        });
                    }
                    return;
                case "切歌":
                    {
                        // Player.Next();
                        dispatcher.Invoke(() =>
                        {
                            if (Songs.Count > 0)
                            {
                                if (Songs[0].UserName == danmakuModel.UserName)
                                {
                                    Songs[0].Remove(Songs, Downloader, Player);
                                    ProcessCmdResult($"【{danmakuModel.UserName}】切歌成功！");
                                }
                            }
                        });
                        /*
                        if (commands.Length >= 2)
                        {
                            // TODO: 切指定序号的歌曲
                        }
                        */
                    }
                    return;
                case "投票切歌":
                    {
                        // TODO: 投票切歌
                        if (!Player.SkipSongVoteUsers.Contains(danmakuModel.UserName))
                        {
                            Player.SkipSongVoteUsers.Add(danmakuModel.UserName);
                            if (Player.SkipSongVoteUsers.Count == 1)
                            {
                                ProcessCmdResult($"【{danmakuModel.UserName}】发起切歌投票");
                            }
                            else if (Player.SkipSongVoteUsers.Count < Player.SkipSongVote)
                            {
                                ProcessCmdResult($"【{danmakuModel.UserName}】投票切歌，共{Player.SkipSongVoteUsers.Count}人");
                            }
                        }
                        if (Player.SkipSongVoteUsers.Count >= Player.SkipSongVote)
                        {
                            Songs[0].Remove(Songs, Downloader, Player);
                            ProcessCmdResult($"【{danmakuModel.UserName}】投票，切歌成功！");
                        }
                    }
                    return;
                default:
                    break;
            }
        }

        private void DanmuAddSong(DanmakuModel danmakuModel, string keyword)
        {
            if (dispatcher.Invoke(callback: () => CanAddSong(username: danmakuModel.UserName)))
            {
                SongInfo songInfo = null;

                if (SearchModules.PrimaryModule != SearchModules.NullModule)
                    songInfo = SearchModules.PrimaryModule.SafeSearch(keyword);

                if (songInfo == null)
                    if (SearchModules.SecondaryModule != SearchModules.NullModule)
                        songInfo = SearchModules.SecondaryModule.SafeSearch(keyword);

                if (songInfo == null)
                    return;

                if (songInfo.IsInBlacklist(Blacklist))
                {

                    ProcessCmdResult($"歌曲{songInfo.Name}在黑名单中");

                    return;
                }

                ProcessCmdResult($"【{danmakuModel.UserName}】点歌成功:{songInfo.Name}");

                dispatcher.Invoke(callback: () =>
                {
                    if (CanAddSong(danmakuModel.UserName) &&
                        !Songs.Any(x =>
                            x.SongId == songInfo.Id &&
                            x.Module.UniqueId == songInfo.Module.UniqueId)
                    )
                    {
                        int inserIndex = 0;
                        var last = Songs.LastOrDefault(p => p.UserName == Utilities.SparePlaylistUser);
                        if (last != null)
                        {
                            inserIndex = Songs.IndexOf(last);
                        }
                        else
                        {
                            inserIndex = Songs.Count;
                        }
                        Songs.Insert(inserIndex, new SongItem(songInfo, danmakuModel.UserName));
                    }
                });
            }
        }

        /// <summary>
        /// 能否点歌
        /// <para>
        /// 注：调用侧需要在主线程上运行
        /// </para>
        /// </summary>
        /// <param name="username">点歌用户名</param>
        /// <returns></returns>
        private bool CanAddSong(string username)
        {
            return Songs.Count < MaxTotalSongNum ? (Songs.Where(x => x.UserName == username).Count() < MaxPersonSongNum) : false;
        }

        private readonly static char[] SPLIT_CHAR = { ' ' };

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });
    }
}
