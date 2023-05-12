using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DGJv3
{
    public enum PlayMode
    {

        /// <summary>
        /// 列表循环
        /// </summary>
        LooptListPlay = 0,
        /// <summary>
        /// 单曲循环
        /// </summary>
        LoopOnetPlay = 1,
        /// <summary>
        /// 随机播放
        /// </summary>
        ShufflePlay = 2,

        /// <summary>
        /// 列表播放
        /// </summary>
        //ListPlay = 3,
    }

    //class PlayModeConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return (PlayMode)value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return (PlayMode)value;
    //    }
    //}
}
