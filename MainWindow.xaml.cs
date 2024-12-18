using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using Vosk;
using System.Windows.Controls;
using System.ComponentModel;
using System.IO;
namespace HottoMotto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VoskRecognizer recognizer;
        private VoskRecognizer mic_recognizer;

        //リアルタイムログの保存先
        private List<Conversation_Log_Data> realtimeLogs = new List<Conversation_Log_Data>();
        //リアルタイムログのjsonリスト
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

            //音声ファイル保存先フォルダが存在しない場合は作成
            // フォルダが存在しない場合は作成
            if (!Directory.Exists("Audio"))
            {
                Directory.CreateDirectory("Audio");
            }
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

        private void UpdateTextBox(string text, bool is_speaker,string audiopath)
        {
            Debug.Print("UpdateTextBox");
            //"text"のみのjsonで送られてくるためパースする
            JsonText json_text = JsonSerializer.Deserialize<JsonText>(text) ?? new JsonText();
            json_text.text = json_text.text.Replace(" ", "");
            Debug.Print("json_text:" + json_text.text);

            //nullか空でない場合に書き起こす
            if(json_text.text != null && json_text.text != "")
            {
                DateTime dateTime = DateTime.Now;

                // Dispatcher.Invokeを使用してUIスレッドで実行
                try
                {
                    RealtimeListBox.Dispatcher.BeginInvoke(() =>
                {
                    Debug.Print("Dispatcher.Invoke");
                    //スピーカー音声の処理
                    if (is_speaker)
                    {
                        //出力中のテキストを上書きして確定する
                        if (speakerIndex != null)
                        {
                            RealtimeListBox.Items[(int)speakerIndex] = new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker };
                        }
                        //出力中のテキストがなければ行追加して出力する
                        else
                        {
                            speakerDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = speakerDateTime + " (スピーカー)", IsHighlighted = false, IsSpeaker = is_speaker });
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker });
                        }
                        //確定したテキストをjson化してリストに入れる
                        speakerIndex = null;
                        realtimeLogs.Add(new Conversation_Log_Data
                        {
                            TimeStamp = speakerDateTime,
                            Text = json_text.text,
                            IsSpeaker = is_speaker,
                            AudioPath = audiopath,
                        });
                    }
                    //マイク音声の処理
                    else
                    {
                        //出力中のテキストを上書きして確定する
                        if (micIndex != null)
                        {
                            RealtimeListBox.Items[(int)micIndex] = new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker };
                        }
                        //出力中のテキストがなければ行追加して出力する
                        else
                        {
                            micDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = micDateTime + " (マイク)", IsHighlighted = false, IsSpeaker = is_speaker });
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker });
                        }
                        //確定したテキストをjson化してリストに入れる
                        micIndex = null;
                        realtimeLogs.Add(new Conversation_Log_Data
                        {
                            TimeStamp = micDateTime,
                            Text = json_text.text,
                            IsSpeaker = is_speaker,
                            AudioPath = audiopath,
                        });
                    }
                });
                }
                catch (Exception ex){
                    Debug.Print(ex.ToString());
                }
            }
        }

        private void UpdateToPartial(string partial, bool is_speaker)
        {
            //"partial"のみのjsonで送られてくるためパースする
            JsonText json_text = JsonSerializer.Deserialize<JsonText>(partial) ?? new JsonText();
            json_text.partial = json_text.partial.Replace(" ", "");

            //空文字を除去
            if (json_text.partial != null && json_text.partial != "")
            {
                // Dispatcher.Invokeを使用してUIスレッドで実行
                RealtimeListBox.Dispatcher.BeginInvoke(() =>
                {
                    //スピーカー音声の処理
                    if (is_speaker)
                    {
                        //出力中のテキストがなければ行を追加して出力開始
                        if(speakerIndex == null)
                        {
                            speakerDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = speakerDateTime + " (スピーカー)", IsHighlighted = false, IsSpeaker = is_speaker });
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = json_text.partial, IsHighlighted = true, IsSpeaker = is_speaker });
                            //出力中の行番号を保存
                            speakerIndex = RealtimeListBox.Items.Count - 1;
                            RealtimeListBox.ScrollIntoView(RealtimeListBox.Items[RealtimeListBox.Items.Count - 1]);
                        }
                        else
                        {
                            //出力中のテキストを上書き
                            RealtimeListBox.Items[(int)speakerIndex] = new ListBoxModel { Text = json_text.partial, IsHighlighted = true, IsSpeaker = is_speaker };
                        }
                    }
                    //マイク音声の処理
                    else
                    {
                        //出力中のテキストがなければ行を追加して出力開始
                        if (micIndex == null)
                        {
                            micDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = micDateTime + " (マイク)", IsHighlighted = false, IsSpeaker = is_speaker });
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = json_text.partial, IsHighlighted = true, IsSpeaker = is_speaker });
                            //出力中の行番号を保存
                            micIndex = RealtimeListBox.Items.Count - 1;
                            RealtimeListBox.ScrollIntoView(RealtimeListBox.Items[RealtimeListBox.Items.Count - 1]);
                        }
                        else
                        {
                            //出力中のテキストを上書き
                            RealtimeListBox.Items[(int)micIndex] = new ListBoxModel { Text = json_text.partial, IsHighlighted = true, IsSpeaker = is_speaker };
                        }
                    }
                });
            }
        }

           



        //conboboxのやーつ
        private void ClearAudioDevices_Click(object sender, RoutedEventArgs e)
        {
            ComboBox_AudioDevices.SelectedIndex = -1;
        }

        private void ClearMicDevices_Click(object sender, RoutedEventArgs e)
        {
            ComboBox_MicDevices.SelectedIndex = -1;
        }

        private void Button_Log_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: Log_Click");
            LogWindow logWindow = new LogWindow();
            logWindow.Show();
        }

        private void ComboBox_AudioDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
    public class ListBoxModel
    {
        public string Text {  get; set; }       //ログのテキスト
        public bool IsHighlighted { get; set; } //背景ありか(日時かテキストか)
        public bool IsSpeaker {  get; set; }    //スピーカーかマイクか
        public string AudioPath {  get; set; }
    }
}