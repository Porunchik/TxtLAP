using Antipublic.Models;
using Antipublic.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace Antipublic.ViewModels
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public string AntipublicDirectoryPath
        {
            get => Settings.Default.AntipublicDirectoryPath;
            set
            {
                Settings.Default.AntipublicDirectoryPath = value;
                Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public string BaseFilePath
        {
            get => baseFilePath;
            set
            {
                baseFilePath = value;
                OnPropertyChanged();
            }
        }
        private string baseFilePath;

        public string ResultFilePath
        {
            get => resultFilePath;
            set
            {
                resultFilePath = value;
                OnPropertyChanged();
            }
        }
        private string resultFilePath;

        public int NumberOfLinesInBase
        {
            get => numberOfLinesInBase;
            set
            {
                numberOfLinesInBase = value;
                OnPropertyChanged();
            }
        }
        private int numberOfLinesInBase;

        public int NumberOfUniqueLinesInBase
        {
            get => numberOfUniqueLinesInBase;
            set
            {
                numberOfUniqueLinesInBase = value;
                OnPropertyChanged();
            }
        }
        private int numberOfUniqueLinesInBase;

        public int NumberOfLinesInAntipublic
        {
            get => numberOfLinesInAntipublic;
            set
            {
                numberOfLinesInAntipublic = value;
                OnPropertyChanged();
            }
        }
        private int numberOfLinesInAntipublic;

        public string ElapsedTime
        {
            get => elapsedTime;
            set
            {
                elapsedTime = value;
                OnPropertyChanged();
            }
        }
        private string elapsedTime;

        public int SelectedLineComprasion { get; set; }

        public bool IsAddResultToAntipublic { get; set; }

        public bool IsCaseSensitive { get; set; }

        public Command OpenAntipublicCommand { get; set; }

        public Command OpenBaseCommand { get; set; }

        public Command SaveResultCommand { get; set; }

        public Command CheckBaseCommand { get; set; }

        public MainWindowViewModel()
        {
            SelectedLineComprasion = (int)LineComprasion.Full;
            IsCaseSensitive = true;

            OpenAntipublicCommand = new Command((o) =>
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                        AntipublicDirectoryPath = folderBrowserDialog.SelectedPath;
            });

            OpenBaseCommand = new Command((o) =>
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*" })
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                        BaseFilePath = openFileDialog.FileName;
            });

            SaveResultCommand = new Command((o) =>
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*",
                    FileName = File.Exists(BaseFilePath) ?
                    Path.Combine(Path.GetDirectoryName(BaseFilePath), Path.GetFileNameWithoutExtension(BaseFilePath) + "_unique" + Path.GetExtension(BaseFilePath)) :
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unique.txt")
                })
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        ResultFilePath = saveFileDialog.FileName;
            });

            CheckBaseCommand = new Command((o) =>
            {
                ElapsedTime = GetTime(CheckBase).ToString();
            });
        }

        private delegate bool TryProcessLine(string line, out string result);
        private void CheckBase()
        {
            TryProcessLine tryProcessLine = null;
            switch ((LineComprasion)SelectedLineComprasion)
            {
                case LineComprasion.Login:
                    if (IsCaseSensitive)
                        tryProcessLine = TryGetLogin;
                    else
                        tryProcessLine = TryGetLoginLower;
                    break;
                case LineComprasion.Email:
                    if (IsCaseSensitive)
                        tryProcessLine = TryGetEmail;
                    else
                        tryProcessLine = TryGetEmailLower;
                    break;
                case LineComprasion.Full:
                    if (IsCaseSensitive)
                        tryProcessLine = TryNothing;
                    else
                        tryProcessLine = TryToLower;
                    break;
                case LineComprasion.FullWithSeparatorReplacement:
                    if (IsCaseSensitive)
                        tryProcessLine = TryReplaceSeparator;
                    else
                        tryProcessLine = TryReplaceSeparatorLower;
                    break;
            }

            Dictionary<string, string> uniqueLines = new Dictionary<string, string>();

            int numberOfLinesInBase = 0;
            using (StreamReader sr = new StreamReader(BaseFilePath, Encoding.UTF8, false, 131072))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (tryProcessLine(line, out string key))
                    {
                        try
                        {
                            uniqueLines.Add(key, line);
                        }
                        catch (ArgumentException) { }
                    }
                    numberOfLinesInBase++;
                }
            }
            NumberOfLinesInBase = numberOfLinesInBase;

            int numberOfLinesInAntipublic = 0;
            foreach (string path in Directory.EnumerateFiles(AntipublicDirectoryPath, "*.txt", SearchOption.AllDirectories))
            {
                using (StreamReader sr = new StreamReader(path, Encoding.UTF8, false, 131072))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (tryProcessLine(line, out string key))
                            uniqueLines.Remove(key);
                        numberOfLinesInAntipublic++;
                    }
                }
            }
            NumberOfLinesInAntipublic = numberOfLinesInAntipublic;
            NumberOfUniqueLinesInBase = uniqueLines.Count;

            File.WriteAllLines(ResultFilePath, uniqueLines.Values, Encoding.UTF8);
            if (IsAddResultToAntipublic)
                File.WriteAllLines(Path.Combine(AntipublicDirectoryPath, Path.GetFileNameWithoutExtension(BaseFilePath) + "_unique" + Path.GetExtension(BaseFilePath)), uniqueLines.Values, Encoding.UTF8);
        }

        private bool TryNothing(string line, out string newLine)
        {
            newLine = line;
            return true;
        }

        private bool TryToLower(string line, out string newLine)
        {
            newLine = line.ToLower();
            return true;
        }

        private bool TryReplaceSeparator(string line, out string newLine)
        {
            newLine = line.Replace(';', ':');
            return true;
        }

        private bool TryReplaceSeparatorLower(string line, out string newLine)
        {
            bool result = TryReplaceSeparator(line, out newLine);
            if (result)
                newLine = newLine.ToLower();
            return result;
        }

        private bool TryGetLogin(string line, out string login)
        {
            int index = line.IndexOf('@');
            if (index > 0)
            {
                login = line.Substring(0, index);
                return true;
            }
            else
            {
                login = null;
                return false;
            }
        }
        private bool TryGetLoginLower(string line, out string login)
        {
            bool result = TryGetLogin(line, out login);
            if (result)
                login = login.ToLower();
            return result;
        }

        private static readonly char[] separators = new char[] { ':', ';' };
        private bool TryGetEmail(string line, out string email)
        {
            int index = line.IndexOfAny(separators);
            if (index > 0)
            {
                email = line.Substring(0, index);
                return true;
            }
            else
            {
                email = null;
                return false;
            }
        }

        private bool TryGetEmailLower(string line, out string email)
        {
            bool result = TryGetEmail(line, out email);
            if (result)
                email = email.ToLower();
            return result;
        }

        private TimeSpan GetTime(Action action)
        {
            Stopwatch watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            return watch.Elapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    enum LineComprasion
    {
        Login,
        Email,
        Full,
        FullWithSeparatorReplacement
    }
}
