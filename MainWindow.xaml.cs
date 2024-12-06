using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using Vosk;
namespace HottoMotto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VoskRecognizer recognizer;
        private VoskRecognizer mic_recognizer;

        private List<string> json_list = new List<string>(); 

        private Model model;
        public MainWindow()
        {
            InitializeComponent();
            LoadAudioDevices();
            LoadMicDevices();
            // モデルをロード（解凍したモデルのパスを指定）
            string modelPath = "Models/vosk-model-small-ja-0.22";
            Console.WriteLine("モデルパス: " + modelPath);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
            mic_recognizer = new VoskRecognizer(model, 16000.0f);
            SetupNotifyIcon();
        }

        private void UpdateTextBox(string text, bool is_speaker)
        {

            // Dispatcher.Invokeを使用してUIスレッドで実行
            txtOutput.Dispatcher.Invoke(() =>
            {
                txtOutput.Text += text + Environment.NewLine;
                txtOutput.ScrollToEnd();
            });

            JsonUtil jsonutil = new JsonUtil();

            Debug.Print("Updatetext" + text);
            json_list.Add(jsonutil.ToJson(text, is_speaker));
        }

        private void Button_Log_Click(object sender, RoutedEventArgs e)
        {
            LogWindow logWindow = new LogWindow();
            logWindow.Show();
        }
    }
}