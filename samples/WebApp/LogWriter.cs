// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 19b462998531ca017975384636941da5636d5ffd6441e4348a7c97f2902068c2
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp
{
    public class LogWriter : ILogWriter
    {
        public Task WriteError(LogRecord record)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] {record.Message}");
            Console.ResetColor();
            
            return Task.CompletedTask;
        }

        public Task WriteEvent(LogRecord record)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(record.Message);
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
