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
        public async Task play(string path)
        {
            try
            {
                using (var reader = new AudioFileReader(path))
                using (var waveOut = new WaveOut())
                {
                    reader.Position = 0;
                    waveOut.Init(reader);
                    waveOut.Play();

                    // 再生の終了を待つ
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        await Task.Delay(100); // 少し待機してループを制御
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("error:" + ex.Message);
            }
        }
    }
}
