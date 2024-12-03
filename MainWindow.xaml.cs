using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;

namespace HottoMotto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //WASAPIループバック録音用のオブジェクト
        private WasapiLoopbackCapture capture;
        //録音データをWAV形式で保存するためのオブジェクト
        private WaveFileWriter writer;
        public MainWindow()
        {
            InitializeComponent();
            LoadAudioDevices();
            LoadMicDevices();
            Debug.Print("init");
        }

        //オーディオデバイスを取得する関数
        private void LoadAudioDevices()
        {
            ComboBox_AudioDevices.Items.Clear();
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                ComboBox_AudioDevices.Items.Add(device.FriendlyName);
            }

            if (ComboBox_AudioDevices.Items.Count > 0)
            {
                ComboBox_AudioDevices.SelectedIndex = 0; // 最初のデバイスを選択
            }
        }
        //マイクデバイスを取得する関数
        private void LoadMicDevices()
        {
            ComboBox_MicDevices.Items.Clear();
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture,DeviceState.Active);

            foreach (var device in devices)
            {
                ComboBox_MicDevices.Items.Add(device.FriendlyName);
            }

            if(ComboBox_MicDevices.Items.Count > 0) 
            { 
                ComboBox_MicDevices.SelectedIndex = 0;
            }
        }


        private void Button_capture_start_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_AudioDevices.SelectedIndex == -1)
            {
                MessageBox.Show("デバイスを選択してください");
                return;
            }

            // 選択されたデバイスを取得
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var selectedDevice = devices[ComboBox_AudioDevices.SelectedIndex];
            Debug.Print(selectedDevice.ID);

            try
            {
                //出力先パス
                string outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "recorded_audio.wav");
                //オブジェクト生成
                capture = new WasapiLoopbackCapture(selectedDevice);
                writer = new WaveFileWriter(outputPath, capture.WaveFormat);

                //capture.DataAvailable: 録音データが利用可能になるたびに発生するイベント
                //a.Buffer: 録音データが格納されたバッファ。
                //writer.Write: 録音データをWAVファイルに書き込む。
                capture.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                    //Debug.Print("DataAvailable");
                };

                /*RecordingStopped: 録音が停止したときに発生するイベント。
                  writer.Dispose(): ファイルを書き込み終了し、リソースを解放。
                  capture.Dispose(): 録音用オブジェクトを解放。
                */
                capture.RecordingStopped += (s, a) =>
                {
                    writer?.Dispose();
                    writer = null;
                    capture.Dispose();
                };

                //録音を開始
                capture.StartRecording();
                Label_status.Content = "録音中...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラー: {ex.Message}");
            }
        }
        private void Button_capture_stop_Click(object sender, RoutedEventArgs e)
        {
            if (capture != null)
            {
                //録音を停止
                capture.StopRecording();
                Label_status.Content = "録音停止";
            }
        }
    }
}