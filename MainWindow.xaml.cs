using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EnumSharpIntegrator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Semaphore Semaphore { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Semaphore = new Semaphore(1, 1);
        }

        private void BtnFilePath_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Please select any file containing enum as C# format.";

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value)
            {
                tbAlert.Text = "Error: file path is invalid!";
                spAlert.Visibility = Visibility.Visible;
                return;
            }

            TxtFilePath.Text = ofd.FileName;
        }
        private void BtnTargetPath_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "cs files |*.cs";
            ofd.Title = "Please select an enum c# file to update.";
            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value)
            {
                tbAlert.Text = "Error: target path is invalid!";
                spAlert.Visibility = Visibility.Visible;
                return;
            }

            TxtTargetPath.Text = ofd.FileName;
        }
        private void BtnMerge_OnClick(object sender, RoutedEventArgs e)
        {
            Semaphore.WaitOne();

            string targetFile = TxtTargetPath.Text;
            string integratedFile = TxtFilePath.Text;

            // If files are well selected & not empty
            if (integratedFile != "" && targetFile != "")
            {
                if (File.Exists(targetFile) && File.Exists(TxtFilePath.Text))
                {
                    ProgressBar.Value = 0;
                    // We store the target enum content to a dictionnary then separate the header and the footer
                    Dictionary<int, string> targetContent = new Dictionary<int, string>();
                    List<string> header = new List<string>();
                    List<string> footer = new List<string>();
                    int spaceMemory = 0;
                    // The readerIndex will determines where we are in the loop compared to the target file
                    // 0 : Start of file // 1 : In the enum // 2 : In the footer
                    int readerIndex = 0;

                    foreach (string line in File.ReadAllLines(targetFile))
                    {
                        if (Regex.Match(line, @".*\s=\s[0-9][0-9]*").Success)
                        {
                            // Here we store how much spaces we have to reconstituate lines further 
                            if (spaceMemory == 0)
                            {
                                readerIndex++; // indicates we are in the content
                                spaceMemory = line.Substring(0, line.IndexOf("=") - 1).Count(Char.IsWhiteSpace);
                            }

                            string formatedLine = line.Replace(" ", ""); // removes useless spaces
                            string leftside = formatedLine.Substring(0, formatedLine.IndexOf("=")); // Enum Name
                            string rightside = Regex.Match(formatedLine.Substring(formatedLine.IndexOf("=") + 1), @"\d+").Value; // Value associated
                            targetContent.Add(Int32.Parse(rightside), leftside); // We add the content
                        }
                        else if (readerIndex == 1) // If we do not detect enum format anymore, we're in the footer so we indicates where we are
                        {
                            UpdateProgressBar(15);
                            readerIndex++;
                        }

                        // Then we are differents lines thanks to readerIndex
                        if (readerIndex == 0)
                        {
                            header.Add(line);
                        }
                        else if (readerIndex == 2)
                        {
                            footer.Add(line);
                        }
                    }
                    if (targetContent.Count < 1) 
                    {
                        tbAlert.Text = "Error: target file content is empty or format is not recognized !";
                        ProgressBar.Value = 0;
                        spAlert.Visibility = Visibility.Visible;
                        ProgressBar.Visibility = Visibility.Hidden;
                        Semaphore.Release();
                    }

                    // We also do the same with the file to integrate, however we don't need to use it's content
                    Dictionary<int, string> integrateContent = new Dictionary<int, string>();
                    UpdateProgressBar(30);

                    foreach (string line in File.ReadAllLines(integratedFile))
                    {
                        if (Regex.Match(line, @".*\s=\s[0-9][0-9]*").Success)
                        {
                            string formatedLine = line.Replace(" ", "");
                            string leftside = formatedLine.Substring(0, formatedLine.IndexOf("="));
                            string rightside = Regex.Match(formatedLine.Substring(formatedLine.IndexOf("=") + 1), @"\d+").Value;
                            integrateContent.Add(Int32.Parse(rightside), leftside);
                        }
                    }
                    if (integrateContent.Count < 1)
                    {
                        tbAlert.Text = "Error: integrated file is empty or format is not recognized ! Format: XXXXX = int,";
                        ProgressBar.Value = 0;
                        spAlert.Visibility = Visibility.Visible;
                        ProgressBar.Visibility = Visibility.Hidden;
                        Semaphore.Release();
                        return;
                    }

                    foreach (KeyValuePair<int, string> kvp in integrateContent)
                    {
                        targetContent[kvp.Key] = kvp.Value;
                    }

                    // Here we sort the dictionnary in order to well write lines
                    IOrderedEnumerable<KeyValuePair<int, string>> sortedCollection = targetContent.OrderBy(x => x.Key);

                    // create newfile as string array

                    List<string> newFile = new List<string>();
                    newFile.AddRange(header);
                    UpdateProgressBar(50);

                    foreach (var kvp in sortedCollection)
                    {
                        string lastChar = kvp.Key == sortedCollection.Last().Key ? "" : ",";
                        string line = null;
                        line += new string(' ', spaceMemory);
                        line += kvp.Value + " = " + kvp.Key + lastChar;
                        newFile.Add(line);
                    }

                    newFile.AddRange(footer);

                    // Then we write the newFile content into the target one
                    UpdateProgressBar(75);

                    using (StreamWriter newTask = new StreamWriter(targetFile, false))
                    {
                        foreach (var line in newFile)
                        {
                            newTask.WriteLine(line);
                        }
                    }

                    UpdateProgressBar(100);
                    MessageBox.Show("Success : " + integrateContent.Count + " values added/updated");
                    spAlert.Visibility = Visibility.Hidden;
                    ProgressBar.Value = 0;
                    Semaphore.Release();
                    return;
                }
            }

            tbAlert.Text = "Error: required files are not valid !";
            spAlert.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;
            Semaphore.Release();
        }

        private void UpdateProgressBar(int value)
        {
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = value, DispatcherPriority.Background);
        }
    }
}
