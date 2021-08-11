using System;
using System.IO;

namespace BCKarcmove
{

    public enum EventType
    {
        Info,
        Warn,
        Err
    }

    public class Logger
    {
        private readonly StreamWriter _writer;

        public Logger()
        {
            _writer = new StreamWriter("logfile.log");
            WriteToLog($"Program started at {Environment.MachineName}", EventType.Info);
        }

        public void WriteToLog(string message, EventType eventType)
        {
            var prefix = $"{DateTime.Now}: ";
            switch (eventType)
            {
                case EventType.Err:
                    prefix += "|ERROR|";
                    break;
                case EventType.Warn:
                    prefix += "|WARNING|";
                    break;
                case EventType.Info:
                    prefix += "|INFO|";
                    break;
            }
            _writer.WriteLine($"{prefix} {message}");
        }

        public void FinishLogger()
        {
            WriteToLog("Program finished", EventType.Info);
            _writer.Close();
        }

    }
}
