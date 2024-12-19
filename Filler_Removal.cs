using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HottoMotto
{
    public class Filler_Removal
    {
        /// <summary>
        /// フィラーを除去するexeを実行する関数（非同期）
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        static public async Task<string> removal(string arg)
        {
            try
            {
                // プロセスの設定
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "Filler_removal.exe",    //実行するexe
                    Arguments = arg,                    //引数
                    RedirectStandardOutput = true, // 標準出力をリダイレクト
                    RedirectStandardError = true, // 標準エラー出力をリダイレクト
                    UseShellExecute = false,     // シェルを使用しない
                    CreateNoWindow = true        // ウィンドウを作成しない
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    var tcs = new TaskCompletionSource<bool>();

                    //process.Exited += (sender, e) =>
                    //{
                    //    tcs.SetResult(true);
                    //    process.Dispose();
                    //};

                    process.Start();
                    // 標準出力を取得
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    //プロセス終了を待機
                    process.WaitForExit();
                    // プロセス終了を待機
                    //await tcs.Task;

                    // 出力を表示（必要ならUIやログに表示）
                    Debug.WriteLine("exe:" + output.Replace(Environment.NewLine, ""));
                    if (errorOutput != null)
                    {
                        Debug.WriteLine("exe error" + errorOutput);
                    }

                    return output.Replace(Environment.NewLine, "");

                    
                }

            }
            catch (Exception e)
            {
                Debug.Print("exe error" + e);
                return ""; // エラー時の戻り値
            }
        }
    }
}
