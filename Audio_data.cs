using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

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
                                Debug.Print("Audio" + result);
                                UpdateTextBox(result, true);
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
                    writer = null;
                    capture.Dispose();
                    capture = null;
                    if (is_Mic_Connected)//マイクが接続されているときのみ
                    {
                        mic_capture.Dispose();
                        mic_capture = null;
                    }
                    Debug.Print("Stop");
                    // 最終結果を取得
                    var finalResult = recognizer.FinalResult();
                    UpdateTextBox(finalResult, true);
                };

                //マイク録音の処理--マイクが接続されているときのみ
                if (is_Mic_Connected)
                {
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
                            UpdateTextBox(result, false);
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
                        var finalResult = mic_recognizer.FinalResult();
                        UpdateTextBox(finalResult, false);
                        Button_Mute.IsEnabled = true;

                    };
                }

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
        private void ButtonCaptureStop(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: capture_stop_Click");
            ComboBox_AudioDevices.IsEnabled = true;
            ComboBox_MicDevices.IsEnabled = true;
            if (capture != null)
            {
                //録音を停止
                capture.StopRecording();
                Label_status.Content = "録音停止";
                if (is_Mic_Connected)
                {
                    mic_capture.StopRecording();
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
                //ボタンの画像を差し替え
                CaptureStopImage.Source = new BitmapImage(new Uri(@"Resource/start.png", UriKind.Relative));
                //RECマークを非表示
                RecImage.Visibility = Visibility.Hidden;
                //録音停止メソッド
                ButtonCaptureStop(sender, e);

                //タスクトレイを変更
                menu_capture_click_button.Text = "録音開始";
                menu_status.Text = "Welcome";
                recFlag = false;
            }
            //開始処理
            else
            {
                //ボタンの画像を差し替え
                CaptureStopImage.Source = new BitmapImage(new Uri(@"Resource/stop.png", UriKind.Relative));
                //RECマークを表示
                RecImage.Visibility = Visibility.Visible;
                //録音開始メソッド
                ButtonCaptureStart(sender, e);

                //タスクトレイを変更
                menu_capture_click_button.Text = "録音停止";
                menu_status.Text = "録音中...";
                recFlag = true;
            }
        }

        //ファイルの保存関数
        private void File_Output()
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
            foreach (Conversation_Log_Data log in sortedRealtimeLogs)
            {
                json_list.Add(jsonUtil.ToJson(log.TimeStamp, log.Text, log.IsSpeaker));
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
                }
                ButtonIcon.Source = new BitmapImage(new Uri("Resource/unmute.png", UriKind.Relative));
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
                ButtonIcon.Source = new BitmapImage(new Uri("Resource/mute.png", UriKind.Relative));
            }
        }
    }
}
