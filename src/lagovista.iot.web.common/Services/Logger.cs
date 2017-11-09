using LagoVista.Core.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Services
{
    public class ConsoleLogger : ILogger
    {
        public bool DebugMode { get; set; }

        public void AddCustomEvent(LogLevel level, string tag, string customEvent, params KeyValuePair<string, string>[] args)
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

            Console.WriteLine($"Area: {tag} => {customEvent}");
            foreach (var arg in args)
            {
                Console.WriteLine($"\t\t\t{arg.Key} = {arg.Value}");
            }
        }

        public TimedEvent StartTimedEvent(string area, string description)
        {
            return new TimedEvent(area, description);
        }

        public void EndTimedEvent(TimedEvent evt)
        {
            var duration = DateTime.Now - evt.StartTime;
            AddCustomEvent(LogLevel.Message, evt.Area, evt.Description, new KeyValuePair<string, string>("Duration", Math.Round(duration.TotalSeconds, 4).ToString()));
        }

        public bool Verbose { get; set; }

        public void AddException(string area, Exception ex, params KeyValuePair<string, string>[] args)
        {

        }
        
        public void AddKVPs(params KeyValuePair<string, string>[] args)
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
