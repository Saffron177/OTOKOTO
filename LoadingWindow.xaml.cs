using System.Windows;
using System.Windows.Threading;

namespace HottoMotto
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgress(string message)
        {
            if (Dispatcher.CheckAccess()) // UIスレッドの場合
            {
                ProgressText.Text = message;
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    ProgressText.Text = message;
                }));
            }
        }

        public void BringToFront()
        {
            if (Dispatcher.CheckAccess())
            {
                Activate();
                Topmost = true;
                Topmost = false;
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    Activate();
                    Topmost = true;
                    Topmost = false;
                }));
            }
        }
    }
}