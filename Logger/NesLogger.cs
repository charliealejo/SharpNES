using System;
using System.IO;

namespace Logger
{
    public class NesLogger : IDisposable
    {
        private StreamWriter? _logWriter;

        public NesLogger()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"cpu_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _logWriter = new StreamWriter(logPath, false) { AutoFlush = true };
        }

        public void Log(string message)
        {
            _logWriter!.WriteLine(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logWriter!.Dispose();
                _logWriter = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
