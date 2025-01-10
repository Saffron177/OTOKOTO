using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using NAudio.MediaFoundation;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Xml;
using System.Formats.Asn1;
using NAudio.Lame;
using MahApps.Metro.Controls;

namespace HottoMotto
{
    partial class MainWindow : Window
    {
        //WASAPIループバック録音用のオブジェクト
        private WasapiLoopbackCapture capture;
        //録音データをWAV形式で保存するためのオブジェクト
        private WaveFileWriter writer;
        //WASAPIマイク録音用のオブジェクト
        private WasapiCapture mic_capture;

        private string M_outputPath;
        private WaveFileWriter M_writer;
        private WaveFileWriter S_writer;

        //ミュートボタン用のフラグ
        private bool is_mute = false;
        //録音開始・停止フラグ
        private bool recFlag = false;

        //マイク接続確認フラグ
        private bool is_Mic_Connected = true;
        ///<summary>
        ///オーディオデバイスを取得する関数
        ///</summary>
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

        /// <summary>
        /// マイクデバイスを取得する関数
        /// </summary>
        private void LoadMicDevices()
        {
            ComboBox_MicDevices.Items.Clear();
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            foreach (var device in devices)
            {
                ComboBox_MicDevices.Items.Add(device.FriendlyName);
            }

            if (ComboBox_MicDevices.Items.Count > 0)
            {
                ComboBox_MicDevices.SelectedIndex = 0;
            }
        }

        private void ReloadDevices(object sender, RoutedEventArgs e)
        {
            LoadAudioDevices();
            LoadMicDevices();
        }

        /// <summary>
        /// ボタンを押したときのイベント関数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCaptureStart(object sender, RoutedEventArgs e)
        {
            ComboBox_AudioDevices.IsEnabled = false;
            ComboBox_MicDevices.IsEnabled = false;
            is_Mic_Connected = true;
            Debug.Print("Button: capture_start_Click");
            if (ComboBox_AudioDevices.SelectedIndex == -1)
            {
                System.Windows.MessageBox.Show("再生デバイスを選択してください");
                return;
            }
            if (ComboBox_MicDevices.SelectedIndex == -1)
            {
                //System.Windows.MessageBox.Show("録音デバイスを選択してください");
                //return;
                is_Mic_Connected = false;
                is_mute = true;//ミュートに設定
            }
            // 選択されたデバイスを取得
            //再生デバイス
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var selectedDevice = devices[ComboBox_AudioDevices.SelectedIndex];
            Debug.Print("再生デバイスID：" + selectedDevice.ID);
            //録音デバイス--マイクが接続されているときのみ
            MMDeviceCollection? mic_devices;
            MMDevice? mic_selectedDevice;
            if (is_Mic_Connected)
            {
                mic_devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                mic_selectedDevice = mic_devices[ComboBox_MicDevices.SelectedIndex];
                Debug.Print("録音デバイスID：" + mic_selectedDevice.ID);
            }
            else
            {
                mic_devices = null;
                mic_selectedDevice = null;
                Debug.Print("録音デバイスID：接続されていません");
            }
            
            try
            {
                //出力先パス
                string outputPath = System.IO.Path.Combine("recorded_audio.wav");
                //オブジェクト生成
                capture = new WasapiLoopbackCapture(selectedDevice);
                writer = new WaveFileWriter(outputPath, capture.WaveFormat);

                string S_outputPath = System.IO.Path.Combine($"./Audio/S_{DateTime.Now:yyMMddHHmmss}.wav");
                S_writer = new WaveFileWriter(S_outputPath,capture.WaveFormat);
                // リサンプル用フォーマット（16kHz, モノラル）
                var targetFormat = new WaveFormat(16000, 1);


                //capture.DataAvailable: 録音データが利用可能になるたびに発生するイベント
                //a.Buffer: 録音データが格納されたバッファ。
                //writer.Write: 録音データをWAVファイルに書き込む。
                capture.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                    S_writer.Write(a.Buffer,0,a.BytesRecorded);

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
                                //音声ファイル保存
                                S_writer.Flush();
                                S_writer.Dispose();
                                var result = recognizer.Result();
                                UpdateTextBox(result, true, ConvertWavToMp3(S_outputPath));
                                //UpdateTextBox(result, true, S_outputPath);
                                Debug.Print("Audio" + result);

                                //次の音声ファイルの準備
                                S_outputPath = System.IO.Path.Combine($"./Audio/S_{DateTime.Now:yyMMddHHmmss}.wav");
                                S_writer = new WaveFileWriter(S_outputPath, capture.WaveFormat);
                                
                            }
                            else
                            {
                                //partialの処理
                                UpdateToPartial(recognizer.PartialResult(), true);
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
                    Debug.Print("writer:Dispose");
                    writer = null;
                    capture.Dispose();
                    Debug.Print("capture:Dispose");
                    capture = null;
                    if (is_Mic_Connected)//マイクが接続されているときのみ
                    {
                        mic_capture.Dispose();
                        Debug.Print("mic_capture:Dispose");
                        mic_capture = null;
                    }
                    Debug.Print("Stop");

                    // 最終結果を取得
                    S_writer.Flush();
                    S_writer.Dispose();
                    var finalResult = recognizer.FinalResult();
                    UpdateTextBox(finalResult, true, ConvertWavToMp3(S_outputPath));
                };

                //マイク録音の処理--マイクが接続されているときのみ
                if (is_Mic_Connected)
                {
                    mic_capture = new WasapiCapture(mic_selectedDevice)
                    {
                        WaveFormat = targetFormat,
                    };

                    M_outputPath = System.IO.Path.Combine($"./Audio/M_{DateTime.Now:yyMMddHHmmss}.wav");
                    M_writer = new WaveFileWriter(M_outputPath, mic_capture.WaveFormat);



                    mic_capture.DataAvailable += (s, a) =>
                    {
                        M_writer.Write(a.Buffer, 0,a.BytesRecorded);
                        // 音声データを認識器に送信
                        if (mic_recognizer.AcceptWaveform(a.Buffer, a.BytesRecorded))
                        {
                            M_writer.Flush();
                            M_writer.Dispose();
                            var result = mic_recognizer.Result();
                            
                            UpdateTextBox(result, false, ConvertWavToMp3(M_outputPath));
                            Debug.Print(result);
                            M_outputPath = System.IO.Path.Combine($"./Audio/M_{DateTime.Now:yyMMddHHmmss}.wav");
                            M_writer = new WaveFileWriter(M_outputPath, mic_capture.WaveFormat);
                        }
                        else
                        {
                            //partialの処理
                            UpdateToPartial(mic_recognizer.PartialResult(), false);
                        }
                    };

                    mic_capture.RecordingStopped += (s, a) =>
                    {
                        Debug.Print("mic_Stop");
                        // 最終結果を取得
                        M_writer.Flush();
                        M_writer.Dispose();
                        M_writer = null;
                        var finalResult = mic_recognizer.FinalResult();
                        UpdateTextBox(finalResult, false,ConvertWavToMp3(M_outputPath));
                        Button_Mute.IsEnabled = true;

                    };
                }

                //録音を開始
                capture.StartRecording();
                if (!is_mute)
                {
                    mic_capture.StartRecording();
                }

            }
            catch (IOException ex)
            {
                Debug.Print($"エラー: {ex.Message}");
                System.Windows.MessageBox.Show("連打するな🫵", "エラー");
                ExitApplication();
            }
            catch (Exception ex)
            {
                Debug.Print($"エラー: {ex.Message}");
                System.Windows.MessageBox.Show(ex.Message,"エラー");
                ExitApplication();
            }
        }
        private void ButtonCaptureStop(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: capture_stop_Click");
            ComboBox_AudioDevices.IsEnabled = true;
            ComboBox_MicDevices.IsEnabled = true;
            if (capture != null)
            {
                //録音を停止
                capture.StopRecording();
                Debug.Print("capture.StopRecording");
                if (is_Mic_Connected)
                {
                    mic_capture.StopRecording();
                    Debug.Print("mic_capture.StopRecording");
                }
            }
        }

        //保存ボタンのクリック処理
        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: Save_Click");
            File_Output();
        }

        //録音ボタン
        private void Button_Capture_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: Capture_Click");
            //録音中の場合は停止処理
            if (recFlag)
            {
                //画像を差し替え
                FadeAnimation(CaptureButtonImage, "Resource/start.png");

                //RECマークを非表示
                RecImage.Visibility = Visibility.Hidden;
                //RECラベルを変更
                Label_status.Content = "";
                // アニメーションを停止
                RecImage.BeginAnimation(UIElement.OpacityProperty, null);

                //録音停止メソッド
                ButtonCaptureStop(sender, e);

                //タスクトレイを変更
                menu_capture_click_button.Text = "録音開始";
                menu_status.Text = "オトコト";
                recFlag = false;

                //タイマーを停止
                _timer.Stop();
            }
            //開始処理
            else
            {
                //画像を差し替え
                FadeAnimation(CaptureButtonImage, "Resource/stop.png");

                //RECマークを表示
                RecImage.Visibility = Visibility.Visible;
                //RECラベルを変更
                Label_status.Content = "録音中...";
                //点滅アニメーションを適用
                RecImage.BeginAnimation(UIElement.OpacityProperty, BlinkAnimation());

                //録音開始メソッド
                ButtonCaptureStart(sender, e);

                //タスクトレイを変更
                menu_capture_click_button.Text = "録音停止";
                menu_status.Text = "録音中...";
                recFlag = true;

                //タイマーを起動
                _timer.Start();
            }
        }

        //Imageコントロールに画像をフェードインで差し替えるメソッド
        private void FadeAnimation(System.Windows.Controls.Image image, string imagePath)
        {
            // フェードアウトアニメーション
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.1));
            fadeOut.Completed += (s, _) =>
            {
                // フェードアウト完了後に画像を変更
                image.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                // フェードインアニメーション
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.1));
                image.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
            // アニメーション開始
            image.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        //点滅アニメーション
        private DoubleAnimation BlinkAnimation()
        {
            DoubleAnimation blinkAnimation;
            blinkAnimation = new DoubleAnimation
            {
                From = 1.0, // 完全に表示
                To = 0.0,   // 完全に非表示
                Duration = TimeSpan.FromSeconds(1), // 半分の時間で切り替え
                AutoReverse = true, // 元の状態に戻る
                RepeatBehavior = RepeatBehavior.Forever // 永遠に繰り返し
            };
            return blinkAnimation;
        }

        //ファイルの保存関数
        private async void File_Output()
        {
            //保存時のダイアログのクラス
            SaveFileDialog sfd = new SaveFileDialog();

            //ログ保存先フォルダ
            string log_directory = "./Logs"; //相対パスで取得
            string directoryPath = System.IO.Path.GetFullPath(log_directory); // 絶対パスに変換

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            //今日の日付を取得
            DateTime today = DateTime.Now;
            //ファイル名の初期設定
            string file_name = today.Year.ToString() + "_" + today.Month.ToString() + "_" + today.Day.ToString() + "_" + today.Hour.ToString() + "_" + today.Minute.ToString() + "_" + today.Second.ToString();

            //ダイアログを表示
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // 初期フォルダを指定（オプション）
                saveFileDialog.InitialDirectory = directoryPath;

                //ファイルの初期名を指定
                saveFileDialog.FileName = file_name;

                // ファイル名のフィルタを設定（オプション）
                saveFileDialog.Filter = "テキストファイル (*.txt)|*.txt";

                // ダイアログを表示し、ユーザーがファイル名を指定した場合
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    file_name = saveFileDialog.FileName; // ユーザーが選択したファイルパスを返す
                }
                else
                {
                    return;  // ユーザーがキャンセルした場合
                }
            }


            // ユーザーがファイル名を指定しなかった場合は処理を終了
            if (string.IsNullOrEmpty(file_name))
            {
                Console.WriteLine("保存するファイル名が指定されていません。");
                return;
            }

            //リアルタイムログを保存順から日付順に並び替え
            List<Conversation_Log_Data> sortedRealtimeLogs = realtimeLogs.OrderBy(log => log.TimeStamp).ToList();
            JsonUtil jsonUtil = new JsonUtil();
            //リアルタイムログをjson化
            //並列処理でフィラー除去を実行
            progressRing.Visibility = Visibility.Visible;
            Filler_Removal.Initialize();
            try
            {
                foreach (Conversation_Log_Data log in sortedRealtimeLogs)
                {
                    Debug.Print("対象のテキスト：" + log.Text);
                    json_list.Add(jsonUtil.ToJson(log.TimeStamp, await Filler_Removal.Removal(log.Text), log.IsSpeaker, log.AudioPath));
                }
            }
            finally
            {
                Debug.Print("Pythonプロセスの終了");
                // Pythonプロセスの終了
                Filler_Removal.Terminate();
            }
            
            //複数のjsonをリスト化
            string log_text = $"[{string.Join(",", json_list)}]";

            try
            {
                // テキストファイルに書き出し
                File.WriteAllText(file_name, log_text);
                Console.WriteLine($"ファイルに書き出しました: {file_name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
            }

            //一時保存したログを消去
            json_list.Clear();
            progressRing.Visibility = Visibility.Hidden;
        }


        // ユーザーにファイル名を指定させるメソッド


        private void Button_Mute_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: Mute_Click");
            if (is_mute)
            {
                //ミュートをオフ
                //Button_Mute.Content = "ミュートOFF";
                is_mute = false;
                if (mic_capture != null)
                {
                    mic_capture.StartRecording();
                    if (M_writer == null)
                    {
                        M_outputPath = System.IO.Path.Combine($"./Audio/M_{DateTime.Now:yyMMddHHmmss}.wav");
                        M_writer = new WaveFileWriter(M_outputPath, mic_capture.WaveFormat);
                    }
                }
                ButtonIcon.Source = new BitmapImage(new Uri("Resource/mic.png", UriKind.Relative));
            }
            else
            {
                //ミュートをオン
                //Button_Mute.Content = "ミュートON";
                is_mute = true;
                if (mic_capture != null)
                {
                    Button_Mute.IsEnabled = false;
                    mic_capture.StopRecording();
                }
                ButtonIcon.Source = new BitmapImage(new Uri("Resource/mic_off.png", UriKind.Relative));
            }
        }

        //mp3に変換して元のwavは削除する関数
        public static string ConvertWavToMp3(string wavFilePath)
        {
            // WAVファイルの存在確認
            if (!File.Exists(wavFilePath))
            {
                throw new FileNotFoundException("WAV file not found.", wavFilePath);
            }

            // 拡張子を除いたファイル名を取得
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(wavFilePath);

            // MP3ファイルのパスを生成
            string mp3FilePath = Path.Combine(Path.GetDirectoryName(wavFilePath), fileNameWithoutExtension + ".mp3");

            // WAVファイルを開く
            using (var reader = new AudioFileReader(wavFilePath))
            {
                // LameMP3FileWriterを使ってMP3に変換
                using (var writer = new LameMP3FileWriter(mp3FilePath, reader.WaveFormat, LAMEPreset.STANDARD))
                {
                    reader.CopyTo(writer);
                }
            }
            // WAVファイルを削除
            File.Delete(wavFilePath);

            // 変換されたMP3ファイルのパスを返す
            return mp3FilePath;
        }
    }
}
