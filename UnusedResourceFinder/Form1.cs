using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace UnusedResourceFinder
{
    using System.Collections;
    using System.IO;
    using System.Resources;

    public partial class Form1 : Form
    {
        #region Fields

        private List<string> fileTypes = null;

        private List<string> files = null;

        private List<string> resources = null;

        private List<string> unusedResources;

        #endregion

        #region Constructors and Destructors

        public Form1()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Methods

        private static string GetFileText(string name)
        {
            string fileContents = string.Empty;

            if (System.IO.File.Exists(name))
            {
                fileContents = System.IO.File.ReadAllText(name);
            }

            return fileContents;
        }

        private void FindUnusedResources()
        {
            var content = string.Empty;
            string filename = string.Empty;
            try
            {
                foreach (var s in this.resources)
                {
                    this.progressBar1.Value += 1;
                    this.filesProgressBar.Value = 0;
                    this.Log(string.Format("{1}****************** {0}", s, Environment.NewLine));
                    bool foundResource = false;
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        continue;
                    }

                    foreach (var file in this.files)
                    {
                        this.filesProgressBar.Value += 1;
                        filename = file;
                        content = File.ReadAllText(file);
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            continue;
                        }

                        if (content.Contains(s))
                        {
                            this.Log(string.Format("Is used in {0}", file));
                            foundResource = true;
                        }
                    }

                    if (foundResource == false && this.unusedResources.Contains(s) == false)
                    {
                        this.Log(string.Format("{0} is UNUSED", s));
                        this.RecordResult(s);
                        this.unusedResources.Add(s);
                    }
                }

                this.filesProgressBar.Value = this.filesProgressBar.Maximum;
                this.progressBar1.Value = this.progressBar1.Maximum;
            }
            catch (Exception ex)
            {
                this.Log(string.Format("Could not read file {0}. This exception occured: {1}", filename, ex.Message));
            }
        }

        private void GetAllFilesInFolder(string folderToSearch)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(folderToSearch))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        if (this.IsValidFileType(f))
                        {
                            this.files.Add(f);
                        }
                    }

                    this.GetAllFilesInFolder(d);
                }
            }
            catch (Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private List<string> GetFileTypes()
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(this.txtFileTypes.Text))
            {
                return result;
            }

            foreach (var s in this.txtFileTypes.Text.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    result.Add(s.Trim().ToLower());
                }
            }

            return result;
        }

        private bool IsValidFileType(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }

            file = file.ToLower();
            foreach (var fileType in this.fileTypes)
            {
                if (file.EndsWith(fileType))
                {
                    return true;
                }
            }

            return false;
        }

        private void LoadFiles()
        {
            string startFolder = this.txtFolder.Text;
            var dir = new System.IO.DirectoryInfo(startFolder);
            IEnumerable<FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            string resourceFilename = this.txtResourceFile.Text;
            resourceFilename = resourceFilename.Substring(0, resourceFilename.LastIndexOf('.'));

            var queryMatchingFiles = from file in fileList
                                     where
                                         this.IsValidFileType(file.FullName)
                                         && file.FullName.StartsWith(resourceFilename) == false
                                     let fileText = GetFileText(file.FullName)
                                     // where fileText.Contains(searchTerm)
                                     select file.FullName;
            this.files.AddRange(queryMatchingFiles);
        }

        private void LoadIosResources()
        {
            this.resources = new List<string>();
            var lines = File.ReadAllLines(this.txtResourceFile.Text);

            foreach (var line in lines)
            {
                if (line.StartsWith("\"") == false)
                {
                    continue;
                }

                var key = line.Split('=')[0];
                key = key.Trim();
                key = key.TrimStart(new[] { '"' });
                key = key.TrimEnd(new[] { '"' });
                this.resources.Add(key);
            }
        }

        private void LoadWindowsResources()
        {
            this.resources = new List<string>();
            var resourceReader = new ResXResourceReader(this.txtResourceFile.Text);
            bool isReswFile = this.txtResourceFile.Text.EndsWith(".resw");
            string key = string.Empty;
            foreach (DictionaryEntry r in resourceReader)
            {
                if (isReswFile)
                {
                    key = r.Key.ToString();
                    key = key.Substring(0, key.LastIndexOf('.'));
                    this.resources.Add(key);
                }
                else
                {
                    this.resources.Add(r.Key.ToString());
                }
            }
        }

        private void Log(string message)
        {
            this.txtLog.AppendText(message + Environment.NewLine);
            Application.DoEvents();
        }

        private void RecordResult(string message)
        {
            this.txtResult.AppendText(message + Environment.NewLine);
            Application.DoEvents();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtFolder.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.progressBar1.Minimum = 0;
            this.progressBar1.Value = 0;
            this.filesProgressBar.Minimum = 0;
            this.filesProgressBar.Value = 0;
            this.txtResult.Clear();
            this.fileTypes = this.GetFileTypes();
            this.files = new List<string>();
            this.unusedResources = new List<string>();
            this.LoadFiles();
            this.Log(string.Format("Found {0} to search", this.files.Count));
            if (this.rdbiOS.Checked)
            {
                this.LoadIosResources();
            }
            else
            {
                this.LoadWindowsResources();
            }

            this.progressBar1.Maximum = this.resources.Count;
            this.filesProgressBar.Maximum = this.files.Count;
            Application.DoEvents();
            this.FindUnusedResources();

            this.txtResult.AppendText(Environment.NewLine + "Finished Searching");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtResourceFile.Text = this.openFileDialog1.FileName;
            }
        }

        #endregion
    }
}