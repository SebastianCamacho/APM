using AppsielPrintManager.Core.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación para enviar datos RAW directamente a una impresora local (USB/LPT)
    /// utilizando el Spooler de Windows a través de winspool.drv.
    /// </summary>
    public class LocalRawPrinterClient
    {
        private readonly ILoggingService _logger;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string? pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string? pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public LocalRawPrinterClient(ILoggingService logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendRawDataAsync(string printerName, byte[] data, string jobName = "APM Local Print")
        {
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogError("La impresión local via winspool.drv solo está soportada en Windows.");
                return false;
            }

            return await Task.Run(() =>
            {
                IntPtr hPrinter = new IntPtr(0);
                DOCINFOA di = new DOCINFOA();
                bool success = false;

                di.pDocName = jobName;
                di.pDataType = "RAW";

                _logger.LogInfo($"Iniciando trabajo de impresión RAW en '{printerName}' para {data.Length} bytes.");

                if (OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
                {
                    if (StartDocPrinter(hPrinter, 1, di))
                    {
                        if (StartPagePrinter(hPrinter))
                        {
                            IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(data.Length);
                            Marshal.Copy(data, 0, pUnmanagedBytes, data.Length);

                            success = WritePrinter(hPrinter, pUnmanagedBytes, data.Length, out int dwWritten);

                            Marshal.FreeCoTaskMem(pUnmanagedBytes);
                            EndPagePrinter(hPrinter);
                        }
                        EndDocPrinter(hPrinter);
                    }
                    ClosePrinter(hPrinter);
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();
                    _logger.LogError($"No se pudo abrir la impresora '{printerName}'. Error Win32: {lastError}");
                }

                if (success)
                {
                    _logger.LogInfo($"Datos RAW enviados exitosamente a '{printerName}'.");
                }
                else
                {
                    _logger.LogError($"Fallo al enviar datos RAW a '{printerName}'.");
                }

                return success;
            });
        }
    }
}
