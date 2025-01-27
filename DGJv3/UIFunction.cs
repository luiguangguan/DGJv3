﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3
{
    internal class UIFunction : INotifyPropertyChanged
    {
        //public bool TemplatePopOpened
        //{
        //    get => _templatePopOpened; set
        //    {
        //        _templatePopOpened = value;
        //        if (value == false)
        //        {
        //          //  ListViewOutputInfoSelectIndex = -1;
        //        }
        //    }

        //}
        //private bool _templatePopOpened = false;

        //public int ListViewOutputInfoSelectIndex
        //{
        //    get => _listViewOutputInfoSelectIndex; set
        //    {
        //        _listViewOutputInfoSelectIndex = value;
        //            SetField(ref _listViewOutputInfoSelectIndex, value, nameof(ListViewOutputInfoSelectIndex));
        //    }
        //}
        private int _listViewOutputInfoSelectIndex = -1;

        private ObservableCollection<SongItem> Songs { get; set; }

        private ObservableCollection<SongInfo> Playlist { get; set; }

        private ObservableCollection<BlackListItem> Blacklist { get; set; }

        private ObservableCollection<SongItem> SkipSong;
        private SearchModules SearchModules { get; set; }
        private ObservableCollection<OutputInfoTemplate> InfoTemplates { get; set; }
        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });


        public UIFunction(ObservableCollection<SongItem> songs, ObservableCollection<SongInfo> playlist, ObservableCollection<BlackListItem> blacklist, ObservableCollection<SongItem> skipSong, SearchModules searchModules, ObservableCollection<OutputInfoTemplate> infoTemplates)
        {
            Songs = songs;
            Playlist = playlist;
            Blacklist = blacklist;
            SkipSong = skipSong;
            SearchModules = searchModules;
            InfoTemplates = infoTemplates;
        }

        /// <summary>
        /// 添加歌曲到空闲歌单
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public bool AddSongsToPlaylist(string keyword, string user)
        {
            try
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    SongInfo songInfo = null;

                    if (SearchModules.PrimaryModule != SearchModules.NullModule)
                    {
                        songInfo = SearchModules.PrimaryModule.SafeSearch(keyword);
                    }

                    if (songInfo == null)
                    {
                        if (SearchModules.SecondaryModule != SearchModules.NullModule)
                        {
                            songInfo = SearchModules.SecondaryModule.SafeSearch(keyword);
                        }
                    }

                    if (songInfo != null)
                    {
                        songInfo.User = user;
                        Playlist.Add(songInfo);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("添加歌曲到空闲歌单", ex);
            }
            return false;
        }
        /// <summary>
        /// 添加歌单
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>

        public bool AddPlaylist(string keyword, string user)
        {
            try
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    List<SongInfo> songInfoList = null;

                    if (SearchModules.PrimaryModule != SearchModules.NullModule && SearchModules.PrimaryModule.IsPlaylistSupported)
                    {
                        songInfoList = SearchModules.PrimaryModule.SafeGetPlaylist(keyword);
                    }

                    // 歌单只使用主搜索模块搜索

                    if (songInfoList != null)
                    {
                        foreach (var item in songInfoList)
                        {
                            if (Playlist.Any(p => p.Id == item.Id && item.Name == item.Name && p.ModuleId == item.ModuleId) == false)
                            {
                                item.User = user;
                                Playlist.Add(item);
                            }
                        }
                        return true;
                    }


                }
            }
            catch (Exception ex)
            {
                Log("添加空闲歌单", ex);
            }
            return false;
        }

        /// <summary>
        /// 取消所有输出模板文件名的"编辑状态"
        /// </summary>
        public void CancelAllTemplateFileNameEditong()
        {
            if (InfoTemplates != null)
            {
                foreach (var it in InfoTemplates)
                {
                    if (it.Editing)
                        it.Editing = false;
                }
            }
        }

        public bool PlaylistRemove(string keyword1, string keyword2)
        {
            var removeList = Playlist.Where(p =>
           (p.Name.IndexOf(keyword1) > -1 && (p.SingersText.IndexOf(keyword2) > -1 || string.IsNullOrEmpty(keyword2)))
           ||
           ((string.IsNullOrEmpty(keyword2) || p.Name.IndexOf(keyword2) > -1) && p.SingersText.IndexOf(keyword1) > -1)
            ).ToList();
            if (removeList != null)
            {
                for (int i = 0; i < removeList.Count; i++)
                {
                    PlaylistRemove(removeList[i]);

                }
            }
            return true;
        }
        public bool PlaylistRemoveById(string songid, string moduleid)
        {
            var removeList = Playlist.Where(p => p.Id == songid && p.ModuleId == moduleid).ToList();
            if (removeList != null)
            {
                for (int i = 0; i < removeList.Count; i++)
                {
                    PlaylistRemove(removeList[i]);

                }
            }
            return true;
        }
        public bool PlaylistRemove(SongInfo song)
        {
            if (Playlist != null && Playlist.Count > 0)
                return Playlist.Remove(song);
            return false;
        }
        public bool PlaylistRemove(int startIndex, int len)
        {
            startIndex--;
            if (startIndex > -1 && startIndex < Playlist.Count)
            {
                for (int i = startIndex + len - 1; i >= startIndex && i > -1; i--)
                {
                    Playlist.RemoveAt(i);
                }
            }
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
