using AppsielPrintManager.Core.Enums;

namespace AppsielPrintManager.Core.Models
{
    public class Scale
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public ScaleParity Parity { get; set; } = ScaleParity.None;
        public ScaleStopBits StopBits { get; set; } = ScaleStopBits.One;
        public bool IsActive { get; set; } = true;
    }
}
