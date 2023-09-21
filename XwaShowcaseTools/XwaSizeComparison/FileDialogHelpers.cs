using Microsoft.Win32;
using System.Threading;

namespace XwaSizeComparison
{
    static class FileDialogHelpers
    {
        public static string GetOpenSceneFile()
        {
            string fileName = null;

            Thread thread = new Thread(() =>
            {
                var dialog = new OpenFileDialog
                {
                    DefaultExt = ".txt",
                    CheckFileExists = true,
                    Filter = "Scene TXT files (*.txt)|*.txt"
                };

                if (dialog.ShowDialog() != true)
                {
                    fileName = null;
                    return;
                }

                fileName = dialog.FileName;
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return fileName;
        }
    }
}
