﻿using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using Vosk;
using System.Windows.Controls;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Themes;
using System.Windows.Media;
using System.Windows.Documents;
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
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string modelPath = "Models/vosk-model-ja-0.22";
            try
            {
                //初期化処理
                LoadAudioDevices();
                LoadMicDevices();
                SetupNotifyIcon();
                SetupTimer();
                // モデルとレコグナイザーの初期化
                model = new Model(modelPath);
                recognizer = new VoskRecognizer(model, 16000.0f);
                mic_recognizer = new VoskRecognizer(model, 16000.0f);

                if (!Directory.Exists("Audio"))
                {
                    Directory.CreateDirectory("Audio");
                }

                // すべての初期化が完了したらメインウィンドウを表示
                this.Show();
                Debug.Print("MainWindow shown");
            }
            catch (Exception ex)
            {
                Debug.Print($"初期化エラー: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"アプリケーションの初期化に失敗しました。\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                System.Windows.Application.Current.Shutdown();
            }
        }


        //マテリアルダークテーマ関連
        private bool isDarkMode = false;


        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(isDarkMode ?
                BaseTheme.Dark :
                BaseTheme.Light);

            paletteHelper.SetTheme(theme);

            // ComboBoxのカラー更新
            var resources = System.Windows.Application.Current.Resources;
            if (isDarkMode)
            {
                resources["ComboBoxForegroundBrush"] = resources["ComboBoxForegroundBrushDark"];
                resources["ComboBoxBackgroundBrush"] = resources["ComboBoxBackgroundBrushDark"];
                resources["ComboBoxBorderBrush"] = resources["ComboBoxBorderBrushDark"];
                resources["ComboBoxItemForegroundBrush"] = resources["ComboBoxItemForegroundBrushDark"];
                resources["ComboBoxUnderlineBrush"] = resources["ComboBoxUnderlineBrushDark"];
                resources["TextBorderBackgroundBrush"] = resources["TextBorderBackgroundBrushDark"];
                resources["ListBoxItemBackgroundBrush"] = resources["ListBoxItemBackgroundBrushDark"];
                resources["SeekBarBackgroundBrush"] = resources["SeekBarBackgroundBrushDark"];
                resources["MatchTextBackgroundBrush"] = resources["MatchTextBackgroundBrushDark"];
                resources["IconBrushLight"] = resources["IconBrushDark"];
            }
            else
            {
                resources["ComboBoxForegroundBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                resources["ComboBoxBackgroundBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["ComboBoxBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
                resources["ComboBoxItemForegroundBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                resources["ComboBoxUnderlineBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(103, 58, 183));
                resources["TextBorderBackgroundBrush"] = new SolidColorBrush(Colors.AliceBlue);
                resources["ListBoxItemBackgroundBrush"] = new SolidColorBrush(Colors.AliceBlue);
                resources["SeekBarBackgroundBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));
                resources["MatchTextBackgroundBrush"] = new SolidColorBrush(Colors.Yellow);
                resources["IconBrushLight"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(95, 99, 104));
            }


            Debug.Print($"Theme changed to: {(isDarkMode ? "Dark" : "Light")}");
        }
        public class JsonText
        {
            public string text { get; set; }
            public string partial { get; set; }
        }

        //スピーカーログの日付
        private DateTime speakerDateTime;
        //マイクログの日付
        private DateTime micDateTime;
        //スピーカーログの出力先の行番号(出力中でない場合はnull)
        private int? speakerIndex = null;
        //マイクログの出力先の行番号(出力中でない場合はnull)
        private int? micIndex = null;

        private void UpdateTextBox(string text, bool is_speaker, string audiopath)
        {
            Debug.Print("UpdateTextBox");
            //"text"のみのjsonで送られてくるためパースする
            JsonText json_text = JsonSerializer.Deserialize<JsonText>(text) ?? new JsonText();
            json_text.text = json_text.text.Replace(" ", "");
            Debug.Print("json_text:" + json_text.text);

            //nullか空でない場合に書き起こす
            if (json_text.text != null && json_text.text != "")
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
                            RealtimeListBox.Items[(int)speakerIndex] = new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker, AudioPath = audiopath, IsComit = true };
                        }
                        //出力中のテキストがなければ行追加して出力する
                        else
                        {
                            speakerDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = speakerDateTime + " (スピーカー)", IsHighlighted = false, IsSpeaker = is_speaker });
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker, AudioPath = audiopath, IsComit = true });
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
                            RealtimeListBox.Items[(int)micIndex] = new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker, AudioPath = audiopath, IsComit = true };
                        }
                        //出力中のテキストがなければ行追加して出力する
                        else
                        {
                            micDateTime = DateTime.Now;
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = micDateTime + " (マイク)", IsHighlighted = false, IsSpeaker = is_speaker });
                            RealtimeListBox.Items.Add(new ListBoxModel { Text = json_text.text, IsHighlighted = true, IsSpeaker = is_speaker, AudioPath = audiopath, IsComit = true });
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
                catch (Exception ex)
                {
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
                        if (speakerIndex == null)
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

        private LogWindow ?logWindow;
        private void Button_Log_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Button: Log_Click");
            if (logWindow == null || !logWindow.IsLoaded)
            {
                logWindow = null;
                logWindow = new LogWindow();
                //logWindow.Owner = this;
                logWindow.Left = Main_Window.Left + 10;
                logWindow.Top = Main_Window.Top + 10;
                logWindow.Show();
            }
            else
            {
                logWindow.Activate();
            }
        }

        private void ComboBox_AudioDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private int _secondsElapsed = 0; // 経過秒数を格納
        // タイマメソッド
        private void MyTimerMethod(object sender, EventArgs e)
        {
            _secondsElapsed++; // 秒数をインクリメント

            // 時間・分・秒を計算
            int hours = _secondsElapsed / 3600;
            int minutes = (_secondsElapsed % 3600) / 60;
            int seconds = _secondsElapsed % 60;

            // フォーマットして表示
            this.RecordingTimeText.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        }
        // タイマのインスタンス
        private DispatcherTimer _timer;
        // タイマを設定する
        private void SetupTimer()
        {
            // タイマのインスタンスを生成
            _timer = new DispatcherTimer(); // 優先度はDispatcherPriority.Background
                                            // インターバルを設定
            _timer.Interval = new TimeSpan(0, 0, 1);
            // タイマメソッドを設定
            _timer.Tick += new EventHandler(MyTimerMethod);
            // タイマを開始
            //_timer.Start();

            // 画面が閉じられるときに、タイマを停止
            //this.Closing += new CancelEventHandler(StopTimer);
        }

        // タイマを停止
        private void StopTimer(object sender, CancelEventArgs e)
        {
            _timer.Stop();
        }

        //再生ボタンのクリックイベント
        private void AudioButtonClick(object sender, RoutedEventArgs e)
        {
            //senderからボタンを取得
            if (sender is System.Windows.Controls.Button button)
            {
                //ボタンが含まれるリストボックスのアイテムを取得
                ListBoxModel listBoxModel = button.DataContext as ListBoxModel;
                //ボタンのタグからImageを取得
                if (button.Tag is System.Windows.Controls.Image image && listBoxModel != null)
                {
                    AudioPlaying(image, listBoxModel);
                }
            }
        }

        //PlayAudio.playingImageには再生中の音声のボタンの画像が入っている
        //nullの場合は再生中ではない
        private void AudioPlaying(System.Windows.Controls.Image image, ListBoxModel log)
        {
            //ファイルが存在しない場合
            if (!File.Exists(log.AudioPath))
            {
                System.Windows.MessageBox.Show("音声ファイルが存在しません");
                return;
            }
            //再生中の音声がない場合、再生する
            if (PlayAudio.playingImage == null)
            {
                PlayAudio.ChangeToStopImage(image);
                PlayAudio.play(log.AudioPath, image, true);
            }
            //再生中の音声がクリックした音声と同じ場合、再生を止める
            else if (PlayAudio.playingImage == image)
            {
                PlayAudio.ChangeToStartImage();
                PlayAudio.stop(true);
            }
            //再生中の音声がクリックした音声と違う場合、再生中の音声を止め、選択した音声を再生する
            else
            {
                PlayAudio.ChangeToStartImage();
                PlayAudio.ChangeToStopImage(image);
                PlayAudio.play(log.AudioPath, image, true);   //playメソッドの冒頭で再生中の音声を止めている
            }
        }

        private void ThemeToggleButton_Checked(object sender, RoutedEventArgs e)
        {

        }
        //↑なにこれ

        //リアルタイムログのクリアボタンイベント
        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            //ログクリア
            RealtimeListBox.Items.Clear();
            realtimeLogs.Clear();
            //タイマーリセット
            RecordingTimeText.Text = "00:00:00";
            _secondsElapsed = 0;
            //再生中の音声を停止
            PlayAudio.stop(true);
        }
    }

    //ログリストボックスに追加するアイテムのデータモデル
    public class ListBoxModel : INotifyPropertyChanged
    {
        public string Text { get; set; }       //ログのテキスト
        public string Memory { get; set; }


        private string _beforeText;
        private string _matchText;
        private string _afterText;
        private bool _isHighlighted;
        public System.Windows.Media.Brush Background { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string BeforeText
        {
            get => _beforeText?.Trim();
            set
            {
                if (_beforeText != value)
                {
                    _beforeText = value;
                    OnPropertyChanged(nameof(BeforeText));
                }
            }
        }

        public string MatchText
        {
            get => _matchText?.Trim();
            set
            {
                if (_matchText != value)
                {
                    _matchText = value;
                    OnPropertyChanged(nameof(MatchText));
                }
            }
        }

        public string AfterText
        {
            get => _afterText?.Trim();
            set
            {
                if (_afterText != value)
                {
                    _afterText = value;
                    OnPropertyChanged(nameof(AfterText));
                }
            }
        }


        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnPropertyChanged(nameof(IsHighlighted));
                }
            }
        }

        /*
        private List<Inline> _textInlines; 
        public List<Inline> TextInlines
        {
            get => _textInlines; set
            {
                if (_textInlines != value)
                {
                    _textInlines = value; OnPropertyChanged(nameof(TextInlines));
                }
            }
        }
        */

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //public bool IsHighlighted { get; set; } //背景ありか(日時かテキストか)
        public bool IsSpeaker { get; set; }    //スピーカーかマイクか
        public string AudioPath { get; set; }  //音声ファイルのパス
        public bool IsComit { get; set; }       //テキスト確定済みか(リアルタイムログで使用)

        public GridLength FirstColumnWidth = GridLength.Auto;
        public GridLength SecondColumnWidth = new GridLength(1, GridUnitType.Star);
    }
}