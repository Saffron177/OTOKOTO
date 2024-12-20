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

namespace HottoMotto
{
    /// <summary>
    /// LogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LogWindow : Window
    {
        //ファイル一覧を保持
        string[] txtFiles;

        public LogWindow()
        {
            InitializeComponent();
            LogFileCheck();
            logList.MouseDoubleClick += LogList_DoubleClick;
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
            Copy_Button.Content = "コピー";
            Copy_Button.Visibility = Visibility.Visible;

            try
            {
                // JSONをリストに変換（デシリアライズ）
                var logs = JsonSerializer.Deserialize<List<Conversation_Log_Data>>(jsonText);

                LogListBox.Items.Clear();
                // リストボックスにデータを追加
                foreach (var log in logs)
                {
                    LogListBox.Items.Add(new ListBoxModel { Text = (log.TimeStamp + (log.IsSpeaker ? "(スピーカー)" : "(マイク)")), IsHighlighted = false , IsSpeaker = log.IsSpeaker});
                    LogListBox.Items.Add(new ListBoxModel { Text = log.Text, IsHighlighted = true , IsSpeaker = log.IsSpeaker, AudioPath = log.AudioPath });
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
                Copy_Button.Content = "コピーしました";
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
            //再生中の音声がない場合、再生する
            if(PlayAudio.playingImage == null)
            {
                PlayAudio.ChangeToStopImage(image);
                PlayAudio.play(log.AudioPath, image);
            }
            //再生中の音声がクリックした音声と同じ場合、再生を止める
            else if(PlayAudio.playingImage == image)
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

        //検索ボックスのテキストと一致するテキストを抽出して背景色を変更する
        private async void HighlightText()
        {
            // TextBoxのテキストを取得
            string searchText = log_Search_Textbox.Text;

            // 検索テキストが空でないことを確認
            if (!string.IsNullOrEmpty(searchText))
            {
                foreach (var item in LogListBox.Items)
                {
                    ListBoxItem listBoxItem = null;

                    // アイテムコンテナが生成されるのを待つ
                    while (listBoxItem == null)
                    {
                        await System.Threading.Tasks.Task.Delay(10); // 少し待機
                        listBoxItem = LogListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                    }

                    // アイテムコンテナが生成されたかを確認
                    if (LogListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        listBoxItem = LogListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                        if (listBoxItem != null)
                        {
                            ListBoxModel listBoxModel = listBoxItem.Content as ListBoxModel;
                            string itemText = listBoxModel?.Text.ToLower();
                            if (itemText.Contains(searchText.ToLower()) && !string.IsNullOrWhiteSpace(searchText))
                            {
                                // ハイライトの適用
                                HighlightTextBlock(listBoxItem, listBoxModel, searchText);
                                listBoxModel.IsSearch = true;
                            }
                            else
                            {
                                listBoxModel.IsSearch = false;
                                TextBlock textBlock = FindVisualChild<TextBlock>(listBoxItem);
                                if (textBlock != null)
                                {
                                    textBlock.Inlines.Clear();
                                    textBlock.Inlines.Add(new Run(listBoxModel?.Text ?? string.Empty)); // 元のテキストを追加
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
                        // アイテムコンテナが生成されるのを待つ
                        LogListBox.ItemContainerGenerator.StatusChanged += (s, args) =>
                        {
                            if (LogListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                            {
                                // 再度ハイライト処理を呼び出す
                                HighlightText();
                            }
                        };
                    }
                }
            }
        }

        //一致したテキストが含まれるアイテムを再配置
        private void HighlightTextBlock(ListBoxItem listBoxItem, ListBoxModel listBoxModel, string searchText)
        {
            // テキストを分割し、検索テキストをハイライト
            int index = listBoxModel.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                string beforeMatch = listBoxModel.Text.Substring(0, index);
                string match = listBoxModel.Text.Substring(index, searchText.Length);
                string afterMatch = listBoxModel.Text.Substring(index + searchText.Length);

                listBoxModel.BeforText = beforeMatch;
                listBoxModel.MatchText = match;
                listBoxModel.AfterText = afterMatch;

                //背景色をリセット
                listBoxItem.Background = listBoxModel.Background;

                // ハイライト適用
                listBoxModel.IsHighlighted = true;
            }
            else
            {
                // ハイライト対象でない場合は元のテキストを表示
                TextBlock textBlock = FindVisualChild<TextBlock>(listBoxItem);
                if (textBlock != null)
                {
                    textBlock.Inlines.Clear();
                    textBlock.Inlines.Add(new Run(listBoxModel.Text));
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
    }
}
