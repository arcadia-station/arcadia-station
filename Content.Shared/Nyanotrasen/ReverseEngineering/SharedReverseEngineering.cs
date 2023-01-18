using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ReverseEngineering;

[Serializable, NetSerializable]
public enum ReverseEngineeringMachineUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineServerSelectionMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineScanButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachinePrintButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineScanUpdateState : BoundUserInterfaceState
{
    public EntityUid? Target;

    public bool ServerConnected;

    public bool CanScan;

    public FormattedMessage? ScanReport;

    public bool Scanning;

    public TimeSpan TimeRemaining;

    public TimeSpan TotalTime;

    public ReverseEngineeringMachineScanUpdateState(EntityUid? target, bool serverConnected, bool canScan,
        FormattedMessage? scanReport, bool scanning, TimeSpan timeRemaining, TimeSpan totalTime)
    {
        Target = target;
        ServerConnected = serverConnected;
        CanScan = canScan;

        ScanReport = scanReport;

        Scanning = scanning;
        TimeRemaining = timeRemaining;
        TotalTime = totalTime;
    }
}

/// <summary>
// 3d6 + scanner bonus + danger bonus - item difficulty
/// </summary>
[Serializable, NetSerializable]
public enum ReverseEngineeringTickResult : byte
{
    Destruction, // 8 (only destroys if danger bonus is active)
    Stagnation, // 9-10
    SuccessMinor, // 11-12
    SuccessAverage, // 13-15
    SuccessMajor, // 16-17
    InstantSuccess // 18
}
