using System;
using System.Collections.Generic;
using System.Linq;
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

namespace HottoMotto
{
    /// <summary>
    /// LogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
            Load_Log();

        }

        private void Load_Log()
        {
            // ファイルパスを指定
            string filePath = "../../../../Logs/log1.txt";

            // ファイルが存在するかチェック
            if (!File.Exists(filePath))
            {
                Console.WriteLine("JSONファイルが見つかりません。");
                return;
            }

            // ファイルの内容を読み込む
            string jsonText = File.ReadAllText(filePath);

            try
            {
                // JSONをリストに変換（デシリアライズ）
                var logs = JsonSerializer.Deserialize<List<Conversation_Log_Data>>(jsonText);

                // リストボックスにデータを追加
                foreach (var log in logs)
                {
                    LogListBox.Items.Add($"{log.TimeStamp}");
                    LogListBox.Items.Add($"{log.Text}");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSONの解析に失敗しました: {ex.Message}");
            }
        }
    }
}
