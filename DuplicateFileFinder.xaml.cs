namespace FindDuplicateFile
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Forms;
    using System.Text;
  using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Resources;

    public partial class DuplicateFileFinder : Window, INotifyPropertyChanged
    {
        string strLanguage = "";
        Boolean boolInit = true;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }

      
        private bool isHashSearch;

        public bool IsHashSearch
        {
            get { return isHashSearch; }
            set { isHashSearch = value; OnPropertyChanged("IsHashSearch"); }
        }

     
        private System.ComponentModel.BackgroundWorker backgroundWorker = new System.ComponentModel.BackgroundWorker();

        private string searchExtension = "*.*";

        private string _currentPath = string.Empty;

      
        public DuplicateFileFinder()
        {
            InitializeComponent();
            boolInit = false;
            this.DataContext = this;

            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorker_DoWork);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorker_RunWorkerCompleted);

            this.backgroundWorker.WorkerSupportsCancellation = true;
            this.backgroundWorker.WorkerReportsProgress = true;
            lastStackPanel.RegisterName("wpfProgressBar", wpfProgressBar);
        }
      
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
                  
            DirectoryInfo dinfo = new DirectoryInfo(this._currentPath);

            if (isHashSearch)
            {
                
                e.Result = this.GetFiles(dinfo)
                            .GroupBy(i => i.fileImpression)
                            .Where(g => g.Count() > 1)
                            .SelectMany(list => list)
                            .ToList<FileAttrib>();

            }
            else
            {
                e.Result = this.GetFiles(dinfo)
                            .GroupBy(i => i.fileLength)
                            .Where(g => g.Count() > 1)
                            .SelectMany(list => list)
                            .ToList<FileAttrib>();
               
            }

            // Cancel if cancel button was clicked. 
            if (this.backgroundWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
        }

          
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
              
                wpfProgressBarAndText.Visibility = Visibility.Collapsed;

                if (e.Cancelled)
                {
                    dataGrid1.ItemsSource = "There is no Data to display";
                }
                else
                {
                    dataGrid1.ItemsSource = (List<FileAttrib>)e.Result;
                }

                CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(dataGrid1.ItemsSource);

                if (IsHashSearch)
                {
                    PropertyGroupDescription pgd = new PropertyGroupDescription("fileImpression");
                    cv.GroupDescriptions.Add(pgd);
                }
                else
                {
                    PropertyGroupDescription pgd = new PropertyGroupDescription("fileLength");
                    cv.GroupDescriptions.Add(pgd);
                }
            }
            finally
            {
                this.Calculate.Content = "Start";
                fileExtension.Visibility = System.Windows.Visibility.Visible;
            }
        }

       
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
           

            //// set isHashSearch flag from radio button value.
            IsHashSearch = rdbHash.IsChecked.Value;

            if (Directory.Exists(this._currentPath))
            {

                if (this.Calculate.Content.Equals("Start"))
                {
                    this.searchExtension = this.fileExtension.Text;
                    if (string.IsNullOrWhiteSpace(this.searchExtension))
                    {
                        this.searchExtension = "*";
                    }

                    this.Calculate.Content = "Stop";
                    this.dataGrid1.AutoGenerateColumns = true;
                    this.backgroundWorker.RunWorkerAsync();
                    this.wpfProgressBarAndText.Visibility = Visibility.Visible;
                    this.fileExtension.Visibility = System.Windows.Visibility.Collapsed;

                }
                else
                {
                             
                    this.backgroundWorker.CancelAsync();

                    this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
                    this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorker_DoWork);
                    this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorker_RunWorkerCompleted);
                    this.backgroundWorker.WorkerSupportsCancellation = true;
                    this.backgroundWorker.WorkerReportsProgress = true;

                    this.backgroundWorker = new BackgroundWorker();
                    this.Calculate.Content = "Start";
                    this.wpfProgressBarAndText.Visibility = Visibility.Collapsed;
                    this.fileExtension.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select correct folder");
            }
        }

     
        private List<FileAttrib> GetFiles(DirectoryInfo dinfo)
        {
            List<FileAttrib> files = new List<FileAttrib>();

            try
            {
                if (this.searchExtension == "*")
                {
                    files.AddRange(dinfo.GetFiles().Select(s => this.ConvertFileInfo(s)));
                }
                else
                {
                    files.AddRange(dinfo.GetFiles().Where(g => g.Extension.ToLower() == string.Format(".{0}", this.searchExtension.ToLower())).Select(s => this.ConvertFileInfo(s)));
                }

                foreach (var directory in dinfo.GetDirectories())
                {
                    files.AddRange(this.GetFiles(directory));
                }
            }
            catch (UnauthorizedAccessException)
            {

            }

            return files;
        }

      
        private FileAttrib ConvertFileInfo(FileInfo finfo)
        {
            return new FileAttrib
            {
                fileName = finfo.Name,
                filePath = finfo.FullName,
                fileImpression = isHashSearch ? this.FileToMD5Hash(finfo.FullName) : null,
                fileLength = finfo.Length
            };
        }

       
        private string FileToMD5Hash(string _fileName)
        {
            var returnString = string.Empty;
            try
            {
                using (var stream = new BufferedStream(File.OpenRead(_fileName), 1200000))
                {
                    SHA256Managed sha = new SHA256Managed();
                    byte[] checksum = sha.ComputeHash(stream);
                    returnString = BitConverter.ToString(checksum).Replace("-", string.Empty);
                }
            }
            catch
            { }
            return returnString;
        }

    
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var source = dataGrid1.ItemsSource.Cast<FileAttrib>().ToList();

                foreach (var selectedItem in dataGrid1.SelectedItems)
                {
                    var deleteFilePath = source.Where((s, i) => s.filePath == ((FileAttrib)selectedItem).filePath).Select(s => s.filePath).First();
                    if (File.Exists(deleteFilePath))
                    {
                        File.Delete(deleteFilePath);
                        source.RemoveAt(dataGrid1.SelectedIndex);
                    }
                }

                dataGrid1.ItemsSource = source;

                CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(dataGrid1.ItemsSource);

                if (IsHashSearch)
                {
                    PropertyGroupDescription pgd = new PropertyGroupDescription("fileImpression");
                    cv.GroupDescriptions.Add(pgd);
                }
                else
                {
                    PropertyGroupDescription pgd = new PropertyGroupDescription("fileLength");
                    cv.GroupDescriptions.Add(pgd);
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("You do not have permission to delete this file.");
            }
        }

     
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            var source = dataGrid1.ItemsSource.Cast<FileAttrib>().ToList();
            var deleteFilePath = source.Where((s, i) => i == dataGrid1.SelectedIndex).Select(s => s.filePath).First();
            ProcessStartInfo fileFolder = new ProcessStartInfo("explorer.exe", "/select,\"" + deleteFilePath + "\"");
            fileFolder.UseShellExecute = false;
            Process.Start(fileFolder);
        }

        private void selectPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select folder to find similar files.";
            folderDialog.SelectedPath = this._currentPath;
            folderDialog.ShowNewFolderButton = false;

            DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this._currentPath = folderDialog.SelectedPath;
                this.folderPath.Text = this._currentPath;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("You have not selected ");
            }
        }

        private void fileExtension_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void dataGrid1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Set_Language();
        }
        private void ddlLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Set_Language();
        }
        private void Set_Language()
        {
            if (boolInit == false)
            {
                strLanguage = "FindDuplicateFile.Languages." + ((ComboBoxItem)ddlLanguage.SelectedItem).Name.ToString();
                ResourceManager LocRM = new ResourceManager(strLanguage, typeof(DuplicateFileFinder).Assembly);
                textBlock.Text = LocRM.GetString("Start");
                textBlock1.Text = LocRM.GetString("Hash");
                textBlock2.Text = LocRM.GetString("Length");

            }
        }

        private void folderPath_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
