﻿using Logship.Agent.Core.Events;
using Logship.Agent.Core.Inputs.Windows;
using Logship.Agent.Core.Records;
using Logship.Agent.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Inputs.Shared
{
    public class ProcessInfoService : BaseInputService
    {
        protected override TimeSpan DefaultInterval => TimeSpan.FromSeconds(5);

        public ProcessInfoService(IEventBuffer buffer, ILogger logger) : base(buffer, nameof(ProcessInfoService), logger)
        {
        }

        protected override async Task ExecuteSingleAsync(CancellationToken token)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var processCache = new Dictionary<uint, string>();
            foreach(var p in Process.GetProcesses())
            {
                processCache[(uint)p.Id] = p.ProcessName;
                var c = CreateRecord("System.Process", timestamp);
                c.Data["Name"] = p.ProcessName;
                c.Data["Id"] = p.Id;
                c.Data["BasePriority"] = p.BasePriority;
                c.Data["MainWindowTitle"] = p.MainWindowTitle;
                c.Data["HandleCount"] = p.HandleCount;
                c.Data["NonpagedSystemMemorySize64"] = p.NonpagedSystemMemorySize64;
                c.Data["PagedMemorySize64"] = p.PagedMemorySize64;
                c.Data["PagedSystemMemorySize64"] = p.PagedSystemMemorySize64;
                c.Data["PeakPagedMemorySize64"] = p.PeakPagedMemorySize64;
                c.Data["PeakVirtualMemorySize64"] = p.PeakVirtualMemorySize64;
                c.Data["PeakWorkingSet64"] = p.PeakWorkingSet64;
                c.Data["PrivateMemorySize64"] = p.PrivateMemorySize64;
                c.Data["SessionId"] = p.SessionId;
                c.Data["ThreadCount"] = p.Threads.Count;
                c.Data["VirtualMemorySize64"] = p.VirtualMemorySize64;
                c.Data["WorkingSet64"] = p.WorkingSet64;                
                this.Buffer.Add(c);
            }

            const string schema = "System.Process.Ports";
            if (OperatingSystem.IsWindows())
            {
                var tcp = Windows.WindowsProcessPortInfoUtils.GetTcpPortInfo();
                foreach(var port in tcp)
                {
                    var record = CreateRecord(schema);
                    if (false == processCache.TryGetValue(port.OwningPid, out string? processName))
                    {
                        processName = string.Empty;
                    }

                    AddInfo("tcp", record, port, processName);
                    this.Buffer.Add(record);
                }

                var udp = Windows.WindowsProcessPortInfoUtils.GetUdpPortInfo();
                foreach (var port in udp)
                {
                    var record = CreateRecord(schema);
                    if (false == processCache.TryGetValue(port.OwningPid, out string? processName))
                    {
                        processName = string.Empty;
                    }

                    AddInfo("udp", record, port, processName);
                    this.Buffer.Add(record);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                var tcp = await Linux.LinuxProcessPortInfoUtils.ListProcessesUsingTcpPortsAsync(token);
                foreach (var port in tcp)
                {
                    var record = CreateRecord(schema);
                    if (false == processCache.TryGetValue(port.OwningPid, out string? processName))
                    {
                        processName = string.Empty;
                    }

                    AddInfo("tcp", record, port, processName);
                    this.Buffer.Add(record);
                }

                var udp = await Linux.LinuxProcessPortInfoUtils.ListProcessesUsingUdpPortsAsync(token);
                foreach (var port in udp)
                {
                    var record = CreateRecord(schema);
                    if (false == processCache.TryGetValue(port.OwningPid, out string? processName))
                    {
                        processName = string.Empty;
                    }

                    AddInfo("udp", record, port, processName);
                    this.Buffer.Add(record);
                }
            }
        }

        private void AddInfo(string portType, DataRecord record, ProcessPortInfo info, string processName = "unknown")
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                try
                {
                    processName = Process.GetProcessById((int)info.OwningPid).ProcessName;
                }
                catch (ArgumentException)
                {
                    processName = "unknown";
                }
            }

            record.Data["PortType"] = portType;
            record.Data["OwningPid"] = info.OwningPid;
            record.Data["ProcessName"] = processName;
            record.Data["LocalPort"] = info.LocalPort;
            record.Data["LocalAddr"] = info.LocalAddr;
            record.Data["RemotePort"] = info.RemotePort;
            record.Data["RemoteAddr"] = info.RemoteAddr;
        }

        public readonly struct ProcessPortInfo
        {
            public readonly uint State;
            public readonly uint LocalAddr;
            public readonly uint LocalPort;
            public readonly uint RemoteAddr;
            public readonly uint RemotePort;
            public readonly uint OwningPid;

            public ProcessPortInfo(uint localAddr, uint localPort, uint remoteAddr, uint remotePort, uint owningPid)
            {
                LocalAddr = localAddr;
                LocalPort = localPort;
                RemoteAddr = remoteAddr;
                RemotePort = remotePort;
                OwningPid = owningPid;
            }
        }
    }
}