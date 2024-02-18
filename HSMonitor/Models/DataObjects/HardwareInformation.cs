﻿using System;
using System.Collections.Generic;
using HSMonitor.Models.Enums;

namespace HSMonitor.Models.DataObjects;

public class HardwareData : Data
{
    public HardwareData(HardwareMessage message) : base(message, InformationType.HardwareInformation)
    {
        
    }
}

public class HardwareMessage : IMessage
{
    public CpuInformation? CpuInformation { get; set; }
    public GpuInformation? GpuInformation { get; set; }
    public MemoryInformation? MemoryInformation { get; set; }
}

public class CpuInformation
{
    public string? Type { get; set; }

    public string? Name { get; set; }

    public double Power { get; set; }
    public double Voltage { get; set; }
    public int Clock { get; set; }
    public int DefaultClock { get; set; }
    public int Temperature { get; set; }
    public int Load { get; set; }
}

public class GpuInformation
{
    public string? Type { get; set; }
    public string? Name { get; set; }
    public double Power { get; set; }
    public int CoreClock { get; set; }
    
    public int DefaultCoreClock { get; set; }
    public int CoreTemperature { get; set; }
    public int CoreLoad { get; set; }
    public int VRamClock { get; set; }
    public int VRamMemoryTotal { get; set; }
    public int VRamMemoryUsed { get; set; }
    public IEnumerable<GpuFan>? GpuFans { get; set; }
}

public class GpuFan
{
    public string? Name { get; set; }
    public int Load { get; set; }
    public int Rpm { get; set; }
}

public class MemoryInformation
{
    public string? Type { get; set; }
    public int Load { get; set; }
    public double Available { get; set; }
    public double Used { get; set; }

    public double Total => Math.Round(Available + Used, MidpointRounding.AwayFromZero);
}