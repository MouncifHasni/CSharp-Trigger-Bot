          }
                  }
                      }

                          class Program
                              {
                                      static async T# 🎯 C# Trigger Bot - Purple Enemy Detector

                                      An advanced C# trigger bot that monitors screen pixels around the crosshair for purple enemies and automatically shoots when detected.

                                      ## 🚀 Features

                                      - **Toggle Control**: Press 'J' key to toggle monitoring ON/OFF
                                      - **Smart Detection**: Monitors 20x20 pixel area around screen center (crosshair)
                                      - **Color Recognition**: Detects purple color with RGB tolerance (±15 for lighting variations)
                                      - **Auto Shooting**: Automatically presses 'P' key when purple enemy detected
                                      - **Shot Cooldown**: 50ms cooldown between shots to prevent spam
                                      - **High Performance**: 5-10ms monitoring loop for real-time detection
                                      - **Safe Exit**: Press 'ESC' to safely exit the application

                                      ## 🛠️ Technical Implementation

                                      - **Screen Capture**: Uses `Graphics.CopyFromScreen` for efficient pixel capture
                                      - **Fast Analysis**: Uses `LockBits` with unsafe code for maximum performance
                                      - **Threading**: Background thread for monitoring without blocking UI
                                      
