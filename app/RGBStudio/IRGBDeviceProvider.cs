using System.Drawing;

namespace GHelper.RGBStudio
{
    public enum RGBDeviceType
    {
        AsusLaptopKeyboard,
        AsusLightbar,
        KreoKeyboard,
        KreoMouse,
        GenericPeripheral
    }

    public enum RGBZoneType
    {
        SingleZone,
        MultiZone4,
        PerKey,
        Logo,
        ScrollWheel,
        SideStrip,
        Underglow
    }

    public interface IRGBDeviceProvider
    {
        string DeviceId { get; }
        string DeviceName { get; }
        RGBDeviceType DeviceType { get; }
        RGBZoneType ZoneType { get; }
        bool IsConnected { get; }
        bool IsSimulated { get; set; }
        int VendorId { get; }
        int ProductId { get; }

        int MaxBrightness { get; }
        int CurrentBrightness { get; set; }
        int CurrentSpeed { get; set; }

        List<string> SupportedEffects { get; }
        bool SupportsPerKey { get; }
        int TotalKeyOrZoneCount { get; }

        bool ApplyStaticColor(Color color);
        bool ApplyZoneColors(Dictionary<int, Color> zoneColors);
        bool ApplyEffect(RGBEffectConfig effectConfig);
        void TurnOff();
        string GetDiagnosticsInfo();
    }
}
