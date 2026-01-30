using System;
using System.IO;
using UnityEngine;

namespace Project.Domain
{
    /// <summary>
    /// Simple file logger that writes logs to a persistent file.
    /// File is replaced on each game session start.
    /// Thread-safe for Unity main thread usage.
    /// </summary>
    public static class FileLogger
    {
        private static string _logFilePath;
        private static StreamWriter _writer;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initialize the logger. Call this once at the start of your game session.
        /// </summary>
        public static void Initialize(string fileName = "battle_debug.log")
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                try
                {
                    // Use Unity's persistent data path (accessible and writable)
                    _logFilePath = Path.Combine(Application.dataPath, "..", fileName);
                    
                    // Delete old log file if exists
                    if (File.Exists(_logFilePath))
                    {
                        File.Delete(_logFilePath);
                    }

                    // Create new file and keep stream open
                    _writer = new StreamWriter(_logFilePath, append: false)
                    {
                        AutoFlush = true // Ensure immediate write to disk
                    };

                    _isInitialized = true;

                    // Write header
                    string header = $"=== VANGUARD ARENA DEBUG LOG ===\n" +
                                  $"Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"Unity Version: {Application.unityVersion}\n" +
                                  $"Platform: {Application.platform}\n" +
                                  $"Log File: {_logFilePath}\n" +
                                  $"=====================================\n\n";
                    
                    _writer.WriteLine(header);
                    Debug.Log($"[FileLogger] Initialized. Logging to: {_logFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FileLogger] Failed to initialize: {ex.Message}");
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// Log a message with timestamp and category.
        /// Also logs to Unity Console for real-time debugging.
        /// Auto-initializes if not already initialized.
        /// </summary>
        public static void Log(string message, string category = "INFO")
        {
            if (!_isInitialized)
            {
                Initialize(); // Auto-initialize on first use
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{category}] {message}";

            lock (_lock)
            {
                try
                {
                    _writer?.WriteLine(formattedMessage);
                    
                    // Also log to Unity Console for real-time debugging
                    Debug.Log(formattedMessage);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FileLogger] Write failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log a warning message.
        /// Auto-initializes if not already initialized.
        /// </summary>
        public static void LogWarning(string message, string category = "WARNING")
        {
            if (!_isInitialized)
            {
                Initialize(); // Auto-initialize on first use
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{category}] ⚠️ {message}";

            lock (_lock)
            {
                try
                {
                    _writer?.WriteLine(formattedMessage);
                    Debug.LogWarning(formattedMessage);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FileLogger] Write failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log an error message.
        /// Auto-initializes if not already initialized.
        /// </summary>
        public static void LogError(string message, string category = "ERROR")
        {
            if (!_isInitialized)
            {
                Initialize(); // Auto-initialize on first use
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{category}] ❌ {message}";

            lock (_lock)
            {
                try
                {
                    _writer?.WriteLine(formattedMessage);
                    Debug.LogError(formattedMessage);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FileLogger] Write failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log a section separator for better readability.
        /// Auto-initializes if not already initialized.
        /// </summary>
        public static void LogSeparator(string title = "")
        {
            if (!_isInitialized)
            {
                Initialize(); // Auto-initialize on first use
            }

            string separator = string.IsNullOrEmpty(title) 
                ? "\n" + new string('=', 80) + "\n"
                : $"\n{'='} {title} {new string('=', 75 - title.Length)}\n";

            lock (_lock)
            {
                try
                {
                    _writer?.WriteLine(separator);
                }
                catch { }
            }
        }

        /// <summary>
        /// Flush and close the log file. Call this on application quit.
        /// </summary>
        public static void Shutdown()
        {
            if (!_isInitialized) return;

            lock (_lock)
            {
                try
                {
                    _writer?.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] [SYSTEM] Session ended.\n");
                    _writer?.Flush();
                    _writer?.Close();
                    _writer?.Dispose();
                    _writer = null;
                    _isInitialized = false;

                    Debug.Log($"[FileLogger] Shutdown complete. Log saved to: {_logFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FileLogger] Shutdown failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the current log file path.
        /// </summary>
        public static string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Check if logger is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }
}
