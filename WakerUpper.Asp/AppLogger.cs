using System;
using System.Text.Json;

namespace WakerUpper.Asp
{
    // this is just a placeholder to be able to DI a plain ILogger
    public class AppLogger
    {
        public static void LogJson(object obj)
        {
            Console.WriteLine(JsonSerializer.Serialize(obj));
        }
    }
}
