using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace HottoMotto
{
    partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;                //タスクトレイアイコン
        private ToolStripMenuItem menu_capture_click_button; //タスクトレイの録音開始・停止ボタン
        private ToolStripLabel menu_status;                //タスクトレイのステータスラベル
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
            var contextMenu = new ContextMenuStrip()
            {
                ShowImageMargin = false,
            };
            menu_status = new ToolStripLabel()
            {
                Text = "Welcome",
                Font = new Font("Yu Gothic UI", 12),
            };

            menu_capture_click_button = new ToolStripMenuItem()
            {
                Text = "録音開始",
                Image = null,
            };

            menu_capture_click_button.Click += (s, e) => 
            {
                RoutedEventArgs dummy_Event = new RoutedEventArgs();
                Button_Capture_Click(s,dummy_Event);
            };

            contextMenu.Items.Add(menu_status);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menu_capture_click_button);
            contextMenu.Items.Add("開く", null, (s, e) => ShowWindow());
            contextMenu.Items.Add(new ToolStripSeparator());
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

        private void Task_Tray_icon_change()
        {

        }
    }
}
