﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DGJv3
{
    internal static class Extensions
    {
        internal static void Remove(this SongItem songItem, ObservableCollection<SongItem> songList, Downloader downloader, Player player)
        {
            switch (songItem.Status)
            {
                case SongStatus.WaitingDownload:
                    songList.Remove(songItem);
                    break;
                case SongStatus.Downloading:
                    downloader.CancelDownload();
                    break;
                case SongStatus.WaitingPlay:
                    songList.Remove(songItem);
                    try { File.Delete(songItem.FilePath); } catch (Exception) { }
                    break;
                case SongStatus.Playing:
                    player.Next();
                    break;
                default:
                    break;
            }
        }

        internal static string GetDownloadUrl(this SongItem songItem) => songItem.Module.SafeGetDownloadUrl(songItem);

        internal static bool IsInBlacklist(this SongInfo songInfo, IEnumerable<BlackListItem> blackList)
        {
            return blackList.ToArray().Any(x =>
            {
                switch (x.BlackType)
                {
                    case BlackListType.Id: return songInfo.Id.Equals(x.Content);
                    case BlackListType.Name: return songInfo.Name.IndexOf(x.Content, StringComparison.CurrentCultureIgnoreCase) > -1;
                    case BlackListType.Singer: return songInfo.SingersText.IndexOf(x.Content, StringComparison.CurrentCultureIgnoreCase) > -1;
                    default: return false;
                }
            });
        }

        internal static string ToStatusString(this SongStatus songStatus)
        {
            switch (songStatus)
            {
                case SongStatus.WaitingDownload:
                    return "等待下载";
                case SongStatus.Downloading:
                    return "正在下载";
                case SongStatus.WaitingPlay:
                    return "等待播放";
                case SongStatus.Playing:
                    return "正在播放";
                default:
                    return "？？？？";
            }
        }
        internal static string ToZhName(this PlayMode playMode)
        {
            switch (playMode)
            {
                case PlayMode.LooptListPlay:
                    return "列表循环";
                case PlayMode.LoopOnetPlay:
                    return "单曲循环";
                case PlayMode.ShufflePlay:
                    return "随机播放";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 使用正则表达式去掉Windows中不能作为文件名的字符
        /// </summary>
        /// <param name="target">this</param>
        /// <returns></returns>
        public static string RemoveIllegalCharacterNTFS(this string target)
        {
            if (!string.IsNullOrEmpty(target))
                return Regex.Replace(target, "[<>:\"/\\\\|\\?\\*]", "");
            return target;
        }

        //internal static bool Remove(string key,Dictionary<string ,OutputInfo> idc)
        //{
        //    if(idc!=null&& idc.ContainsKey(key))
        //    {

        //    }
        //}
    }
}
