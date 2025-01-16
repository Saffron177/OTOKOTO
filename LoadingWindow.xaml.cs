using System;
using System.Windows;  // WPFのWindowを使用
using System.Windows.Threading;
using System.Windows.Controls;  // WPFのコントロール
using MaterialDesignThemes.Wpf.Transitions;
using MaterialDesignThemes.Wpf;

namespace HottoMotto
{
    public partial class LoadingWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime startTime;
        private bool isPaused = false;
        public bool IsCancelled { get; private set; } = false;

        public event EventHandler<bool> PauseRequested;
        public event EventHandler CancelRequested;

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
        private void Timer_Tick(object? sender, EventArgs e)
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

        // 一時停止ボタンのクリックイベント
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
            var button = (System.Windows.Controls.Button)sender;
            var icon = (PackIcon)((StackPanel)button.Content).Children[0];
            var text = (TextBlock)((StackPanel)button.Content).Children[1];

            if (isPaused)
            {
                icon.Kind = PackIconKind.Play;
                text.Text = " 再開";
                timer.Stop(); // タイマーを停止
            }
            else
            {
                icon.Kind = PackIconKind.Pause;
                text.Text = " 一時停止";
                timer.Start(); // タイマーを再開
            }

            PauseRequested?.Invoke(this, isPaused);
        }

        // キャンセルボタンのクリックイベント
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "ダウンロードをキャンセルしますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsCancelled = true;
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        // ウィンドウが閉じられる時の処理
        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            base.OnClosed(e);
        }

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