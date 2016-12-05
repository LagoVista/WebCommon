using LagoVista.Core.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoI.Web.Common.Services
{
    public class Logger : ILogger
    {
        public void Log(LogLevel level, string area, string message, params KeyValuePair<string, string>[] args)
        {
            if (!Verbose && level == LogLevel.Verbose)
            {
                return;
            }

            switch (level)
            {
                case LogLevel.Error: Console.ForegroundColor = ConsoleColor.Red; Console.Write("[Error]: "); break;
                case LogLevel.Warning: Console.ForegroundColor = ConsoleColor.Yellow; Console.Write("[Warning]: "); break;
                case LogLevel.Message: Console.ForegroundColor = ConsoleColor.Green; Console.Write("[Info]: "); break;
            }

            Console.ResetColor();

            Console.WriteLine($"Area: {area} => {message}");
            foreach (var arg in args)
            {
                Console.WriteLine($"\t\t\t{arg.Key} = {arg.Value}");
            }
        }

        public bool Verbose { get; set; }

        public void LogException(string area, Exception ex, params KeyValuePair<string, string>[] args)
        {

        }

        public void SetKeys(params string[] args)
        {

        }

        public void SetUserId(string userId)
        {

        }

        public void TrackEvent(string message, Dictionary<string, string> parameters)
        {

        }
    }
}
