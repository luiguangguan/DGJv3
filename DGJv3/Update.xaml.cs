using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace DGJv3
{
    /// <summary>
    /// Update.xaml 的交互逻辑
    /// </summary>
    public partial class Update : Window
    {
        public VersionChecker VersionChecker { get; set; }
        public UniversalCommand GoToDownloadPage { get; set; }
        public UniversalCommand CloseWindow { get; set; }

        public Update(VersionChecker checker)
        {
            //ReleaseMd.Markdown
            VersionChecker = checker;
            GoToDownloadPage = new UniversalCommand((obj) =>
            {
                Process.Start(VersionChecker.UpdatePage.AbsoluteUri);
            });
            CloseWindow = new UniversalCommand((obj) =>
            {
                this.Hide();
            });
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
