using HottoMotto;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Net.Http;

public class ModelManager
{
    public static async Task DownloadAndExtractModel(LoadingWindow loadingWindow)
    {
        try
        {
            string modelPath = "Models/vosk-model-ja-0.22";
            if (!Directory.Exists(modelPath))
            {
                Debug.Print("モデルファイルが見つかりません。ダウンロードを開始します...");
                loadingWindow.UpdateProgress("ダウンロードを開始します...");

                Directory.CreateDirectory("Models");

                string modelUrl = "https://drive.google.com/file/d/1io1e_jCIQg8ZM9nTEJz9wXuf-QAtL-qN/view?usp=drive_link";
                string zipPath = "Models/vosk-model-ja-0.22.zip";

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);
                    var contentLength = response.Content.Headers.ContentLength ?? 0;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(zipPath))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            var percentage = (int)((double)totalBytesRead / contentLength * 100);
                            loadingWindow.UpdateProgress($"ダウンロード中... {percentage}%");
                            Debug.Print($"ダウンロード進行状況: {percentage}%");
                        }
                    }
                }

                loadingWindow.UpdateProgress("ファイルを解凍中...");
                Debug.Print("ダウンロード完了。解凍を開始します...");

                ZipFile.ExtractToDirectory(zipPath, "Models");

                File.Delete(zipPath);

                loadingWindow.UpdateProgress("モデルファイルの準備が完了しました");
                Debug.Print("モデルファイルの準備が完了しました。");
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"モデルファイルのダウンロードまたは解凍中にエラーが発生しました: {ex.Message}");
            throw;
        }
    }
}