using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Vosk;
using NAudio.Wave;
using System.IO;

namespace HottoMotto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveInEvent waveIn;
        private VoskRecognizer recognizer;
        private Model model;
        private MemoryStream audioStream;
        private FileStream fileStream;

        public MainWindow()
        {
            InitializeComponent();
            btnStop.IsEnabled = false;

            // モデルをロード（解凍したモデルのパスを指定）
            string modelPath = "../../../../Models/vosk-model-small-ja-0.22";
            Console.WriteLine("モデルパス: " + modelPath);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // 録音の設定
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1)
            };
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            // ストリームを初期化
            audioStream = new MemoryStream();


            // WAVファイルに保存する準備
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recorded.wav");
            fileStream = new FileStream(filePath, FileMode.Create);

            // WAVヘッダーを書き込む（後で更新するため、仮の値を設定）
            WriteWavHeader(fileStream, waveIn.WaveFormat);


            // 録音開始
            waveIn.StartRecording();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            // 録音停止
            waveIn.StopRecording();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            Console.WriteLine($"バッファサイズ: {e.BytesRecorded}バイト");
            // 音声データをファイルに書き込み
            fileStream.Write(e.Buffer, 0, e.BytesRecorded);

            // 音声データを認識器に送信
            if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                var result = recognizer.Result();
                UpdateTextBox(result);
            }
            else
            {
                var partialResult = recognizer.PartialResult();
                if (!IsEmptyPartialResult(partialResult))
                {
                    UpdateTextBox(partialResult);
                }
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            waveIn.Dispose();
            waveIn = null;

            // WAVファイルのサイズ情報を更新して閉じる
            UpdateWavHeader(fileStream);
            fileStream.Close();


            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;

            // 最終結果を取得
            var finalResult = recognizer.FinalResult();
            UpdateTextBox(finalResult);
        }

        private void UpdateTextBox(string text)
        {
            // Dispatcher.Invokeを使用してUIスレッドで実行
            txtOutput.Dispatcher.Invoke(() =>
            {
                txtOutput.Text += text + Environment.NewLine;
            });
        }

        private void WriteWavHeader(Stream stream, WaveFormat format)
        {
            // WAVファイルのヘッダーを仮の値で書き込む
            // 実際のデータサイズは録音終了時に更新する
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(0); // ファイルサイズ（仮）
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // fmtチャンクのサイズ
            writer.Write((short)1); // フォーマット（PCM）
            writer.Write((short)format.Channels); // チャンネル数
            writer.Write(format.SampleRate); // サンプリングレート
            writer.Write(format.SampleRate * format.Channels * format.BitsPerSample / 8); // バイトレート
            writer.Write((short)(format.Channels * format.BitsPerSample / 8)); // ブロックアライン
            writer.Write((short)format.BitsPerSample); // サンプルあたりのビット数
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(0); // データチャンクのサイズ（仮）
        }

        private void UpdateWavHeader(Stream stream)
        {
            // WAVファイルのヘッダーに正しいサイズ情報を更新
            long fileSize = stream.Length;
            stream.Seek(4, SeekOrigin.Begin);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)(fileSize - 8)); // ファイルサイズ - 8バイト
            stream.Seek(40, SeekOrigin.Begin);
            writer.Write((int)(fileSize - 44)); // データチャンクサイズ
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

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}