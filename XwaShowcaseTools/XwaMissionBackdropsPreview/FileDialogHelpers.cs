using Microsoft.Win32;
using System.Threading;

namespace XwaMissionBackdropsPreview;

internal static class FileDialogHelpers
{
    public static string GetOpenTieFile()
    {
        string fileName = null;

        Thread thread = new Thread(() =>
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".tie",
                CheckFileExists = true,
                Filter = "TIE files (*.tie)|*.tie"
            };

            if (dialog.ShowDialog() != true)
            {
                fileName = null;
            }

            fileName = dialog.FileName;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return fileName;
    }
}
