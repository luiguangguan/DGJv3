using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Media;
using NAudio.Wave;

namespace DGJv3
{
    internal class PlayerConfig : INotifyPropertyChanged
    {
        public Guid DirectSoundDevice { get => _directSoundDevice; set => SetField(ref _directSoundDevice, value); }
        private Guid _directSoundDevice;

        /// <summary>
        /// WaveoutEvent 设备
        /// </summary>
        public int WaveoutEventDevice { get => _waveoutEventDevice; set => SetField(ref _waveoutEventDevice, value); }
        private int _waveoutEventDevice;

        public PlayerType PlayerType { get => _playerType; set => SetField(ref _playerType, value); }
        private PlayerType _playerType;

        public float Volume2 { get; set; }

        public bool IsMute { get => _muted; set => SetField(ref _muted, value, nameof(IsMute)); }
        private bool _muted = false;

        /// <summary>
        /// 播放器音量
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                //if (sampleChannel != null)
                //{
                //    sampleChannel.Volume = value;
                //}
                SetField(ref _volume, value, nameof(Volume));
            }
        }
        private float _volume;

        internal PlayerConfig()
        {
            PropertyChanged += This_PropertyChanged;
        }

        /// <summary>
        /// 根据当前设置初始化 IWavePlayer
        /// </summary>
        /// <returns></returns>
        public IWavePlayer CreateIWavePlayer()
        {
            switch (PlayerType)
            {
                case PlayerType.WaveOutEvent:
                    return new WaveOutEvent() { DeviceNumber = WaveoutEventDevice };
                case PlayerType.DirectSound:
                    return new DirectSoundOut(DirectSoundDevice);
                default:
                    return null;
            }
        }

        private void This_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
          if (e.PropertyName == nameof(Volume))
            {
                //音量变动事件
                float vol = this.Volume;

                //记录非点击静音时的音量
                if (this.IsMute == false)
                    this.Volume2 = vol;

                if (this.Volume > 0)
                {
                    this.IsMute = false;
                }
                else
                {
                    this.IsMute = true;
                }
            }
            else if (e.PropertyName == nameof(IsMute))
            {
                //静音变动事件
                if (this.IsMute)
                    this.Volume = 0;
                else if (this.Volume == 0)
                    this.Volume = this.Volume2;
            }
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
