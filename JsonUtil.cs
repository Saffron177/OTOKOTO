﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace HottoMotto
{
    /// <summary>
    /// 会話ログのデータ用クラス
    /// </summary>
    public class Conversation_Log_Data
    {
        public DateTime TimeStamp { get; set; }
        public string Text { get; set; }
        public bool IsSpeaker { get; set; }
        public string AudioPath {  get; set; }
    }
    
    /// <summary>
    /// Json処理用のクラス
    /// </summary>
    public class JsonUtil
    {
        /// <summary>
        /// Jsonを返す関数
        /// </summary>
        /// <param name="text"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public string ToJson(DateTime timestamp, string text,bool is_speaker,string audiopath)
        {
            var log = new Conversation_Log_Data
            {
                TimeStamp = timestamp,
                Text = text,
                IsSpeaker = is_speaker,
                AudioPath = audiopath
            };

            string jsonstring = JsonSerializer.Serialize<Conversation_Log_Data>(log);
            Debug.Print(jsonstring);
            Debug.Print(log.Text);
            return jsonstring;
        }
    }
}