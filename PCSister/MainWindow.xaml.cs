using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PCSister.Config;
using PCSister.Core;

namespace PCSister
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        public struct RECT { public int Left, Top, Right, Bottom; }
        public struct POINT { public int X; public int Y; }

        private PetAnimator animator;
        private DispatcherTimer gameTimer;
        private PerformanceCounter? cpuCounter;

        // Явно используем NotifyIcon из Forms
        private System.Windows.Forms.NotifyIcon? trayIcon;

        private string currentState = Settings.StateIdle;
        private int currentFrame = 0;
        private int frameTimer = 0;
        private int direction = 1;

        private double petX = 0;
        private double petY = 0;
        private double floorY = 0;
        private double targetX = 0;

        private int wanderTimer = 0;
        private int wanderInterval = 90;
        private int stateTimer = 0;
        private int textTimer = 0;
        private int systemCheckTimer = 0;

        private bool isFocusMode = false;
        private string lastClipboard = "";

        private string idleVersion = Settings.StateIdle;
        private int idleVersionTimer = 0;
        private const int IdleVersionSwitchTime = 300;

        private Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            animator = new PetAnimator();

            try { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { cpuCounter = null; }

            gameTimer = new DispatcherTimer(DispatcherPriority.Render);
            gameTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / Settings.FPS);
            gameTimer.Tick += GameLoop;

            SetupTrayIcon();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Растягиваем на все мониторы
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;

            petX = -SystemParameters.VirtualScreenLeft + (SystemParameters.PrimaryScreenWidth / 2);

            double petHeight = Settings.SpriteSize * Settings.Scale;

            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (taskbarHwnd != IntPtr.Zero && GetWindowRect(taskbarHwnd, out RECT taskbarRect))
            {
                floorY = (taskbarRect.Top - SystemParameters.VirtualScreenTop) - petHeight + 105;
            }
            else
            {
                floorY = (SystemParameters.PrimaryScreenHeight - SystemParameters.VirtualScreenTop) - petHeight;
            }

            petY = floorY;
            targetX = petX;

            gameTimer.Start();
        }

        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();

            try
            {
                // Явно указываем System.Drawing.Icon
                trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            trayIcon.Text = "PCSister";
            trayIcon.Visible = true;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var focusItem = new System.Windows.Forms.ToolStripMenuItem("Режим фокусу: ВИМК");
            focusItem.Click += (s, e) =>
            {
                isFocusMode = !isFocusMode;
                focusItem.Text = isFocusMode ? "Режим фокусу: УВІМК" : "Режим фокусу: ВИМК";
                ShowText(isFocusMode ? "Режим фокусу активовано!" : "Режим фокусу вимкнено.", 5);
            };
            contextMenu.Items.Add(focusItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Вихід");
            // Явно указываем System.Windows.Application
            exitItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            contextMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnClosed(e);
        }

        private void GameLoop(object sender, EventArgs e)
        {
            UpdateLogic();
            UpdateAnimation();
            UpdateSystemChecks();
            Render();
        }

        private void UpdateLogic()
        {
            if (petY < floorY) petY += 6;
            if (petY > floorY) petY = floorY;

            if (currentState == Settings.StateHurt || currentState == Settings.StateDead || currentState == Settings.StateJump)
            {
                stateTimer--;
                if (stateTimer <= 0) SetState(Settings.StateIdle);
            }
            else if (currentState == Settings.StateIdle || currentState == Settings.StateIdle2)
            {
                idleVersionTimer++;
                if (idleVersionTimer >= IdleVersionSwitchTime)
                {
                    idleVersionTimer = 0;
                    idleVersion = (idleVersion == Settings.StateIdle) ? Settings.StateIdle2 : Settings.StateIdle;
                    currentState = idleVersion;
                    currentFrame = 0;
                    frameTimer = 0;
                }

                if (rnd.NextDouble() < 0.002)
                {
                    SetState(Settings.StateJump);
                    stateTimer = Settings.FrameCounts[Settings.StateJump] * Settings.AnimDelayFast;
                    petY -= 120;
                }
                else
                {
                    wanderTimer++;
                    if (wanderTimer >= wanderInterval)
                    {
                        wanderTimer = 0;
                        wanderInterval = rnd.Next(90, 250);
                        targetX = rnd.Next(50, (int)this.Width - (Settings.SpriteSize * Settings.Scale) - 50);

                        if (rnd.NextDouble() < 0.1) SetState(Settings.StateRun);
                        else SetState(Settings.StateWalk);
                    }
                }
            }

            if (currentState == Settings.StateWalk || currentState == Settings.StateRun)
            {
                double speed = currentState == Settings.StateRun ? Settings.SPEED_RUN : Settings.SPEED_WALK;
                double dx = targetX - petX;

                if (Math.Abs(dx) > speed)
                {
                    direction = dx > 0 ? 1 : -1;
                    petX += speed * direction;
                }
                else
                {
                    petX = targetX;
                    if (currentState == Settings.StateRun && rnd.NextDouble() < 0.5)
                    {
                        SetState(Settings.StateDead);
                        stateTimer = Settings.FPS * 3;
                        ShowText("Ой... проклята гравітація...", 4);
                    }
                    else
                    {
                        SetState(Settings.StateIdle);
                    }
                }
            }

            if (petX < 0) petX = 0;
            if (petX > this.Width - (Settings.SpriteSize * Settings.Scale))
                petX = this.Width - (Settings.SpriteSize * Settings.Scale);
        }

        private void UpdateAnimation()
        {
            frameTimer++;
            int animDelay = (currentState == Settings.StateRun || currentState == Settings.StateJump) ? Settings.AnimDelayFast : Settings.AnimDelaySlow;
            animDelay = Math.Max(1, animDelay);

            if (frameTimer >= animDelay)
            {
                frameTimer = 0;
                int maxFrames = Settings.FrameCounts[currentState];

                if (currentState == Settings.StateDead && currentFrame == maxFrames - 1) { }
                else currentFrame = (currentFrame + 1) % maxFrames;
            }
        }

        private void UpdateSystemChecks()
        {
            systemCheckTimer++;
            if (systemCheckTimer >= Settings.FPS)
            {
                systemCheckTimer = 0;

                if (cpuCounter != null)
                {
                    float cpuUsage = cpuCounter.NextValue();
                    if (cpuUsage > 85 && textTimer <= 0)
                    {
                        SetState(Settings.StateRun);
                        targetX = rnd.Next(50, (int)this.Width - 50);
                        ShowText($"Викид! Процесор плавиться ({Math.Round(cpuUsage)}%)!", 5);
                    }
                }

                // Явно указываем System.Windows.Clipboard
                if (System.Windows.Clipboard.ContainsText())
                {
                    string cb = System.Windows.Clipboard.GetText().ToLower();
                    if (cb != lastClipboard && !string.IsNullOrWhiteSpace(cb))
                    {
                        lastClipboard = cb;
                        foreach (var kvp in Settings.ClipboardReactions)
                        {
                            if (cb.Contains(kvp.Key))
                            {
                                ShowText(kvp.Value, 5);
                                break;
                            }
                        }
                    }
                }

                if (isFocusMode)
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        StringBuilder sb = new StringBuilder(256);
                        GetWindowText(hwnd, sb, 256);
                        string title = sb.ToString().ToLower();

                        foreach (var app in Settings.DistractionApps)
                        {
                            if (title.Contains(app))
                            {
                                SetState(Settings.StateHurt);
                                stateTimer = 25;
                                ShowText($"Ану закрий {app.ToUpper()}! Працювати треба!", 5);
                                break;
                            }
                        }
                    }
                }
            }

            if (textTimer > 0)
            {
                textTimer--;
                if (textTimer <= 0) SpeechBubble.Visibility = Visibility.Hidden;
            }
        }

        private void Render()
        {
            if (animator.Sprites.ContainsKey(currentState))
            {
                PetImage.Source = animator.Sprites[currentState][currentFrame];
            }

            Canvas.SetLeft(PetImage, petX);
            Canvas.SetTop(PetImage, petY);
            PetTransform.ScaleX = direction * Settings.Scale;

            if (SpeechBubble.Visibility == Visibility.Visible)
            {
                double bubbleX = petX + (Settings.SpriteSize * Settings.Scale / 2.0) - (SpeechBubble.ActualWidth / 2.0);
                double bubbleY = petY - SpeechBubble.ActualHeight - 10;

                if (bubbleX < 10) bubbleX = 10;
                if (bubbleX > this.Width - SpeechBubble.ActualWidth - 10)
                    bubbleX = this.Width - SpeechBubble.ActualWidth - 10;

                Canvas.SetLeft(SpeechBubble, bubbleX);
                Canvas.SetTop(SpeechBubble, bubbleY);
            }
        }

        private void SetState(string newState)
        {
            if (newState == Settings.StateIdle || newState == Settings.StateIdle2)
            {
                newState = idleVersion;
            }

            if (currentState != newState)
            {
                currentState = newState;
                currentFrame = 0;
                frameTimer = 0;
            }
        }

        private void ShowText(string text, int seconds = 5)
        {
            SpeechText.Text = text;
            SpeechBubble.Visibility = Visibility.Visible;
            textTimer = seconds * Settings.FPS;
            SpeechBubble.UpdateLayout();
        }

        private void PetImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e) { }

        private void PetImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetState(Settings.StateHurt);
            stateTimer = 25;
            ShowText("Ой! Не лякай мене так!", 5);
        }

        private void MenuFocus_Click(object sender, RoutedEventArgs e) { }
        private void MenuExit_Click(object sender, RoutedEventArgs e) { }
    }
}