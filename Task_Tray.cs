using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace HottoMotto
{
    partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;

        /// <summary>
        /// タスクトレイを設定する関数
        /// </summary>
        private void SetupNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = new Icon("Resource/Icon.ico"), // アイコンファイルをプロジェクトに追加
                Visible = true,
                Text = "アプリケーション名"
            };

            // コンテキストメニューの設定
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("開く", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("終了", null, (s, e) => ExitApplication());

            notifyIcon.ContextMenuStrip = contextMenu;

            // アイコンをダブルクリックした際のイベント
            notifyIcon.DoubleClick += (s, e) => ShowWindow();


        }

        /// <summary>
        /// ウィンドウを再度表示
        /// </summary>
        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        /// <summary>
        /// アプリケーションを終了
        /// </summary>
        private void ExitApplication()
        {
            notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// バツを押した時に終了しないように上書き
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            e.Cancel = true; // 閉じる動作をキャンセル
            this.Hide(); // ウィンドウを非表示にする
        }
    }
}
