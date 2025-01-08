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
                logList.Items.Clear(); //ListBoxの中身を初期化

                if (txtFiles.Length > 0) //logfileが見つかればfile名を出力
                {

                    foreach (string file in txtFiles)
                    {
                        logList.Items.Add(System.IO.Path.GetFileName(file));
                    }
                }
                else //fileがない場合
                {
                    logList.Items.Add("ログが見つかりませんでした。");
                }
            }
            else //ディレクトリが存在しない場合
            {
                logList.Items.Add("ログを保存するとここから確認できるようになります。");
            }
        }

        // listBox中身をダブルクリックしたときに実行する関数
        private void LogList_DoubleClick(object sender, EventArgs e)
        {
            if (logList.SelectedItem != null)
            {
                string selectedFile = logList.SelectedItem.ToString() ?? "";

                Debug.Print("selectedFile:" + selectedFile);

                //ログファイルの読み取りを実行
                Load_Log(selectedFile);
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

                LogListBox.Items.Clear();
                // リストボックスにデータを追加
                foreach (var log in logs)
                {
                    LogListBox.Items.Add(new ListBoxModel
                    {
                        Memory = (log.TimeStamp + (log.IsSpeaker ? "(スピーカー)" : "(マイク)")),
                        IsHighlighted = false,
                        IsSpeaker = log.IsSpeaker,
                        TextInlines = new List<Inline>
                        {
                            new Run { Text = (log.TimeStamp + (log.IsSpeaker ? "(スピーカー)" : "(マイク)"))}
                        }
                    });
                    LogListBox.Items.Add(new ListBoxModel
                    {
                        Memory = log.Text,
                        IsHighlighted = true,
                        IsSpeaker = log.IsSpeaker,
                        AudioPath = log.AudioPath,
                        TextInlines = new List<Inline>
                        {
                            new Run { Text = log.Text}
                        }
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
            logList.Items.Clear();
            if (filteredFiles.Length <= 0)
            {
                logList.Items.Add("ファイルが見つかりませんでした。");
                return;
            }
            foreach (string file in filteredFiles)
            {
                logList.Items.Add(System.IO.Path.GetFileName(file));
            }
        }

        //コピーボタンのクリック処理
        private void Copy_Button_Click(object sender, RoutedEventArgs e)
        {
            if (LogListBox.Items.Count > 0)
            {
                // すべてのアイテムを文字列として結合
                string allItemsText = string.Join("\n", LogListBox.Items.Cast<ListBoxModel>().Select(item => item.Text));

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
                PlayAudio.play(log.AudioPath, image);
            }
            //再生中の音声がクリックした音声と同じ場合、再生を止める
            else if (PlayAudio.playingImage == image)
            {
                PlayAudio.ChangeToStartImage();
                PlayAudio.stop();
            }
            //再生中の音声がクリックした音声と違う場合、再生中の音声を止め、選択した音声を再生する
            else
            {
                PlayAudio.ChangeToStartImage();
                PlayAudio.ChangeToStopImage(image);
                PlayAudio.play(log.AudioPath, image);   //playメソッドの冒頭で再生中の音声を止めている
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
                                    Background = System.Windows.Media.Brushes.Yellow
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
                            listBoxModel.TextInlines = inlines;
                            //マッチ数表示
                            //search_enabled = true;
                            //Search_Enabled(search_enabled);

                        }
                        else
                        {
                            // 検索テキストがない場合は元のテキストをそのまま表示
                            listBoxModel.TextInlines = new List<Inline>
                            {
                                new Run { Text = listBoxModel.Memory }
                            };
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
    }

}
