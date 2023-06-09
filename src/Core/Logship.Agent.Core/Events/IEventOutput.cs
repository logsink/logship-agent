﻿using Logship.Agent.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Events
{
    public interface IEventOutput
    {
        Task<bool> SendAsync(IReadOnlyCollection<DataRecord> records, CancellationToken cancellationToken);
    }
}
