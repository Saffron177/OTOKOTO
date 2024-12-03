using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
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
using System.IO;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace HottoMotto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveInEvent waveIn;
        private VoskRecognizer recognizer;
        private VoskRecognizer mic_recognizer;

        private Model model;
        private MemoryStream audioStream;
        private FileStream fileStream;
        
        //WASAPIループバック録音用のオブジェクト
        private WasapiLoopbackCapture capture;
        //録音データをWAV形式で保存するためのオブジェクト
        private WaveFileWriter writer;
        //WASAPIマイク録音用のオブジェクト
        private WasapiCapture mic_capture;

        //ミュートボタン用のフラグ
        private bool is_mute = false;
        public MainWindow()
        {
            InitializeComponent();
            LoadAudioDevices();
            LoadMicDevices();
            Debug.Print("init");
            
            btnStop.IsEnabled = false;

            // モデルをロード（解凍したモデルのパスを指定）
            string modelPath = "../../../../Models/vosk-model-small-ja-0.22";
            Console.WriteLine("モデルパス: " + modelPath);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
            mic_recognizer = new VoskRecognizer(model, 16000.0f);
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
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recorded.wav");
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
                txtOutput.ScrollToEnd();
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
        

        //オーディオデバイスを取得する関数
        private void LoadAudioDevices()
        {
            ComboBox_AudioDevices.Items.Clear();
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                ComboBox_AudioDevices.Items.Add(device.FriendlyName);
            }

            if (ComboBox_AudioDevices.Items.Count > 0)
            {
                ComboBox_AudioDevices.SelectedIndex = 0; // 最初のデバイスを選択
            }
        }
        //マイクデバイスを取得する関数
        private void LoadMicDevices()
        {
            ComboBox_MicDevices.Items.Clear();
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture,DeviceState.Active);

            foreach (var device in devices)
            {
                ComboBox_MicDevices.Items.Add(device.FriendlyName);
            }

            if(ComboBox_MicDevices.Items.Count > 0) 
            { 
                ComboBox_MicDevices.SelectedIndex = 0;
            }
        }


        private void Button_capture_start_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_AudioDevices.SelectedIndex == -1)
            {
                MessageBox.Show("再生デバイスを選択してください");
                return;
            }
            if (ComboBox_MicDevices.SelectedIndex == -1)
            {
                MessageBox.Show("録音デバイスを選択してください");
                return;
            }
            // 選択されたデバイスを取得
            //再生デバイス
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var selectedDevice = devices[ComboBox_AudioDevices.SelectedIndex];
            Debug.Print("再生デバイスID：" + selectedDevice.ID);
            //録音デバイス
            var mic_devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            var mic_selectedDevice = mic_devices[ComboBox_MicDevices.SelectedIndex];
            Debug.Print("録音デバイスID：" + mic_selectedDevice.ID);

            try
            {
                //出力先パス
                string outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "recorded_audio.wav");
                //オブジェクト生成
                capture = new WasapiLoopbackCapture(selectedDevice);
                writer = new WaveFileWriter(outputPath, capture.WaveFormat);

                // リサンプル用フォーマット（16kHz, モノラル）
                var targetFormat = new WaveFormat(16000, 1);


                //capture.DataAvailable: 録音データが利用可能になるたびに発生するイベント
                //a.Buffer: 録音データが格納されたバッファ。
                //writer.Write: 録音データをWAVファイルに書き込む。
                capture.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);

                    //Debug.Print("DataAvailable");

                    //リサンプルをするためにBufferをIWaveProvider型に変換
                    using (var inputStream = new RawSourceWaveStream(a.Buffer, 0, a.BytesRecorded, capture.WaveFormat))
                    //リサンプル処理
                    using (var resampler = new MediaFoundationResampler(inputStream, targetFormat))
                    {
                        //resampler.ResamplerQuality = 60; // リサンプル品質を設定

                        // リサンプルされたデータを格納するバッファ
                        byte[] resampledBuffer = new byte[4096];
                        int bytesResampled;

                        //リサンプルデータが存在する限りループ
                        while ((bytesResampled = resampler.Read(resampledBuffer, 0, resampledBuffer.Length)) > 0)
                        {
                            //Debug.Print("while");
                            // 音声データを認識器に送信
                            if (recognizer.AcceptWaveform(resampledBuffer, bytesResampled))
                            {
                                var result = recognizer.Result();
                                Debug.Print(result);
                                UpdateTextBox(result);
                            }
                            else
                            {
                                var partialResult = recognizer.PartialResult();
                                if (!IsEmptyPartialResult(partialResult))
                                {
                                    //UpdateTextBox(partialResult);
                                }
                            }
                        }
                    }
                };

                /*RecordingStopped: 録音が停止したときに発生するイベント。
                  writer.Dispose(): ファイルを書き込み終了し、リソースを解放。
                  capture.Dispose(): 録音用オブジェクトを解放。
                */
                capture.RecordingStopped += (s, a) =>
                {
                    writer?.Dispose();
                    writer = null;
                    capture.Dispose();
                    mic_capture.Dispose();
                    Debug.Print("Stop");
                    // 最終結果を取得
                    var finalResult = recognizer.FinalResult();
                    UpdateTextBox(finalResult);
                };

                //マイク録音の処理
                mic_capture = new WasapiCapture(mic_selectedDevice)
                {
                    WaveFormat = targetFormat,
                };

                
                mic_capture.DataAvailable += (s, a) =>
                {
                    // 音声データを認識器に送信
                    if (mic_recognizer.AcceptWaveform(a.Buffer, a.BytesRecorded))
                    {
                        var result = mic_recognizer.Result();
                        Debug.Print(result);
                        UpdateTextBox(result);
                    }
                    else
                    {
                        var partialResult = mic_recognizer.PartialResult();
                        if (!IsEmptyPartialResult(partialResult))
                        {
                            //UpdateTextBox(partialResult);
                        }
                    }
                };

                mic_capture.RecordingStopped += (s, a) =>
                {
                    Debug.Print("mic_Stop");
                    // 最終結果を取得
                    var finalResult = recognizer.FinalResult();
                    UpdateTextBox(finalResult);
                    Button_Mute.IsEnabled = true;

                };


                //録音を開始
                capture.StartRecording();
                Label_status.Content = "録音中...";
                if (!is_mute)
                {
                    mic_capture.StartRecording();
                }
               
            }
            catch (Exception ex)
            {
                Debug.Print($"エラー: {ex.Message}");
            }
        }
        private void Button_capture_stop_Click(object sender, RoutedEventArgs e)
        {
            if (capture != null)
            {
                //録音を停止
                capture.StopRecording();
                Label_status.Content = "録音停止";
                mic_capture.StopRecording();
            }
        }

        private void Button_Mute_Click(object sender, RoutedEventArgs e)
        {
            if (is_mute)
            {
                //ミュートをオフ
                Button_Mute.Content = "ミュートOFF";
                is_mute = false;
                if(mic_capture != null)
                {
                    mic_capture.StartRecording();
                }
            }
            else
            {
                //ミュートをオン
                Button_Mute.Content = "ミュートON";
                is_mute=true;
                if (mic_capture != null)
                {
                    Button_Mute.IsEnabled = false;
                    mic_capture.StopRecording();
                }
            }
        }
    }
}