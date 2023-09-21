using System;
using System.IO;

namespace XwaSizeComparison
{
    static class ConsoleHelpers
    {
        public static void OpenConsole()
        {
            NativeMethods.AllocConsole();
            TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);
        }

        public static void CloseConsole()
        {
            NativeMethods.FreeConsole();
        }
    }
}
