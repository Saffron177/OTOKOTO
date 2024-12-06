using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
                string[] txtFiles = Directory.GetFiles(directoryPath, "*.txt");

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

                // コマンドを実行
            }
        }
    }
}
