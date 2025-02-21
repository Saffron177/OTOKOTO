﻿using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace HottoMotto
{
    internal class PlayAudio
    {
        public static AudioFileReader reader;
        private static WaveOut waveOut;

        public static System.Windows.Controls.Image? playingImage;

        private static bool isPlaying = false;
        public static async Task play(string path, System.Windows.Controls.Image image, bool isMainWindow)
        {
            stop(isMainWindow);
            try
            {
                reader = new AudioFileReader(path);
                waveOut = new WaveOut();

                reader.Position = 0;
                waveOut.Init(reader);
                waveOut.Play();
                isPlaying = true;

                if (!isMainWindow)
                {
                    // 再生バーを設定
                    LogWindow.seekBar.Maximum = reader.TotalTime.TotalSeconds;
                    LogWindow.totalTime.Text = reader.TotalTime.ToString(@"mm\:ss");
                    //タイマー開始
                    LogWindow.timer.Start();
                }

                // 再生の終了を待つ
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(100); // 少し待機してループを制御
                }

                //再生停止する際にこの音声がまだ再生中の場合は画像を戻す
                if(playingImage == image)
                {
                    ChangeToStartImage();
                }
            }
            catch (Exception ex)
            {
                Debug.Print("error:" + ex.Message);
            }
            finally
            {
                // 再生終了時にリソースを解放
                //Cleanup();
            }
        }
        public static async Task stop(bool isMainWindow)
        {
            try
            {
                if (isPlaying && waveOut != null)
                {
                    waveOut.Stop();
                    reader.Position = 0;

                    if (!isMainWindow)
                    {
                        //タイマー停止
                        LogWindow.timer.Stop();
                        // 再生バーをリセット
                        LogWindow.seekBar.Value = 0;
                        LogWindow.currentTime.Text = "00:00";
                    }
                }
                isPlaying = false;
            }
            catch(Exception ex)
            {
                Debug.Print("error:" + ex.Message);
            }
            finally
            {
                // 再生終了時にリソースを解放
                //Cleanup();
            }
        }

        public static void Cleanup()
        {
            waveOut?.Dispose();
            reader?.Dispose();
            waveOut = null;
            reader = null;
        }

        public static void ChangeToStartImage()
        {
            if (playingImage != null)
            {
                playingImage.Source = new BitmapImage(new Uri("Resource/start.png", UriKind.Relative));
            }
            playingImage = null;
        }

        public static void ChangeToStopImage(System.Windows.Controls.Image image)
        {
            playingImage = image;
            playingImage.Source = new BitmapImage(new Uri("Resource/stop.png", UriKind.Relative));
        }
    }
}
