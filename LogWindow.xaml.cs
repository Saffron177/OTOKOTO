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
using System.IO;
using System.Diagnostics;

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
            logFileCheck();
            logList.MouseDoubleClick += logList_DoubleClick;
        }

        //Logsファイルの中身を検索し、結果を出力する
        public void logFileCheck()
        {
            string relativePath = @"../../../../Logs"; // チェックしたいディレクトリのパス
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
                else //fileがないばあい
                {
                    logList.Items.Add("logファイルがありません");
                }
            }
            else //ディレクトリが存在しない場合
            {
                logList.Items.Add("指定したディレクトリが存在しません。");
            }
        }

        // listBox中身をダブルクリックしたときに実行する関数
        private void logList_DoubleClick(object sender, EventArgs e)
        {
            if (logList.SelectedItem != null)
            {
                string selectedFile = logList.SelectedItem.ToString();

                Debug.Print("selectedFile:" + selectedFile);

                //ログファイルの読み取りを実行
                Load_Log(selectedFile);
            }
        }

        //ログファイルの内容をListBoxに表示
        private void Load_Log(string selectedFile)
        {
            string filePath = "../../../../Logs/" + selectedFile;

            // ファイルが存在するかチェック
            if (!File.Exists(filePath))
            {
                Debug.Print("JSONファイルが見つかりません。");
                return;
            }

            // ファイルの内容を読み込む
            string jsonText = File.ReadAllText(filePath);

            try
            {
                // JSONをリストに変換（デシリアライズ）
                var logs = JsonSerializer.Deserialize<List<Conversation_Log_Data>>(jsonText);

                LogListBox.Items.Clear();
                // リストボックスにデータを追加
                foreach (var log in logs)
                {
                    LogListBox.Items.Add($"{log.TimeStamp}");
                    LogListBox.Items.Add($"{log.Text}");
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

    }
}
