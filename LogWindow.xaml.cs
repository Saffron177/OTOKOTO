using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using NAudio.Wave;
using System.ComponentModel;
using System.Windows.Threading;

namespace HottoMotto
{
    /// <summary>
    /// LogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LogWindow : Window
    {
        //ファイル一覧を保持
        string[] txtFiles;
        //検索中かどうかを判定
        bool search_enabled = false;
        int matchCounter = -1;  // マッチ数カウンター(なんでか忘れたけど初期値は0じゃなくて-1です)
        public static DispatcherTimer timer; // 再生バー更新用タイマー
        public static Slider seekBar;
        public static TextBlock totalTime;
        public static TextBlock currentTime;

        public LogWindow()
        {
            InitializeComponent();
            LogFileCheck();
            logList.MouseDoubleClick += LogList_DoubleClick;
            // ビューモデルを設定
            this.DataContext = new MatchModel
            {
                Match_Label = ""
            };
            // タイマーの設定
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 100msごとに更新
            };
            timer.Tick += Timer_Tick;
            seekBar = SeekBar;
            totalTime = TotalTime;
            currentTime = CurrentTime;
        }

        //Logsファイルの中身を検索し、結果を出力する
        public void LogFileCheck()
        {
            string relativePath = "./Logs"; // チェックしたいディレクトリのパス
            string directoryPath = System.IO.Path.GetFullPath(relativePath); // 絶対パスに変換

            if (Directory.Exists(directoryPath))
            {
                // ディレクトリ内のすべてのtxtファイルを検索
                txtFiles = Directory.GetFiles(directoryPath, "*.txt");

                // 取得したtxtファイルをListBoxに表示
                logList.ItemsSource = new List<object>(); //ListBoxの中身を初期化

                if (txtFiles.Length > 0) //logfileが見つかればfile名を出力
                {
                    List<object> items = new List<object>();
                    foreach (string file in txtFiles)
                    {
                        items.Add(new { Text = System.IO.Path.GetFileName(file) });
                    }
                    logList.ItemsSource = (items);
                }
                else //fileがない場合
                {
                    logList.ItemsSource = new List<object> { new { Text = "ログが見つかりませんでした。" } };
                }
            }
            else //ディレクトリが存在しない場合
            {
                //logList.Items.Add("ログを保存するとここから確認できるようになります。");
                logList.ItemsSource = new List<object> { new { Text = "ログを保存するとここから確認できるようになります。" } };
            }
        }

        // listBox中身をダブルクリックしたときに実行する関数
        private void LogList_DoubleClick(object sender, EventArgs e)
        {
            if (logList.SelectedItem != null)
            {
                var item = logList.SelectedItem as dynamic;
                string selectedFile = item.Text;

                Debug.Print("selectedFile:" + selectedFile);

                //ログファイルの読み取りを実行
                Load_Log(selectedFile);

                InitSoundData();

                if(SoundData.Visibility != Visibility.Visible)
                {
                    SoundData.Visibility = Visibility.Visible;
                }
            }
        }

        //ログファイルの内容をListBoxに表示
        private void Load_Log(string selectedFile)
        {
            string filePath = "./Logs/" + selectedFile;

            // ファイルが存在するかチェック
            if (!File.Exists(filePath))
            {
                Debug.Print("JSONファイルが見つかりません。");
                return;
            }

            // ファイルの内容を読み込む
            string jsonText = File.ReadAllText(filePath);
            //ファイル名を表示
            file_Title.Content = System.IO.Path.GetFileName(filePath);
            Copy_Button.ToolTip = "コピー";
            Copy_Button.Visibility = Visibility.Visible;

            try
            {
                // JSONをリストに変換（デシリアライズ）
                var logs = JsonSerializer.Deserialize<List<Conversation_Log_Data>>(jsonText);

                //リストボックスを初期化
                LogListBox.Items.Clear();
                // リストボックスにデータを追加
                foreach (var log in logs)
                {
                    // 2025/01/10(金)
                    // ItemControlはWrap設定を無視してしまうため削除した。
                    // それに伴い、ListBoxModelのTextInlinesは使わず、
                    // MainWindowと同様にTextにバインディングする方式に変更した。
                    // HighlightText関数の処理をTextInlinesを使わない処理に変更した。
                    // 以上の変更で改行されるようになったことを確認したが、
                    // スクロールして一度画面外に出るとハイライトが消えてしまう問題が発生したため、
                    // ListBoxに仮想化処理をオフにするプロパティを追加して対応した。

                    LogListBox.Items.Add(new ListBoxModel
                    {
                        Memory = (log.TimeStamp + (log.IsSpeaker ? "(スピーカー)" : "(マイク)")),
                        IsHighlighted = false,
                        IsSpeaker = log.IsSpeaker,
                        Text = (log.TimeStamp + (log.IsSpeaker ? "(スピーカー)" : "(マイク)"))
                        /*
                        TextInlines = new List<Inline>
                        {
                            new Run { Text = (log.TimeStamp + (log.IsSpeaker ? "(スピーカー)" : "(マイク)"))}
                        }
                        */
                    });
                    LogListBox.Items.Add(new ListBoxModel
                    {
                        Memory = log.Text,
                        IsHighlighted = true,
                        IsSpeaker = log.IsSpeaker,
                        AudioPath = log.AudioPath,
                        Text = log.Text
                        /*
                        TextInlines = new List<Inline>
                        {
                            new Run { Text = log.Text}
                        }
                        */
                    });

                }
            }
            catch (JsonException ex)
            {
                Debug.Print($"JSONの解析に失敗しました: {ex.Message}");
            }
        }

        //ログ一覧内のファイル名を検索する
        private void Search_Textbox_Changed(object sender, TextChangedEventArgs e)
        {
            if (txtFiles != null)
            {
                string searchWord = search_Textbox.Text; // 検索ボックスのテキストを取得
                var filteredFiles = txtFiles
                    .Where(fileName => System.IO.Path.GetFileName(fileName).ToLower().Contains(searchWord)) // 部分一致で検索
                    .ToArray();

                UpdateListBox(filteredFiles); // 検索結果でListBoxを更新
            }

        }

        // ListBoxの内容を更新するメソッド
        private void UpdateListBox(string[] filteredFiles)
        {
            //リストボックスを初期化
            logList.ItemsSource = new List<object>();
            if (filteredFiles.Length <= 0)
            {
                logList.ItemsSource = new List<object> { new { Text = "ファイルが見つかりませんでした。" } };
                return;
            }
            List<object> items = new List<object>();
            foreach (string file in filteredFiles)
            {
                items.Add(new { Text = System.IO.Path.GetFileName(file) });
            }
            logList.ItemsSource = (items);
        }

        //コピーボタンのクリック処理
        private void Copy_Button_Click(object sender, RoutedEventArgs e)
        {
            if (LogListBox.Items.Count > 0)
            {
                // すべてのアイテムを文字列として結合
                string allItemsText = string.Join("\n", LogListBox.Items.Cast<ListBoxModel>().Select(item => item.Memory));

                // クリップボードにコピー
                System.Windows.Clipboard.SetText(allItemsText);
                Copy_Button.ToolTip = "コピーしました";
            }
            else
            {
                System.Windows.MessageBox.Show("リストボックスにアイテムがありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void InitSoundData()
        {
            TotalTime.Text = "00:00";
            CurrentTime.Text = "00:00";
            SeekBar.Value = 0;
            timer.Stop();
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
                PlayAudio.play(log.AudioPath, image, false);
            }
            //再生中の音声がクリックした音声と同じ場合、再生を止める
            else if (PlayAudio.playingImage == image)
            {
                PlayAudio.ChangeToStartImage();
                PlayAudio.stop(false);
            }
            //再生中の音声がクリックした音声と違う場合、再生中の音声を止め、選択した音声を再生する
            else
            {
                PlayAudio.ChangeToStartImage();
                PlayAudio.ChangeToStopImage(image);
                PlayAudio.play(log.AudioPath, image, false);   //playメソッドの冒頭で再生中の音声を止めている
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (PlayAudio.reader != null)
            {
                // 再生位置の更新
                SeekBar.Value = PlayAudio.reader.CurrentTime.TotalSeconds;
                CurrentTime.Text = PlayAudio.reader.CurrentTime.ToString(@"mm\:ss");
            }
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ユーザーがシークバーを動かしたときに再生位置を変更
            if (PlayAudio.reader != null && SeekBar.IsMouseCaptureWithin)
            {
                PlayAudio.reader.CurrentTime = TimeSpan.FromSeconds(SeekBar.Value);
            }
        }

        //ログ内のテキストを検索する
        private void Log_Search_Textbox_Textchanged(object sender, TextChangedEventArgs e)
        {
            HighlightText();
        }

        //検索ボックスのテキストと一致するテキストを抽出する
        private async void HighlightText()
        {
            string searchText = log_Search_Textbox.Text;
            // マッチカウンターを初期化
            matchCounter = -1;
            foreach (var item in LogListBox.Items)
            {
                ListBoxItem listBoxItem = null;
                while (listBoxItem == null)
                {
                    await System.Threading.Tasks.Task.Delay(10);
                    listBoxItem = LogListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                }

                if (LogListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    listBoxItem = LogListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        ListBoxModel listBoxModel = listBoxItem.Content as ListBoxModel;
                        var inlines = new List<Inline>();

                        if (!string.IsNullOrEmpty(searchText))
                        {
                            string itemText = listBoxModel.Memory.ToLower();
                            int startIndex = 0;
                            int lastIndex = 0;

                            while ((startIndex = itemText.IndexOf(searchText.ToLower(), lastIndex)) != -1)
                            {
                                // マッチ部分の前のテキストを追加
                                inlines.Add(new Run
                                {
                                    Text = listBoxModel.Memory.Substring(lastIndex, startIndex - lastIndex)
                                });

                                // マッチ部分のテキストをハイライトして追加
                                inlines.Add(new Run
                                {
                                    Text = listBoxModel.Memory.Substring(startIndex, searchText.Length),
                                    //Background = System.Windows.Media.Brushes.Yellow
                                    //↓ダークテーマ対応したが挙動に問題あり
                                    Background = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["MatchTextBackgroundBrush"]
                                });
                                matchCounter++;
                                // 次の検索開始位置を更新
                                lastIndex = startIndex + searchText.Length;

                            }

                            // 最後のマッチ部分後のテキストを追加
                            inlines.Add(new Run
                            {
                                Text = listBoxModel.Memory.Substring(lastIndex)
                            });

                            // テキストの変更を通知するためのプロパティを更新
                            //listBoxModel.TextInlines = inlines;
                            //マッチ数表示
                            //search_enabled = true;
                            //Search_Enabled(search_enabled);

                            //テキストブロックを取得
                            TextBlock textBlock = FindVisualChild<TextBlock>(listBoxItem);
                            if (textBlock != null)
                            {
                                // 既存のInlinesをクリア
                                textBlock.Inlines.Clear();
                                textBlock.Inlines.AddRange(inlines);
                            }
                        }
                        else
                        {
                            // 検索テキストがない場合は元のテキストをそのまま表示
                            inlines.Add(new Run { Text = listBoxModel.Memory });
                            TextBlock textBlock = FindVisualChild<TextBlock>(listBoxItem);
                            if (textBlock != null)
                            {
                                // 既存のInlinesをクリア
                                textBlock.Inlines.Clear();
                                textBlock.Inlines.AddRange(inlines);
                            }
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("エラー");
                    }
                }
                else
                {
                    LogListBox.ItemContainerGenerator.StatusChanged += (s, args) =>
                    {
                        if (LogListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                        {
                            HighlightText();
                        }
                    };
                }
            }
        }

        // 子要素を検索するためのユーティリティメソッド
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }

        //マッチ数を表示するラベルを管理する関数
        private void Search_Enabled(bool search_enable)
        {
            if (search_enable)
            {
                var matchModel = (MatchModel)this.DataContext;
                matchModel.Match_Label = matchCounter.ToString() + "件見つかりました";
                SearchId.Visibility = Visibility.Visible;
            }
            else
            {
                SearchId.Visibility = Visibility.Collapsed;
            }
        }

        // 読み込まれないとマッチ数が増えないので読み込まれるたびにラベルが変わるようにクラスで管理してます
        public class MatchModel : INotifyPropertyChanged
        {
            private string _match_label;

            public string Match_Label
            {
                get => _match_label;
                set
                {
                    if (_match_label != value)
                    {
                        _match_label = value;
                        OnPropertyChanged(nameof(Match_Label));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //ログサーチボックスのkeydownイベント
        private void log_Search_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //HighlightText();
            }

        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(
                "本当に削除しますか？", // メッセージ
                "", // タイトル
                MessageBoxButton.YesNo, // ボタンの種類
                MessageBoxImage.Question // アイコンの種類
            );

            // 結果が「はい」の場合
            if (result == MessageBoxResult.Yes)
            {
                // 「はい」を押したときの処理
                DeleteLog(sender, e);
            }
        }

        private void DeleteLog(object sender, RoutedEventArgs e)
        {
            //ボタンを取得
            System.Windows.Controls.Button? button = sender as System.Windows.Controls.Button;

            if (button != null)
            {
                //ファイル名を取得
                var dataContext = button.DataContext;
                String text = dataContext?.GetType().GetProperty("Text")?.GetValue(dataContext)?.ToString() ?? "";

                string filePath = "./Logs/" + text;

                // ファイルが存在するかチェック
                if (!File.Exists(filePath))
                {
                    Debug.Print("JSONファイルが見つかりません。");
                    return;
                }

                // ファイルの内容を読み込む
                string jsonText = File.ReadAllText(filePath);

                // JSONをリストに変換（デシリアライズ）
                List<Conversation_Log_Data> logs = JsonSerializer.Deserialize<List<Conversation_Log_Data>>(jsonText) ?? new List<Conversation_Log_Data>();

                //音声ファイル削除
                foreach (Conversation_Log_Data log in logs)
                {
                    if (File.Exists(log.AudioPath))
                    {
                        File.Delete(log.AudioPath);
                        Debug.Print("音声ファイル<" + log.AudioPath + ">が削除されました。");
                    }
                }

                //ログファイルを削除
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Print("ログファイル<" + filePath + ">が削除されました。");
                }

                //ファイル一覧を更新
                LogFileCheck();

                //ログを初期化
                file_Title.Content = "";
                LogListBox.Items.Clear();

                //再生中の音声を停止
                PlayAudio.stop(false);
            }
        }
    }

}
