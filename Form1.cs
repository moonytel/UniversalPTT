using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace PTTGui
{
    //── Configuration ────────────────────────────────────────────────
    public enum InputDevice { Keyboard = 0, Mouse = 1 }

    public class AppConfig
    {
        private const string Path = "config.json";
        public InputDevice DeviceType { get; set; } = InputDevice.Keyboard;
        public Keys HotKey { get; set; } = Keys.CapsLock;
        public MouseButtons HotButton { get; set; } = MouseButtons.Left;
        public bool StartMinimized { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public bool AutoMuteOnStart { get; set; } = true;

        public static AppConfig Load()
        {
            if (File.Exists(Path))
            {
                try { return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Path))!; }
                catch { }
            }
            return new AppConfig();
        }

        public void Save() =>
            File.WriteAllText(Path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    //── Main Form ───────────────────────────────────────────────────
    public class Form1 : Form
    {
        // Audio
        private readonly MMDeviceEnumerator _devEnum = new();
        private MMDevice _mic => _devEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
        private bool _isMuted = true;
        private bool _wasOriginallyMuted = false; // Track original state

        // Config & UI
        private AppConfig _cfg;
        private NotifyIcon _notifyIcon;
        private bool _isCapturing = false;
        private Label lblTitle, lblCurrent, lblStatus;
        private Button btnCapture;
        private CheckBox chkStartMin, chkNotify, chkAutoMute;
        private GroupBox grpSettings, grpStatus;
        private Panel pnlMain;

        // Win32 hooks
        private IntPtr _kbdHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private HookProc _kbdProc, _mouseProc;

        public Form1()
        {
            InitializeForm();
            _cfg = AppConfig.Load();
            StoreOriginalMicState();
            BuildUI();
            SetupNotifyIcon();
            ApplySettings();
            UpdateUI();
        }

        private void InitializeForm()
        {
            Text = "Universal Push-to-Talk";
            Size = new Size(450, 380);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(240, 240, 240);
            Font = new Font("Segoe UI", 9);
        }

        private void StoreOriginalMicState()
        {
            try
            {
                _wasOriginallyMuted = _mic.AudioEndpointVolume.Mute;
            }
            catch
            {
                _wasOriginallyMuted = false;
            }
        }

        private void BuildUI()
        {
            // Title
            lblTitle = new Label
            {
                Text = "🎤 Universal Push-to-Talk",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180)
            };

            // Status Group
            grpStatus = new GroupBox
            {
                Text = "Current Status",
                Location = new Point(20, 55),
                Size = new Size(390, 80),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblCurrent = new Label
            {
                Text = "PTT Trigger: None",
                Location = new Point(15, 25),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            lblStatus = new Label
            {
                Text = "🔇 Microphone: Muted",
                Location = new Point(15, 45),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 60, 60)
            };

            grpStatus.Controls.AddRange(new Control[] { lblCurrent, lblStatus });

            // Capture Button
            btnCapture = new Button
            {
                Text = "📝 Capture New Key/Button",
                Location = new Point(20, 150),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCapture.FlatAppearance.BorderSize = 0;
            btnCapture.Click += BtnCapture_Click;

            // Settings Group
            grpSettings = new GroupBox
            {
                Text = "Settings",
                Location = new Point(20, 210),
                Size = new Size(390, 120),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            chkStartMin = new CheckBox
            {
                Text = "🗕 Start minimized to system tray",
                Location = new Point(15, 25),
                Size = new Size(250, 25),
                Checked = _cfg.StartMinimized,
                Font = new Font("Segoe UI", 9)
            };

            chkNotify = new CheckBox
            {
                Text = "🔔 Show notifications",
                Location = new Point(15, 50),
                Size = new Size(250, 25),
                Checked = _cfg.ShowNotifications,
                Font = new Font("Segoe UI", 9)
            };

            chkAutoMute = new CheckBox
            {
                Text = "🔇 Auto-mute microphone on start",
                Location = new Point(15, 75),
                Size = new Size(250, 25),
                Checked = _cfg.AutoMuteOnStart,
                Font = new Font("Segoe UI", 9)
            };

            grpSettings.Controls.AddRange(new Control[] { chkStartMin, chkNotify, chkAutoMute });

            // Add help text
            var lblHelp = new Label
            {
                Text = "💡 Tip: Click 'Capture New Key/Button' then press any key or mouse button to set as your PTT trigger.",
                Location = new Point(250, 150),
                Size = new Size(160, 60),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.TopLeft
            };

            Controls.AddRange(new Control[] { lblTitle, grpStatus, btnCapture, grpSettings, lblHelp });
        }

        private void BtnCapture_Click(object sender, EventArgs e)
        {
            _isCapturing = true;
            btnCapture.Text = "⏳ Press any key or mouse button...";
            btnCapture.BackColor = Color.FromArgb(255, 165, 0);
        }

        private void ApplySettings()
        {
            if (_cfg.StartMinimized)
            {
                WindowState = FormWindowState.Minimized;
                Hide();
            }
            if (_cfg.AutoMuteOnStart)
            {
                SetMicMute(true);
            }
            else
            {
                // If not auto-muting, restore original state
                SetMicMute(_wasOriginallyMuted);
            }
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Universal Push-to-Talk - Right click for options",
                Visible = true
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("📖 Show Application", null, (_, __) => { Show(); WindowState = FormWindowState.Normal; Activate(); });
            menu.Items.Add("-"); // Separator
            menu.Items.Add("❌ Exit", null, (_, __) =>
            {
                if (MessageBox.Show("Are you sure you want to exit?\n\nThe microphone will be unmuted when you exit.",
                    "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            });

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, __) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
        }

        private void UpdateUI()
        {
            string device = _cfg.DeviceType == InputDevice.Keyboard ? "Keyboard" : "Mouse";
            string trigger = _cfg.DeviceType == InputDevice.Keyboard
                ? _cfg.HotKey.ToString()
                : _cfg.HotButton.ToString();

            lblCurrent.Text = $"⌨️ PTT Trigger: {device} → {trigger}";

            // Update status
            if (_isMuted)
            {
                lblStatus.Text = "🔇 Microphone: Muted (Press PTT to talk)";
                lblStatus.ForeColor = Color.FromArgb(200, 60, 60);
            }
            else
            {
                lblStatus.Text = "🎤 Microphone: Active (Release PTT to mute)";
                lblStatus.ForeColor = Color.FromArgb(60, 200, 60);
            }
        }

        private void FinishCapture()
        {
            _isCapturing = false;
            btnCapture.Text = "📝 Capture New Key/Button";
            btnCapture.BackColor = Color.FromArgb(70, 130, 180);

            _cfg.StartMinimized = chkStartMin.Checked;
            _cfg.ShowNotifications = chkNotify.Checked;
            _cfg.AutoMuteOnStart = chkAutoMute.Checked;
            _cfg.Save();
            UpdateUI();

            if (_cfg.ShowNotifications)
            {
                string device = _cfg.DeviceType == InputDevice.Keyboard ? "Keyboard" : "Mouse";
                string trigger = _cfg.DeviceType == InputDevice.Keyboard
                    ? _cfg.HotKey.ToString()
                    : _cfg.HotButton.ToString();

                _notifyIcon.ShowBalloonTip(2000, "✅ PTT Updated",
                    $"Now using {device}: {trigger}",
                    ToolTipIcon.Info);
            }
        }

        private void SetMicMute(bool mute)
        {
            try
            {
                _mic.AudioEndpointVolume.Mute = mute;
                _isMuted = mute;
                UpdateUI();

                if (_cfg.ShowNotifications && !mute)
                {
                    _notifyIcon.ShowBalloonTip(500, "🎤 PTT Active", "Microphone unmuted", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling microphone: {ex.Message}", "Audio Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(_cfg.StartMinimized ? false : value);
        }

        //──────────────────────────────────────────────────
        // Hook installation & callbacks
        //──────────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Keep delegates alive
            _kbdProc = KeyboardHookProc;
            _mouseProc = MouseHookProc;
            var mod = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
            _kbdHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbdProc, mod, 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, mod, 0);
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var vk = Marshal.ReadInt32(lParam);
                bool isDown = (wParam == (IntPtr)0x0100); // WM_KEYDOWN

                if (_isCapturing)
                {
                    _cfg.DeviceType = InputDevice.Keyboard;
                    _cfg.HotKey = (Keys)vk;
                    Invoke(new Action(FinishCapture));
                }
                else if (_cfg.DeviceType == InputDevice.Keyboard && (int)_cfg.HotKey == vk)
                {
                    SetMicMute(!isDown);
                }
            }
            return CallNextHookEx(_kbdHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                // 0x201 = WM_LBUTTONDOWN, 0x202 = WM_LBUTTONUP
                // 0x204 = WM_RBUTTONDOWN, 0x205 = WM_RBUTTONUP
                MouseButtons btn =
                    msg is 0x201 or 0x202 ? MouseButtons.Left :
                    msg is 0x204 or 0x205 ? MouseButtons.Right :
                    msg is 0x20B or 0x20C ? MouseButtons.XButton1 :
                    msg is 0x20D or 0x20E ? MouseButtons.XButton2 :
                    MouseButtons.None;

                bool isDown = msg == 0x201 || msg == 0x204 || msg == 0x20B || msg == 0x20D;

                if (_isCapturing && btn != MouseButtons.None)
                {
                    _cfg.DeviceType = InputDevice.Mouse;
                    _cfg.HotButton = btn;
                    Invoke(new Action(FinishCapture));
                }
                else if (_cfg.DeviceType == InputDevice.Mouse && btn == _cfg.HotButton)
                {
                    SetMicMute(!isDown);
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Always unmute microphone when closing unless it was originally muted
            try
            {
                if (!_wasOriginallyMuted)
                {
                    _mic.AudioEndpointVolume.Mute = false;
                    if (_cfg.ShowNotifications)
                    {
                        _notifyIcon.ShowBalloonTip(1000, "🎤 Microphone Restored",
                            "Microphone has been unmuted", ToolTipIcon.Info);
                        System.Threading.Thread.Sleep(1000); // Give time for notification
                    }
                }
            }
            catch { }

            // Clean up hooks and resources
            if (_kbdHook != IntPtr.Zero)
                UnhookWindowsHookEx(_kbdHook);
            if (_mouseHook != IntPtr.Zero)
                UnhookWindowsHookEx(_mouseHook);

            _notifyIcon?.Dispose();
            base.OnFormClosing(e);
        }

        // Handle minimize to tray
        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                if (_cfg.ShowNotifications)
                {
                    _notifyIcon.ShowBalloonTip(1000, "Push-to-Talk Minimized",
                        "Application is running in the system tray", ToolTipIcon.Info);
                }
            }
            base.OnResize(e);
        }
    }
}