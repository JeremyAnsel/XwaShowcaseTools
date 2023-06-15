using JeremyAnsel.Xwa.Opt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XwaOptShowcase
{
    /// <summary>
    /// Logique d'interaction pour OptProfileSelectorDialog.xaml
    /// </summary>
    public partial class OptProfileSelectorDialog : Window
    {
        public OptProfileSelectorDialog(string optFileName)
        {
            InitializeComponent();

            this.OptFileName = optFileName;

            OptFile optFile = OptFile.FromFile(optFileName);

            this.OptVersions = Enumerable.Range(0, optFile.MaxTextureVersion).ToList();
            this.OptObjectProfiles = OptModel.GetObjectProfiles(OptFileName).Keys.ToList();
            this.OptSkins = OptModel.GetSkins(OptFileName);

            this.DataContext = this;
        }

        public string OptFileName { get; }

        public List<int> OptVersions { get; }

        public List<string> OptObjectProfiles { get; }

        public List<string> OptSkins { get; }

        public int SelectedVersion { get; private set; }

        public string SelectedObjectProfile { get; private set; }

        public List<string> SelectedSkins { get; } = new();

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            this.SelectedVersion = this.optVersionsListBox.SelectedIndex;
            this.SelectedObjectProfile = (string)this.optObjectProfilesListBox.SelectedItem;
            this.SelectedSkins.AddRange(this.optSelectedSkinsListBox.Items.Cast<string>());

            this.DialogResult = true;
        }

        private void ClearSelectedSkinsButton_Click(object sender, RoutedEventArgs e)
        {
            this.optSelectedSkinsListBox.Items.Clear();
        }

        private void AddSelectedSkinsButton_Click(object sender, RoutedEventArgs e)
        {
            this.AddSelectedSkin();
        }

        private void OptSkinsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.AddSelectedSkin();
        }

        private void AddSelectedSkin()
        {
            string item = this.optSkinsListBox.SelectedItem as string;

            if (item == null)
            {
                return;
            }

            if (this.optSelectedSkinsListBox.Items.Contains(item))
            {
                return;
            }

            this.optSelectedSkinsListBox.Items.Add(item);
        }
    }
}
