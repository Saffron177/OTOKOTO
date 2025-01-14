using System.Windows;
using System.Windows.Threading;

namespace HottoMotto
{
    public partial class LoadingWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime startTime;

        public LoadingWindow()
        {
            InitializeComponent();

            // タイマーの初期化
            startTime = DateTime.Now;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - startTime;
            TimeElapsedText.Text = $"経過時間: {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        public void UpdateProgress(string message)
        {
            if (Dispatcher.CheckAccess())
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

        // ウィンドウが閉じられる時にタイマーを停止
        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            base.OnClosed(e);
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