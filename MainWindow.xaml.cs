using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Application = System.Windows.Application;
using ToastNotifications.Messages;
using ToastNotifications.Core;

namespace RomSync
{
    /*
     * Load roms in input
     * If roms in output
     *      Compare each rom in input to roms in output. 
     *      If rom matches, Rom.HasMatch. Else, Rom.NoMatch. Default: Rom.MatchingNotDone
     *      
     *      User selects multiple roms.
     *      User can then sync those roms with the button. Show a progress bar.
     */


    public partial class MainWindow : Window
    {
        #region Variables

        // Toast notification
        private Notifier notifier = new Notifier(cfg =>
        {
            // Window position
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.BottomRight,
                offsetX: 10,
                offsetY: 10);

            // Time to live
            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(2.5),
                maximumNotificationCount: MaximumNotificationCount.FromCount(1));
                

            // Dispatcher that deploys the notification
            cfg.Dispatcher = Application.Current.Dispatcher;

        });

        private MessageOptions messageOptions = new MessageOptions
        {
            NotificationClickAction = n => // set the callback for notification click event
            {
                n.Close();
            }
        };




        public string InputFileDirectory 
        {
            get { return (string)this.GetValue(InputFileDirectoryProperty); }
            set { this.SetValue(InputFileDirectoryProperty, value); }
        }
        public static readonly DependencyProperty InputFileDirectoryProperty 
            = DependencyProperty.Register("InputFileDirectory", typeof(string), typeof(MainWindow), new PropertyMetadata());

        public string OutputFileDirectory
        {
            get { return (string)this.GetValue(OutputFileDirectoryProperty); }
            set { this.SetValue(OutputFileDirectoryProperty, value); }
        }
        public static readonly DependencyProperty OutputFileDirectoryProperty 
            = DependencyProperty.Register("OutputFileDirectory", typeof(string), typeof(MainWindow), new PropertyMetadata());

        // Bound to ListViews
        public ObservableCollection<Rom> InputRoms { get; set; }        
        public ObservableCollection<Rom> OutputRoms { get; set; }

        #endregion

        public MainWindow()
        {
            // Initialize values
            InputFileDirectory = Properties.Settings.Default.InputPath; 
            OutputFileDirectory = Properties.Settings.Default.OutputPath;
            InputRoms = new ObservableCollection<Rom>();
            OutputRoms = new ObservableCollection<Rom>();
            InitializeComponent();

            bool inputIsBlank = string.IsNullOrEmpty(InputFileDirectory);
            bool outputIsBlank = string.IsNullOrEmpty(InputFileDirectory);

            

            // Directories aren't blank? Then load the roms.
            if (!inputIsBlank)
            {
                populateRomList(InputFileDirectory, true);
            }

            if (!outputIsBlank)
            {
                populateRomList(OutputFileDirectory, false);
            }

            // Only run if both are true.
            if (! inputIsBlank && ! outputIsBlank)
            {
                identifyMatchingRoms();
            }
                
        }

        private void populateRomList(string directory, bool useInputRoms)
        {
            // Clear applicable rom list
            if (useInputRoms)
            {
                InputRoms.Clear();
                Debug.WriteLine("Cleared input roms.");
                FileInfo[] fileInfos = new DirectoryInfo(directory).GetFiles();
                foreach (FileInfo f in fileInfos)
                {
                    InputRoms.Add(new Rom(f));
                }

                Debug.WriteLine("Input roms were added. A total of " + InputRoms.Count);

            } else
            {
                OutputRoms.Clear();
                Debug.WriteLine("Cleared output roms.");
                FileInfo[] fileInfos = new DirectoryInfo(directory).GetFiles();
                foreach (FileInfo f in fileInfos)
                {
                    OutputRoms.Add(new Rom(f));
                }

                Debug.WriteLine("Output roms were added. A total of " + OutputRoms.Count);
            }
        }

        private void identifyMatchingRoms()
        {
            // Reset the sync status for both input and output roms - when you sync one rom, you have to update both sides.
            foreach (Rom r in InputRoms)
            {
                r.SyncStatus = 0;
            }

            foreach (Rom r in OutputRoms)
            {
                r.SyncStatus = 0;
            }

            // Find input roms that match output roms
            IEnumerable<Rom> inputRomsMatchingOutputRoms = InputRoms.Intersect<Rom>(OutputRoms);

            foreach(Rom match in inputRomsMatchingOutputRoms)
            {
                match.SyncStatus = 1;
            }

            // Find output roms that match input roms (remember - these two lists are separate.)
            IEnumerable<Rom> outputRomsMatchingInputRoms = OutputRoms.Intersect<Rom>(InputRoms);

            foreach (Rom match in outputRomsMatchingInputRoms)
            {
                match.SyncStatus = 1;
            }

        }

        private void SelectInputPathButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult dr = folderBrowserDialog.ShowDialog();

            if(dr == System.Windows.Forms.DialogResult.OK)
            {
                InputFileDirectory = folderBrowserDialog.SelectedPath;
                populateRomList(InputFileDirectory, true);
                identifyMatchingRoms();
            }
        }

        private void SelectOutputPathButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult dr = folderBrowserDialog.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                OutputFileDirectory = folderBrowserDialog.SelectedPath;
                populateRomList(OutputFileDirectory, false);
                identifyMatchingRoms();
            }
        }

        // InputRoms ListView has been clicked. 
        private void InputRomsListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool enableButton = false;

            if (InputRomsListView.SelectedItems != null && InputRomsListView.SelectedItems.Count > 0)
            {
                foreach (Rom r in InputRomsListView.SelectedItems)
                {
                    // Unsynced rom in the list?
                    if (r.SyncStatus == 0)
                    {
                        // Enable the button.
                        enableButton = true;
                    }
                }
            }

            SyncSelectedButton.IsEnabled = enableButton;

        }

        private void SyncSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            int errorcount = 0;
            ArrayList roms = new ArrayList(InputRomsListView.SelectedItems);
            foreach (Rom r in roms)
            {
                try
                {
                    File.Copy(r.FileInfo.FullName, OutputFileDirectory + "\\" + r.FileInfo.Name, false);
                }
                catch (UnauthorizedAccessException)
                {
                    ShowError("Can't access the file \"" + r.FileInfo.Name + "\" - this program doesn't have permission. Try running as administrator?");
                    errorcount++;
                }
                catch(DirectoryNotFoundException)
                {
                    ShowError("Wasn't able to copy to that directory - it doesn't exist, at least this program thinks it doesn't. " +
                        "Did you remove the storage media or did the network drive lose connection?");
                    break;
                }
                catch(FileNotFoundException)
                {
                    ShowError("\"" + r.FileInfo.Name + "\" wasn't found. This program couldn't copy it. It may have been removed suddenly, the network drive" +
                        "could have lost connection, or the storage media was suddenly removed.");
                }
                catch(IOException) 
                { 
                    // just ignore this one 
                }
            }

            if (errorcount < 1)
            {
                notifier.ShowSuccess("Sync complete!", messageOptions);
            } else
            {
                notifier.ShowWarning("Sync either partially complete or failed.", messageOptions);
            }

            populateRomList(OutputFileDirectory, false);
            populateRomList(InputFileDirectory, true);
            identifyMatchingRoms();

        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "RomSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the selected ROMs from the output device?", "RomSync Message", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                int errorcount = 0;
                ArrayList roms = new ArrayList(OutputRomsListView.SelectedItems);
                foreach (Rom r in roms)
                {
                    try
                    {
                        File.Delete(r.FileInfo.FullName);
                    } 
                    catch(UnauthorizedAccessException)
                    {
                        ShowError("Can't delete the file \"" + r.FileInfo.Name + "\" - this program doesn't have permission. Try running as administrator?");
                        errorcount++;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        ShowError("Wasn't able to delete from that directory - it doesn't exist, at least this program thinks it doesn't. " +
                            "Did you remove the storage media or did the network drive lose connection?");
                        break;
                    }

                }

                if (errorcount < 1)
                {
                    notifier.ShowSuccess("File deletion complete.");
                }
                else
                {
                    notifier.ShowWarning("Delete either partially complete or failed.");
                }
                populateRomList(OutputFileDirectory, false);
                populateRomList(InputFileDirectory, true);
                identifyMatchingRoms();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save paths before exit
            Properties.Settings.Default.InputPath = InputFileDirectory;
            Properties.Settings.Default.OutputPath = OutputFileDirectory;
            Properties.Settings.Default.Save();
        }
    }
}
