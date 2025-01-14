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

        // タイマーのTickイベントハンドラ
        private void Timer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - startTime;
            TimeElapsedText.Text = $"経過時間: {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        // 進捗状況の更新
        public void UpdateProgress(string message, int? progressPercentage = null)
        {
            if (Dispatcher.CheckAccess()) // UIスレッドの場合
            {
                ProgressText.Text = message;
                if (progressPercentage.HasValue)
                {
                    DownloadProgressBar.Value = progressPercentage.Value;
                    ProgressPercentText.Text = $"{progressPercentage.Value}%";
                }
            }
            else // 別スレッドからの呼び出しの場合
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateProgress(message, progressPercentage)));
            }
        }

        // ウィンドウが閉じられる時の処理
        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            base.OnClosed(e);
        }

        // ウィンドウを前面に表示
        public void BringToFront()
        {
            if (Dispatcher.CheckAccess()) // UIスレッドの場合
            {
                Activate();
                Topmost = true;
                Topmost = false;
            }
            else // 別スレッドからの呼び出しの場合
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