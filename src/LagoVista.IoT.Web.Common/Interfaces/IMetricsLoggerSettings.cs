using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common
{
    public interface IMetricsLoggerSettings
    {
        IConnectionSettings MetricsLoggerStorage { get; }
    }
}
