using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HottoMotto
{
    //public class Filler_Removal
    //{
    //    /// <summary>
    //    /// フィラーを除去するexeを実行する関数（非同期）
    //    /// </summary>
    //    /// <param name="arg"></param>
    //    /// <returns></returns>
    //    static public async Task<string> removal(string arg)
    //    {
    //        try
    //        {
    //            // プロセスの設定
    //            var processStartInfo = new ProcessStartInfo
    //            {
    //                FileName = ".\\Filler_removal.dist\\Filler_removal.exe",    //実行するexe
    //                Arguments = arg,                    //引数
    //                RedirectStandardOutput = true, // 標準出力をリダイレクト
    //                RedirectStandardError = true, // 標準エラー出力をリダイレクト
    //                UseShellExecute = false,     // シェルを使用しない
    //                CreateNoWindow = true        // ウィンドウを作成しない
    //            };

    //            using (var process = new Process { StartInfo = processStartInfo })
    //            {
    //                var tcs = new TaskCompletionSource<bool>();

    //                //process.Exited += (sender, e) =>
    //                //{
    //                //    tcs.SetResult(true);
    //                //    process.Dispose();
    //                //};

    //                process.Start();
    //                // 標準出力を取得
    //                string output = await process.StandardOutput.ReadToEndAsync();
    //                string errorOutput = await process.StandardError.ReadToEndAsync();
    //                //プロセス終了を待機
    //                await process.WaitForExitAsync();
    //                // プロセス終了を待機
    //                //await tcs.Task;

    //                // 出力を表示（必要ならUIやログに表示）
    //                Debug.WriteLine("exe:" + output.Replace(Environment.NewLine, ""));
    //                if (errorOutput != "")
    //                {
    //                    Debug.WriteLine("exe error" + errorOutput);
    //                }

    //                return output.Replace(Environment.NewLine, "");


    //            }

    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Print("exe error" + e);
    //            return ""; // エラー時の戻り値
    //        }
    //    }
    //}

    public static class Filler_Removal
    {
        private static Process pythonProcess;
        private static StreamWriter pythonWriter;
        private static StreamReader pythonReader;

        // Pythonプロセスの初期化
        public static void Initialize()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ".\\Filler_removal.dist\\Filler_removal.exe", // Pythonの実行ファイル
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                pythonProcess = new Process { StartInfo = processStartInfo };
                pythonProcess.Start();

                pythonWriter = pythonProcess.StandardInput;
                pythonReader = pythonProcess.StandardOutput;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Pythonプロセスの初期化に失敗: {e}");
            }
        }

        // Pythonプロセスを終了
        public static void Terminate()
        {
            try
            {
                if (pythonProcess != null && !pythonProcess.HasExited)
                {
                    pythonWriter.Close();
                    pythonProcess.Kill();
                    pythonProcess.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Pythonプロセスの終了に失敗: {e}");
            }
        }

        // フィラー除去メソッド
        public static async Task<string> Removal(string arg)
        {
            try
            {
                if (pythonProcess == null || pythonProcess.HasExited)
                {
                    Debug.WriteLine("Pythonプロセスが起動していません。");
                    return string.Empty;
                }

                // Pythonプロセスに入力を送信
                await pythonWriter.WriteLineAsync(arg);
                await pythonWriter.FlushAsync();

                // Pythonプロセスから出力を受信
                string output = await pythonReader.ReadLineAsync();
                return output ?? string.Empty;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"フィラー除去中にエラー: {e}");
                return string.Empty;
            }
        }
    }
}
