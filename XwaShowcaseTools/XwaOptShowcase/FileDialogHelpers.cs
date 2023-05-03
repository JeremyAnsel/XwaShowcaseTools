using Microsoft.Win32;
using System.IO;
using System.Threading;

namespace XwaOptShowcase
{
    static class FileDialogHelpers
    {
        public static string GetOpenImageFile()
        {
            string fileName = null;

            Thread thread = new Thread(() =>
            {
                var dialog = new OpenFileDialog
                {
                    DefaultExt = ".png",
                    CheckFileExists = true,
                    Filter = "Images (*.png, *.bmp, *.jpg)|*.png;*.bmp;*.jpg|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp|JPG files (*.jpg)|*.jpg"
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

        public static string GetOpenOptFile()
        {
            string fileName = null;

            Thread thread = new Thread(() =>
            {
                var dialog = new OpenFileDialog
                {
                    DefaultExt = ".opt",
                    CheckFileExists = true,
                    Filter = "OPT files (*.opt)|*.opt"
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

        public static string GetSaveAsMp4File(string fileName)
        {
            fileName = Path.GetFullPath(fileName);

            Thread thread = new Thread(() =>
            {
                var dialog = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = ".mp4",
                    Filter = "MP4 files (*.mp4)|*.mp4",
                    InitialDirectory = Path.GetDirectoryName(fileName),
                    FileName = Path.GetFileName(fileName)
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
}
