using System;
using System.IO;

public enum LogLevel { Debug, Info, Warning, Error }

public class Program
{
    public static void Main()
    {
        string path = "C:\\ProgramData\\AppsielPrintManager\\apm_activity.log";
        if (File.Exists(path))
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string line;
            int success = 0;
            int fail = 0;
            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    if (line.StartsWith("["))
                    {
                        int firstClose = line.IndexOf("]");
                        string dateStr = line.Substring(1, firstClose - 1);

                        int levelOpen = line.IndexOf("[", firstClose + 1);
                        int levelClose = line.IndexOf("]", levelOpen + 1);
                        string levelStr = line.Substring(levelOpen + 1, levelClose - levelOpen - 1).Trim();

                        int serviceOpen = line.IndexOf("[", levelClose + 1);
                        int serviceClose = line.IndexOf("]", serviceOpen + 1);
                        string serviceStr = line.Substring(serviceOpen + 1, serviceClose - serviceOpen - 1).Trim();

                        string remaining = line.Substring(serviceClose + 1).Trim();
                        string messageStr = remaining;
                        string dataStr = null;

                        if (remaining.Contains(" | DATA: "))
                        {
                            int dataIdx = remaining.IndexOf(" | DATA: ");
                            messageStr = remaining.Substring(0, dataIdx).Trim();
                            dataStr = remaining.Substring(dataIdx + 9).Trim();
                        }

                        DateTime dt = DateTime.Parse(dateStr);
                        LogLevel level = Enum.Parse<LogLevel>(levelStr, true);

                        success++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Parse error on line: " + line + " -> " + e.Message);
                    fail++;
                }
            }
            Console.WriteLine($"Success: {success}, Fail: {fail}");
        }
        else
        {
            Console.WriteLine("Log file not found.");
        }
    }
}
