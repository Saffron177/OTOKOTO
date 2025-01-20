using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Vosk;

namespace HottoMotto
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            string modelPath = "Models/vosk-model-ja-0.22";
            string modelFile = Path.Combine(modelPath, "am/final.mdl");

            //ダウンロードが必要な場合のみLoadingWindowを表示
            LoadingWindow loadingWindow = new LoadingWindow();
            if (!Directory.Exists(modelPath) || !File.Exists(modelFile))
            {
                // ダウンロードの確認
                var result = System.Windows.MessageBox.Show(
                    "音声認識に必要なモデルファイルがありません。\nダウンロードしますか？(約0.98GB)",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    loadingWindow.Show();
                    try
                    {
                        
                        await InitializeModelAsync(loadingWindow);
                    }
                    catch(Exception ex)
                    {
                        Debug.Print(ex.Message);
                    }
                    finally
                    {
                        loadingWindow.Close();
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "モデルファイルがないため、アプリケーションを終了します。再度起動するか手動でモデルを配置してください。",
                        "終了",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    System.Windows.Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                // モデル既に存在する場合はスルー
            }
            //メインウィンドウを表示
            
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async Task InitializeModelAsync(LoadingWindow loadingWindow)  // 引数を追加
        {
            Debug.Print("=== InitializeModelAsync started ===");

            try
            {
                string modelPath = "Models/vosk-model-ja-0.22";
                string modelFile = Path.Combine(modelPath, "am/final.mdl");

                Debug.Print($"Current Directory: {Directory.GetCurrentDirectory()}");
                Debug.Print($"Model Path (Full): {Path.GetFullPath(modelPath)}");
                Debug.Print($"Model File (Full): {Path.GetFullPath(modelFile)}");
                Debug.Print($"Model directory exists: {Directory.Exists(modelPath)}");
                Debug.Print($"Model file exists: {File.Exists(modelFile)}");

                if (!Directory.Exists(modelPath) || !File.Exists(modelFile))
                {
                    await ModelManager.DownloadAndExtractModel(loadingWindow);

                    // モデルファイルの存在を再確認
                    if (!File.Exists(modelFile))
                    {
                        throw new FileNotFoundException("モデルファイルが見つかりません。", modelFile);
                    }
                }

                Debug.Print("モデルパス: " + modelPath);
            }
            catch (Exception ex)
            {
                Debug.Print($"モデルの初期化でエラーが発生: {ex.Message}");
                throw;
            }
        }
    }

}
