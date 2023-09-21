using JeremyAnsel.Xwa.HooksConfig;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace XwaSizeComparison
{
    class Scene
    {
        public string SceneFileName { get; private set; }

        public string OptDirectory { get; set; }

        public string TextFontName { get; set; }

        public float TextFontSize { get; set; }

        public bool OrderScene { get; set; }

        public int CameraAngle { get; set; }

        public int CameraElevation { get; set; }

        public List<SceneOpt> SceneOpts { get; } = new();

        public static Scene FromFile(string sceneFileName)
        {
            var scene = new Scene
            {
                SceneFileName = sceneFileName
            };

            IList<string> configLines = XwaHooksConfig.GetFileLines(sceneFileName, "Config");

            scene.OptDirectory = XwaHooksConfig.GetFileKeyValue(configLines, "OptDirectory");
            if (!Directory.Exists(scene.OptDirectory))
            {
                scene.OptDirectory = "";
            }

            scene.TextFontName = XwaHooksConfig.GetFileKeyValue(configLines, "TextFontName");
            if (string.IsNullOrEmpty(scene.TextFontName))
            {
                scene.TextFontName = "Verdana";
            }

            scene.TextFontSize = XwaHooksConfig.GetFileKeyValueInt(configLines, "TextFontSize", 60);
            scene.OrderScene = XwaHooksConfig.GetFileKeyValueInt(configLines, "OrderScene", 1) != 0;
            scene.CameraAngle = XwaHooksConfig.GetFileKeyValueInt(configLines, "CameraAngle", 0);
            scene.CameraElevation = XwaHooksConfig.GetFileKeyValueInt(configLines, "CameraElevation", 0);

            IList<string> sceneLines = XwaHooksConfig.GetFileLines(sceneFileName, "Scene");
            bool wasErrorShown = false;

            foreach (string sceneLine in sceneLines)
            {
                IList<string> values = XwaHooksConfig.Tokennize(sceneLine);

                //if (values.Count < 5)
                //{
                //    continue;
                //}

                try
                {
                    string title = values[0];
                    string fileName = values[1];
                    int version = int.Parse(values[2], CultureInfo.InvariantCulture);
                    string objectProfile = values[3];
                    List<string> skins = values[4].Split((char[])null, StringSplitOptions.RemoveEmptyEntries).ToList();

                    var sceneOpt = new SceneOpt(title, Path.Combine(scene.OptDirectory, fileName), version, objectProfile, skins);
                    scene.SceneOpts.Add(sceneOpt);
                }
                catch
                {
                    if (!wasErrorShown)
                    {
                        wasErrorShown = true;

                        MessageBox.Show($"Invalid scene line in \"{sceneFileName}\".\nThe format is:\n// title, fileName, version, objectProfile, skins\nLine is:\n{sceneLine}");
                    }
                }
            }

            return scene;
        }

        public void FillSize()
        {
            //foreach (var sceneOpt in this.SceneOpts)
            //{
            //    sceneOpt.FillSize();
            //}

            this.SceneOpts
                .AsParallel()
                .ForAll(sceneOpt => sceneOpt.FillSize());
        }

        public void OrderBySize(bool orderScene = true)
        {
            SceneOpt opt = this.SceneOpts.FirstOrDefault();

            List<SceneOpt> opts;

            if (orderScene)
            {
                opts = this.SceneOpts
                    .OrderBy(t => (int)(t.OptSize + 0.5f))
                    .ThenBy(t => t.Title)
                    .ToList();
            }
            else
            {
                opts = this.SceneOpts
                    .ToList();
            }

            this.SceneOpts.Clear();
            this.SceneOpts.AddRange(opts);

            if (opt is not null)
            {
                this.SceneOpts.Add(opt.Clone());
            }
        }
    }
}
