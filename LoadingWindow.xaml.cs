using System.Windows;

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
            // UIスレッドで実行するために必要
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = message;
            });
        }
    }
}