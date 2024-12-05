using NAudio.CoreAudioApi;
using NAudio.Wave;
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

        private Model model;
        public MainWindow()
        {
            InitializeComponent();
            LoadAudioDevices();
            LoadMicDevices();
            // モデルをロード（解凍したモデルのパスを指定）
            string modelPath = "../../../../Models/vosk-model-small-ja-0.22";
            Console.WriteLine("モデルパス: " + modelPath);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
            mic_recognizer = new VoskRecognizer(model, 16000.0f);
        }

        private void UpdateTextBox(string text)
        {
            // Dispatcher.Invokeを使用してUIスレッドで実行
            txtOutput.Dispatcher.Invoke(() =>
            {
                txtOutput.Text += text + Environment.NewLine;
                txtOutput.ScrollToEnd();
            });

            JsonUtil jsonutil = new JsonUtil();

            jsonutil.ToJson(text, true);
        }

        // PartialResultが空かどうかを判定するヘルパーメソッド
        private bool IsEmptyPartialResult(string partialResult)
        {
            try
            {
                // JSON解析（System.Text.Jsonを使用）
                var jsonDoc = System.Text.Json.JsonDocument.Parse(partialResult);
                if (jsonDoc.RootElement.TryGetProperty("partial", out var partialProperty))
                {
                    return string.IsNullOrEmpty(partialProperty.GetString());
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                // JSON解析に失敗した場合（デバッグ用にログ出力など）
                Console.WriteLine("JSON解析エラー: " + ex.Message);
            }
            return false;
        }
    }
}