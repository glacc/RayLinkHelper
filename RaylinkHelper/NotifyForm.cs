using System.Runtime.Versioning;

namespace RayLinkHelper
{
    /// <summary>
    /// 用于创建系统托盘图标的隐藏窗口。
    /// </summary>
    [SupportedOSPlatform("Windows")]
    class NotifyForm : Form
    {
        // 说明文本 //

        const string name = "RayLink Helper";

        const string toolTipTextStarted = $"{name}正在运行。\n左键托盘图标可启用／禁用，\n右键托盘图标可启用／禁用或退出。";

        const string toolTipTitleEnabled = $"{name}已启用";
        const string toolTipTitleDisabled = $"{name}已禁用";

        const string toolTipTextEnabled = $"焦点在RayLink窗口时快速移动鼠标即可最小化RayLink窗口。若其中的小窗被最小化成“RaysyncDesktop”，双击它即可恢复。";
        const string toolTipTextDisabled = $"{toolTipTitleDisabled}。";

        // 托盘图标和右键菜单选项 //

        NotifyIcon notifyIcon = new NotifyIcon();

        ToolStripMenuItem menuItemEnable = new ToolStripMenuItem("启用");
        ToolStripMenuItem menuItemExit = new ToolStripMenuItem("退出");
        ToolStripMenuItem menuItemAuthor = new ToolStripMenuItem("by Glacc");
        ToolStripMenuItem menuItemVersionDate = new ToolStripMenuItem("0.1.0 2025/4/5");

        ToolStripSeparator menuSeperator = new ToolStripSeparator();

        ContextMenuStrip menuStrip = new ContextMenuStrip();

        public EventHandler<bool>? onToggleEnable = null;

        /// <summary>
        /// 窗口载入完成时，隐藏窗口，并弹出正在运行的提示。
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);

            notifyIcon.ShowBalloonTip(5000, name, toolTipTextStarted, ToolTipIcon.Info);
        }

        /// <summary>
        /// 切换启用／禁用状态，并弹出说明。
        /// 若是从右键菜单禁用，则不会显示禁用时的提示。
        /// 
        /// 为了将启用／禁用状态传递出去而单独设置了一个onToggleEnable事件。
        /// </summary>
        void ToggleEnabled(object? sender = null, MouseEventArgs? args = null)
        {
            if (args != null)
            {
                if (args.Button != MouseButtons.Left)
                    return;
            }

            menuItemEnable.Checked = !menuItemEnable.Checked;

            if (menuItemEnable.Checked)
                notifyIcon.ShowBalloonTip(5000, toolTipTitleEnabled, toolTipTextEnabled, ToolTipIcon.Info);
            else if (args != null && args?.Button == MouseButtons.Left)
                notifyIcon.ShowBalloonTip(3000, toolTipTitleDisabled, toolTipTextDisabled, ToolTipIcon.Info);

            onToggleEnable?.Invoke(this, menuItemEnable.Checked);
        }

        public NotifyForm()
        {
            #region Form

            // 窗口关闭时的事件，取消显示托盘图标

            FormClosed += delegate { notifyIcon.Visible = false; };

            #endregion

            #region NotifyIcon

            // 设定托盘图标

            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = name;

            notifyIcon.MouseClick += ToggleEnabled;

            notifyIcon.ContextMenuStrip = menuStrip;

            notifyIcon.Visible = true;

            #endregion

            #region ContextMenuStrip_and_ToolStripMenuItems

            // 托盘图标的右键菜单

            menuItemEnable.Click += delegate { ToggleEnabled(); };

            menuItemExit.Click += delegate
            {
                notifyIcon.Visible = false;
                Close();
            };

            menuItemAuthor.Enabled = false;

            menuItemVersionDate.Enabled = false;

            menuStrip.Items.Add(menuItemEnable);
            menuStrip.Items.Add(menuItemExit);
            menuStrip.Items.Add(menuSeperator);
            menuStrip.Items.Add(menuItemAuthor);
            menuStrip.Items.Add(menuItemVersionDate);

            #endregion
        }
    }
}
