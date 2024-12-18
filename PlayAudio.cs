using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HottoMotto
{
    internal class PlayAudio
    {
        private AudioFileReader reader;
        private WaveOut waveOut;

        private bool isPlaying = false;
        public async Task play(string path)
        {
            stop();
            try
            {
                reader = new AudioFileReader(path);
                waveOut = new WaveOut();

                reader.Position = 0;
                waveOut.Init(reader);
                waveOut.Play();
                isPlaying = true;

                // 再生の終了を待つ
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(100); // 少し待機してループを制御
                }

            }
            catch (Exception ex)
            {
                Debug.Print("error:" + ex.Message);
            }
            finally
            {
                // 再生終了時にリソースを解放
                Cleanup();
            }
        }
        public async Task stop()
        {
            try
            {
                if (isPlaying && waveOut != null)
                {
                    waveOut.Stop();
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
                Cleanup();
            }
        }

        private void Cleanup()
        {
            waveOut?.Dispose();
            reader?.Dispose();
            waveOut = null;
            reader = null;
        }
    }
}
