using HottoMotto;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class ModelManager
{
    public static async Task DownloadAndExtractModel(LoadingWindow loadingWindow)
    {
        Debug.Print("=== ModelManager.DownloadAndExtractModel started ===");

        try
        {
            string modelPath = "Models/vosk-model-ja-0.22";
            string modelFile = Path.Combine(modelPath, "am/final.mdl");

            // デバッグ
            Debug.Print($"Current Directory: {Directory.GetCurrentDirectory()}");
            Debug.Print($"Model Path (Full): {Path.GetFullPath(modelPath)}");
            Debug.Print($"Model File (Full): {Path.GetFullPath(modelFile)}");
            Debug.Print($"Model directory exists: {Directory.Exists(modelPath)}");
            Debug.Print($"Model file exists: {File.Exists(modelFile)}");

            // モデルディレクトリが存在しないか、必要なモデルファイルが存在しない場合
            if (!Directory.Exists(modelPath) || !File.Exists(modelFile))
            {
                Debug.Print("Download condition met - starting download process");
                loadingWindow.UpdateProgress("ダウンロードを開始します...");

                //既にディレクトリがある場合は削除
                if (Directory.Exists(modelPath))
                {
                    Directory.Delete(modelPath, true);
                    Debug.Print($"Deleted existing directory: {modelPath}");
                }

                // Modelsディレクトリが存在しない場合は作成
                if (!Directory.Exists("Models"))
                {
                    Directory.CreateDirectory("Models");
                    Debug.Print("Created Models directory");
                }

                string modelUrl = "https://dl.dropboxusercontent.com/scl/fi/dttadn18vs9spmcmu2mj4/vosk-model-ja-0.22.zip?rlkey=8p1lvgbbqohofyiva3taxjfdr";
                string zipPath = Path.Combine(Path.GetTempPath(), $"vosk-model-ja-0.22_{DateTime.Now:yyyyMMddHHmmss}.zip");
                string targetPath = Path.Combine("Models", "vosk-model-ja-0.22");

                Debug.Print($"Using temporary zip path: {zipPath}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(30);
                    Debug.Print($"Starting download from: {modelUrl}");

                    try
                    {
                        using (var response = await client.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            Debug.Print("HTTP response received successfully");

                            var contentLength = response.Content.Headers.ContentLength ?? 0;
                            Debug.Print($"Content length: {contentLength} bytes");

                            // 一時フォルダにダウンロード
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                var buffer = new byte[81920];
                                long totalBytesRead = 0;
                                int bytesRead;

                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesRead += bytesRead;

                                    var percentage = (int)((double)totalBytesRead / contentLength * 100);
                                    loadingWindow.UpdateProgress($"ダウンロード中... {percentage}%");
                                    Debug.Print($"Download progress: {percentage}%");
                                }
                                await fileStream.FlushAsync();
                            }

                            //一時ファイルが正しくダウンロードされたことを確認
                            var fileInfo = new FileInfo(zipPath);
                            if (!fileInfo.Exists || fileInfo.Length != contentLength)
                            {
                                throw new IOException("ダウンロードが不完全です。");
                            }

                            loadingWindow.UpdateProgress("ファイルを解凍中...");
                            Debug.Print("Download completed. Starting extraction...");

                            // 解凍先のディレクトリを準備
                            if (Directory.Exists(targetPath))
                            {
                                Directory.Delete(targetPath, true);
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                            // 解凍
                            ZipFile.ExtractToDirectory(zipPath, "Models");
                            Debug.Print("Extraction completed");

                            // 後処理：一時ファイルの削除
                            try
                            {
                                if (File.Exists(zipPath))
                                {
                                    File.Delete(zipPath);
                                    Debug.Print("Temporary zip file deleted successfully");
                                }
                            }
                            catch (IOException ex)
                            {
                                Debug.Print($"Warning: Failed to delete temporary file: {ex.Message}");
                                // 一時ファイルの削除失敗は致命的エラーとしない
                            }

                            // モデルファイルの存在を確認
                            if (!File.Exists(modelFile))
                            {
                                throw new FileNotFoundException("モデルファイルの展開に失敗しました。", modelFile);
                            }

                            loadingWindow.UpdateProgress("モデルファイルの準備が完了しました");
                            Debug.Print("Model preparation completed successfully");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Debug.Print($"HTTP Request Error: {ex.Message}");
                        throw new Exception($"モデルのダウンロードに失敗しました: {ex.Message}", ex);
                    }
                    catch (IOException ex)
                    {
                        Debug.Print($"IO Error during download or extraction: {ex.Message}");
                        throw new Exception($"ファイル操作中にエラーが発生しました: {ex.Message}", ex);
                    }
                }
            }
            else
            {
                Debug.Print("Existing model file found - skipping download");
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"=== Error in ModelManager.DownloadAndExtractModel ===");
            Debug.Print($"Error type: {ex.GetType().Name}");
            Debug.Print($"Error message: {ex.Message}");
            Debug.Print($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            Debug.Print("=== ModelManager.DownloadAndExtractModel completed ===");
        }
    }
}
