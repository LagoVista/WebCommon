// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: d9a8ed8d35841017222345e2362192f9af678dc91f476cb503399ee491dc6543
// IndexVersion: 0
// --- END CODE INDEX META ---
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
