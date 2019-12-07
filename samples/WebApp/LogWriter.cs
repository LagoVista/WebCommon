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
            return Task.CompletedTask;
        }

        public Task WriteEvent(LogRecord record)
        {
            return Task.CompletedTask;
        }
    }
}
