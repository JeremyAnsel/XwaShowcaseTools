using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.Xwa.Opt;
using System.Collections.Generic;
using System.IO;

namespace XwaSizeComparison
{
    class SceneOpt
    {
        public SceneOpt()
        {
        }

        public SceneOpt(string fileName)
            : this(string.Empty, fileName, 0, null, null, false)
        {
        }

        public SceneOpt(string title, string fileName)
            : this(title, fileName, 0, null, null, false)
        {
        }

        public SceneOpt(string fileName, bool fillSize)
            : this(null, fileName, 0, null, null, fillSize)
        {
        }

        public SceneOpt(string title, string fileName, bool fillSize)
            : this(title, fileName, 0, null, null, fillSize)
        {
        }

        public SceneOpt(string fileName, int version, string optObjectProfile, List<string> optObjectSkins)
            : this(null, fileName, version, optObjectProfile, optObjectSkins, false)
        {
        }

        public SceneOpt(string title, string fileName, int version, string optObjectProfile, List<string> optObjectSkins)
            : this(title, fileName, version, optObjectProfile, optObjectSkins, false)
        {
        }

        public SceneOpt(string fileName, int version, string optObjectProfile, List<string> optObjectSkins, bool fillSize)
            : this(null, fileName, version, optObjectProfile, optObjectSkins, false)
        {
        }

        public SceneOpt(string title, string fileName, int version, string optObjectProfile, List<string> optObjectSkins, bool fillSize)
        {
            this.Title = title ?? Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();

            this.OptFilename = fileName;
            this.OptVersion = version;

            this.OptObjectProfile = optObjectProfile ?? "Default";
            this.OptObjectSkins.AddRange(optObjectSkins ?? new());

            if (fillSize)
            {
                this.FillSize();
            }
        }

        public string Title { get; set; }

        public string OptFilename { get; set; }

        public int OptVersion { get; set; }

        public string OptObjectProfile { get; set; }

        public List<string> OptObjectSkins { get; } = new();

        public float OptSize { get; set; }

        public Vector OptSpanSize { get; set; }

        public Vector OptMaxSize { get; set; }

        public Vector OptMinSize { get; set; }

        public Vector OptCenter { get; set; }

        public XMMatrix WorldTransform { get; set; }

        public void FillSize()
        {
            if (!File.Exists(this.OptFilename))
            {
                return;
            }

            OptFile opt = string.IsNullOrEmpty(this.OptFilename) ? new OptFile() : OptFile.FromFile(this.OptFilename, false);

            this.OptSize = opt.Size * OptFile.ScaleFactor;
            this.OptSpanSize = opt.SpanSize.Scale(OptFile.ScaleFactor, OptFile.ScaleFactor, OptFile.ScaleFactor);

            Vector max = opt.MaxSize.Scale(OptFile.ScaleFactor, OptFile.ScaleFactor, OptFile.ScaleFactor);
            Vector min = opt.MinSize.Scale(OptFile.ScaleFactor, OptFile.ScaleFactor, OptFile.ScaleFactor);

            this.OptMaxSize = max;
            this.OptMinSize = min;

            this.OptCenter = new Vector()
            {
                X = (max.X + min.X) / 2,
                Y = (max.Y + min.Y) / 2,
                Z = (max.Z + min.Z) / 2
            };
        }

        public SceneOpt Clone()
        {
            var opt = new SceneOpt();

            opt.Title = this.Title;
            opt.OptFilename = this.OptFilename;
            opt.OptVersion = this.OptVersion;
            opt.OptObjectProfile = this.OptObjectProfile;
            opt.OptObjectSkins.AddRange(this.OptObjectSkins);
            opt.OptSize = this.OptSize;
            opt.OptSpanSize = this.OptSpanSize;
            opt.OptMaxSize = this.OptMaxSize;
            opt.OptMinSize = this.OptMinSize;
            opt.OptCenter = this.OptCenter;
            opt.WorldTransform = this.WorldTransform;

            return opt;
        }
    }
}
