using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TriggerBot
{
      public class PixelTriggerBot
      {
                // Windows API imports for key simulation
                [DllImport("user32.dll", SetLastError = true)]
                static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

                [DllImport("user32.dll")]
                static extern short GetAsyncKeyState(int vKey);

                // Key codes
                private const int VK_J = 0x4A;  // J key for toggle
                private const int VK_P = 0x50;  // P key for shooting
                private const uint KEYEVENTF_KEYDOWN = 0x0000;
                private const uint KEYEVENTF_KEYUP = 0x0002;

                // Bot configuration
                private const int MONITOR_SIZE = 20;  // 20x20 pixel area
                private const int PURPLE_TOLERANCE = 15;  // RGB tolerance for purple detection
                private const int SHOT_COOLDOWN_MS = 50;  // 50ms cooldown between shots
                private const int MONITOR_DELAY_MS = 5;   // 5ms monitoring loop delay

                // Purple color reference (adjust as needed)
                private static readonly Color PURPLE_TARGET = Color.FromArgb(128, 0, 128);

                // Bot state
                private bool isMonitoring = false;
                private bool isRunning = true;
                private DateTime lastShotTime = DateTime.MinValue;
                private readonly object lockObject = new object();

                // Screen capture variables
                private int screenCenterX;
                private int screenCenterY;
                private Rectangle captureArea;

                public PixelTriggerBot()
                {
                              InitializeScreenCapture();
                              Console.WriteLine("C# Trigger Bot Initialized!");
                              Console.WriteLine("Press 'J' to toggle monitoring ON/OFF");
                              Console.WriteLine("Press 'ESC' to exit");
                              Console.WriteLine($"Monitoring area: {MONITOR_SIZE}x{MONITOR_SIZE} pixels around crosshair");
                              Console.WriteLine($"Target color: Purple (RGB tolerance: ±{PURPLE_TOLERANCE})");
                              Console.WriteLine($"Shot cooldown: {SHOT_COOLDOWN_MS}ms");
                }

                private void InitializeScreenCapture()
                {
                              // Get screen center coordinates
                              screenCenterX = Screen.PrimaryScreen.Bounds.Width / 2;
                              screenCenterY = Screen.PrimaryScreen.Bounds.Height / 2;

                              // Define capture area around crosshair
                              int halfSize = MONITOR_SIZE / 2;
                              captureArea = new Rectangle(
                                                screenCenterX - halfSize,
                                                screenCenterY - halfSize,
                                                MONITOR_SIZE,
                                                MONITOR_SIZE
                                            );

                              Console.WriteLine($"Screen center: ({screenCenterX}, {screenCenterY})");
                              Console.WriteLine($"Capture area: {captureArea}");
                }

                public async Task StartAsync()
                {
                              // Start key monitoring task
                              var keyMonitorTask = Task.Run(MonitorKeys);

                              // Start pixel monitoring task
                              var pixelMonitorTask = Task.Run(MonitorPixels);

                              // Wait for both tasks
                              await Task.WhenAll(keyMonitorTask, pixelMonitorTask);
                }

                private async Task MonitorKeys()
                {
                              bool jKeyPressed = false;
                              bool escKeyPressed = false;

                              while (isRunning)
                              {
                                                try
                                                {
                                                                      // Check J key for toggle
                                                                      bool jKeyCurrentlyPressed = (GetAsyncKeyState(VK_J) & 0x8000) != 0;
                                                                      if (jKeyCurrentlyPressed && !jKeyPressed)
                                                                      {
                                                                                                ToggleMonitoring();
                                                                      }
                                                                      jKeyPressed = jKeyCurrentlyPressed;

                                                                      // Check ESC key for exit
                                                                      bool escKeyCurrentlyPressed = (GetAsyncKeyState(Keys.Escape) & 0x8000) != 0;
                                                                      if (escKeyCurrentlyPressed && !escKeyPressed)
                                                                      {
                                                                                                Console.WriteLine("Exiting...");
                                                                                                isRunning = false;
                                                                                                break;
                                                                      }
                                                                      escKeyPressed = escKeyCurrentlyPressed;

                                                                      await Task.Delay(10); // Small delay to prevent excessive CPU usage
                                                }
                                                catch (Exception ex)
                                                {
                                                                      Console.WriteLine($"Key monitoring error: {ex.Message}");
                                                }
                              }
                }

                private void ToggleMonitoring()
                {
                              lock (lockObject)
                              {
                                                isMonitoring = !isMonitoring;
                                                string status = isMonitoring ? "ON" : "OFF";
                                                Console.WriteLine($"Monitoring: {status}");

                                                if (isMonitoring)
                                                {
                                                                      Console.WriteLine("🎯 Scanning for purple enemies...");
                                                }
                                                else
                                                {
                                                                      Console.WriteLine("⏸️ Monitoring paused");
                                                }
                              }
                }

                private async Task MonitorPixels()
                {
                              while (isRunning)
                              {
                                                try
                                                {
                                                                      if (isMonitoring)
                                                                      {
                                                                                                if (DetectPurpleEnemy())
                                                                                                {
                                                                                                                              if (CanShoot())
                                                                                                                              {
                                                                                                                                                                Shoot();
                                                                                                                              }
                                                                                                }
                                                                      }

                                                                      await Task.Delay(MONITOR_DELAY_MS);
                                                }
                                                catch (Exception ex)
                                                {
                                                                      Console.WriteLine($"Pixel monitoring error: {ex.Message}");
                                                                      await Task.Delay(100); // Longer delay on error
                                                }
                              }
                }

                private bool DetectPurpleEnemy()
                {
                              try
                              {
                                                using (Bitmap screenshot = new Bitmap(captureArea.Width, captureArea.Height))
                                                {
                                                                      using (Graphics g = Graphics.FromImage(screenshot))
                                                                      {
                                                                                                // Capture screen area around crosshair
                                                                                                g.CopyFromScreen(captureArea.Location, Point.Empty, captureArea.Size);
                                                                      }

                                                                      // Lock bitmap for fast pixel access
                                                                      BitmapData bitmapData = screenshot.LockBits(
                                                                                                new Rectangle(0, 0, screenshot.Width, screenshot.Height),
                                                                                                ImageLockMode.ReadOnly,
                                                                                                PixelFormat.Format24bppRgb
                                                                                            );

                                                                      try
                                                                      {
                                                                                                unsafe
                                                                                                {
                                                                                                                              byte* ptr = (byte*)bitmapData.Scan0;
                                                                                                                              int stride = bitmapData.Stride;

                                                                                                                              // Scan all pixels in the capture area
                                                                                                                              for (int y = 0; y < screenshot.Height; y++)
                                                                                                                              {
                                                                                                                                                                for (int x = 0; x < screenshot.Width; x++)
                                                                                                                                                                {
                                                                                                                                                                                                      int offset = y * stride + x * 3;
                                                                                                                                                                                                      byte blue = ptr[offset];
                                                                                                                                                                                                      byte green = ptr[offset + 1];
                                                                                                                                                                                                      byte red = ptr[offset + 2];
                                                                                                                                                                  
                                                                                                                                                                                                      // Check if pixel matches purple color within tolerance
                                                                                                                                                                                                      if (IsPurpleColor(red, green, blue))
                                                                                                                                                                                                                                            {
                                                                                                                                                                                                                                                return true;
                                                                                                                                                                                                                                            }
                                                                                                                                                                }
                                                                                                                              }
                                                                                                }
                                                                      }
                                                                      finally
                                                                      {
                                                                                                screenshot.UnlockBits(bitmapData);
                                                                      }
                                                }
                              }
                              catch (Exception ex)
                              {
                                                Console.WriteLine($"Screen capturte green, byte blue)
                                                                  {
                                                                                // Check if the colorPLE_TARGET.R);
                                                                                int greenDiff = Math.Abs(green - PURPLE_TARGET.G);
                                                                                int blueDiff = Math.Abs(blue - PURPLE_TARGET.B);
                                                                    
                                                                                return redDiff <= PURPLE_TOLERANCE && 
                                                                                                     greenDiff <= PURPLE_TOLERANCE && 
                                                                                                     blueDiff <= PURPLE_TOLERANCE;
                                                                  }
                                                                  
                                                                          private bool CanShoot()
                                                                  {
                                                                                return (DateTime.Now - lastShotTime).TotalMilliseconds >= SHOT_COOLDOWN_MS;
                                                                  }
                                                                  
                                                                          private void Shoot()
                                                                  {
                                                                                try
                                                                                {
                                                                                                  // Simulate P key press
                                                                                                  keybd_event(VK_P, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                                                                                  is within tolerance of our target purple
                                                                                int redDiff = Math.Abs(red - PUR
