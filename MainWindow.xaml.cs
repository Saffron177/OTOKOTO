﻿using Microsoft.VisualBasic.Logging;
using System;
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
            string modelPath = "Models/vosk-model-ja-0.22";
            Console.WriteLine("モデルパス: " + modelPath);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);
            mic_recognizer = new VoskRecognizer(model, 16000.0f);
            SetupNotifyIcon();
        }

        public class JsonText
        {
            public string text { get; set; }
            public string partial {  get; set; }
        }

        //スピーカーログの日付
        private DateTime speakerDateTime;
        //マイクログの日付
        private DateTime micDateTime;
        //スピーカーログの出力先の行番号(出力中でない場合はnull)
        private int? speakerIndex = null;
        //マイクログの出力先の行番号(出力中でない場合はnull)
        private int? micIndex = null;

        private void UpdateTextBox(string text, bool is_speaker)
        {
            //"text"のみのjsonで送られてくるためパースする
            JsonText json_text = JsonSerializer.Deserialize<JsonText>(text) ?? new JsonText();
            Debug.Print("json_text:" + json_text.text);

            //nullか空でない場合に書き起こす
            if(json_text.text != null && json_text.text != "")
            {
                DateTime dateTime = DateTime.Now;
                JsonUtil jsonutil = new JsonUtil();

                // Dispatcher.Invokeを使用してUIスレッドで実行
                RealtimeListBox.Dispatcher.Invoke(() =>
                {
                    //スピーカー音声の処理
                    if (is_speaker)
                    {
                        //出力中のテキストを上書きして確定する
                        if(speakerIndex != null)
                        {
                            RealtimeListBox.Items[(int)speakerIndex] = json_text.text;
                        }
                        //出力中のテキストがなければ行追加して出力する
                        else
                        {
                            speakerDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(speakerDateTime + " (スピーカー)");
                            RealtimeListBox.Items.Add(json_text.text);
                        }
                        //確定したテキストをjson化してリストに入れる
                        speakerIndex = null;
                        json_list.Add(jsonutil.ToJson(speakerDateTime, json_text.text, is_speaker));
                    }
                    //マイク音声の処理
                    else
                    {
                        //出力中のテキストを上書きして確定する
                        if (micIndex != null)
                        {
                            RealtimeListBox.Items[(int)micIndex] = json_text.text;
                        }
                        //出力中のテキストがなければ行追加して出力する
                        else
                        {
                            micDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(micDateTime + " (マイク)");
                            RealtimeListBox.Items.Add(json_text.text);
                        }
                        //確定したテキストをjson化してリストに入れる
                        micIndex = null;
                        json_list.Add(jsonutil.ToJson(micDateTime, json_text.text, is_speaker));
                    }
                });
            }
        }

        private void UpdateToPartial(string partial, bool is_speaker)
        {
            //"partial"のみのjsonで送られてくるためパースする
            JsonText json_text = JsonSerializer.Deserialize<JsonText>(partial) ?? new JsonText();

            //空文字を除去
            if(json_text.partial != null && json_text.partial != "")
            {
                // Dispatcher.Invokeを使用してUIスレッドで実行
                RealtimeListBox.Dispatcher.Invoke(() =>
                {
                    //スピーカー音声の処理
                    if (is_speaker)
                    {
                        //出力中のテキストがなければ行を追加して出力開始
                        if(speakerIndex == null)
                        {
                            speakerDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(speakerDateTime + " (スピーカー)");
                            RealtimeListBox.Items.Add(json_text.partial);
                            //出力中の行番号を保存
                            speakerIndex = RealtimeListBox.Items.Count - 1;
                        }
                        else
                        {
                            //出力中のテキストを上書き
                            RealtimeListBox.Items[(int)speakerIndex] = json_text.partial;
                        }
                    }
                    //マイク音声の処理
                    else
                    {
                        //出力中のテキストがなければ行を追加して出力開始
                        if (micIndex == null)
                        {
                            micDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(micDateTime + " (マイク)");
                            RealtimeListBox.Items.Add(json_text.partial);
                            //出力中の行番号を保存
                            micIndex = RealtimeListBox.Items.Count - 1;
                        }
                        else
                        {
                            //出力中のテキストを上書き
                            RealtimeListBox.Items[(int)micIndex] = json_text.partial;
                        }
                    }
                });
            }
        }

        private void Button_Log_Click(object sender, RoutedEventArgs e)
        {
            LogWindow logWindow = new LogWindow();
            logWindow.Show();
        }
    }
}