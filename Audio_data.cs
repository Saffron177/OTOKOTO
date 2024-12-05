﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                                UpdateTextBox(result, true);
                            }
                            else
                            {
                                //partialの処理
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
                    mic_capture.Dispose();
                    mic_capture = null;
                    Debug.Print("Stop");
                    // 最終結果を取得
                    var finalResult = recognizer.FinalResult();
                    UpdateTextBox(finalResult, true);
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
                        UpdateTextBox(result, false);
                    }
                    else
                    {
                        //partialの処理
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

                //ログ保存先フォルダ
                string log_directory = "../../../../Logs";
                // フォルダが存在しない場合は作成
                if (!Directory.Exists(log_directory))
                {
                    Directory.CreateDirectory(log_directory);
                }
                //ログファイルの採番
                int log_index = 1;
                string file_path = "";
                //// ファイルが存在しない番号を探す
                do
                {
                    file_path = Path.Combine(log_directory, $"log{log_index}.txt");
                    log_index++;
                } while (File.Exists(file_path));

                //複数のjsonをリスト化
                string log_text = $"{{{string.Join(",", json_list)}}}";

                Debug.Print("以下、ログテキスト");
                Debug.Print(log_text);

                try
                {
                    // テキストファイルに書き出し
                    File.WriteAllText(file_path, log_text);

                    Console.WriteLine($"ファイルに書き出しました: {file_path}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"エラーが発生しました: {ex.Message}");
                }

                json_list.Clear();
            }
        }

        private void Button_Mute_Click(object sender, RoutedEventArgs e)
        {
            if (is_mute)
            {
                //ミュートをオフ
                Button_Mute.Content = "ミュートOFF";
                is_mute = false;
                if (mic_capture != null)
                {
                    mic_capture.StartRecording();
                }
            }
            else
            {
                //ミュートをオン
                Button_Mute.Content = "ミュートON";
                is_mute = true;
                if (mic_capture != null)
                {
                    Button_Mute.IsEnabled = false;
                    mic_capture.StopRecording();
                }
            }
        }
    }
}
