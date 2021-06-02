namespace FindMyBatteries.FindMe.DTOs
{
    public partial class FindMeResponse
    {
        public Device[]? Content { get; set; }
    }

    public partial class Device
    {
        public string? Name { get; set; }
        public double? BatteryLevel { get; set; }
        public string? BatteryStatus { get; set; }
    }

    public enum BatteryStatus
    {
        Charging,
        NotCharging,
        Unknown
    };
}
