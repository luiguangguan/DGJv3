﻿using BilibiliDM_PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// 最多点歌数量
        /// </summary>
        public uint MaxTotalSongNum { get => _maxTotalSongCount; set => SetField(ref _maxTotalSongCount, value); }
        private uint _maxTotalSongCount;

        /// <summary>
        /// 每个人最多点歌数量
        /// </summary>
        public uint MaxPersonSongNum { get => _maxPersonSongNum; set => SetField(ref _maxPersonSongNum, value); }
        private uint _maxPersonSongNum;

        internal DanmuHandler(ObservableCollection<SongItem> songs, Player player, Downloader downloader, SearchModules searchModules, ObservableCollection<BlackListItem> blacklist, UIFunction uIFunction)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            Songs = songs;
            Player = player;
            Downloader = downloader;
            SearchModules = searchModules;
            Blacklist = blacklist;
            UIFunction = uIFunction;
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
                switch (commands[0])
                {
                    case "切歌":
                        {
                            // Player.Next();

                            dispatcher.Invoke(() =>
                            {
                                if (Songs.Count > 0)
                                {
                                    Songs[0].Remove(Songs, Downloader, Player);
                                    Log("切歌成功！");
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
                        }
                        return;
                    case "播放":
                        {
                            Player.Play();
                        }
                        return;
                    case "音量":
                        {
                            int volume100 = Convert.ToInt32(Player.Volume * 100f);
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
                                Player.Volume = volume100 / 100f;
                            }
                        }
                        return;
                    case "加歌":
                        {
                            if(commands.Length>1)
                            {
                                dispatcher.Invoke(() => {
                                    if (UIFunction.AddSongsToPlaylist(rest))
                                    {
                                        Log("房管添加歌曲:" + commands[1] + ",到空闲歌单成功");
                                    }
                                });
                            }
                        }
                        return;
                    case "加歌单":
                        {
                            if (commands.Length > 1)
                            {
                                dispatcher.Invoke(() => {
                                    if (UIFunction.AddPlaylist(rest))
                                    {
                                        Log("房管添加歌单:" + commands[1] + ",成功");
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
                                    dispatcher.Invoke(() => {
                                        Blacklist.Add(new BlackListItem(BlackListType.Id, Songs?[num]?.SongId));
                                        Songs?[num]?.Remove(Songs, Downloader, Player);
                                        Log(songName+",加入黑名单成功");
                                    });
                                }
                            }
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
                                            Player.SetPlayMode(PlayMode.LooptListPlay);
                                            Log("切換播放模式：列表循环，成功");
                                        }
                                        return;
                                    case "单曲循环":
                                    case "單曲循環":
                                        {
                                            Player.SetPlayMode(PlayMode.LoopOnetPlay);
                                            Log("切換播放模式：单曲循环，成功");
                                        }
                                        return;
                                    case "随机播放":
                                    case "隨機播放":
                                        {
                                            Player.SetPlayMode(PlayMode.ShufflePlay);
                                            Log("切換播放模式：随机播放，成功");
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
                                if (commands[1] == "開啓" || commands[1] == "开启")
                                {
                                    Player.IsMute = true;
                                    Log("静音开启", null);
                                }
                                else if (commands[1] == "關閉" || commands[1] == "关闭")
                                {
                                    Player.IsMute = false;
                                    Log("静音关闭", null);
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
                                Log("最大点歌数:" + num, null);
                            }
                        }
                        return;
                    case "单人点歌数":
                    case "單人點歌數":
                        {
                            if (commands.Length > 1 && uint.TryParse(commands[1], out uint num))
                            {
                                MaxPersonSongNum = num;
                                Log("单人点歌数:" + num, null);
                            }
                        }
                        return;
                    case "用户点歌优先":
                    case "用戶點歌優先":
                        {
                            if (commands.Length > 1)
                            {
                                if (commands[1] == "開啓" || commands[1] == "开启")
                                {
                                    Player.IsUserPrior = true;
                                    Log("用户点歌优先开启", null);
                                }
                                else if (commands[1] == "關閉" || commands[1] == "关闭")
                                {
                                    Player.IsUserPrior = false;
                                    Log("用户点歌优先关闭", null);
                                }
                            }
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

            switch (commands[0])
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
                            SongItem songItem = Songs.LastOrDefault(x => x.UserName == danmakuModel.UserName && x.Status != SongStatus.Playing);
                            if (songItem != null)
                            {
                                songItem.Remove(Songs, Downloader, Player);
                            }
                        });
                    }
                    return;
                case "投票切歌":
                    {
                        // TODO: 投票切歌
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
                    Log($"歌曲{songInfo.Name}在黑名单中");
                    return;
                }
                Log($"点歌成功:{songInfo.Name}");
                dispatcher.Invoke(callback: () =>
                {
                    if (CanAddSong(danmakuModel.UserName) &&
                        !Songs.Any(x =>
                            x.SongId == songInfo.Id &&
                            x.Module.UniqueId == songInfo.Module.UniqueId)
                    )
                        Songs.Add(new SongItem(songInfo, danmakuModel.UserName));
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
