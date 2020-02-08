using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RomSync
{
    public class Rom : INotifyPropertyChanged
    {
        // List of images available to set Icon to. Depends on the extension for the file.
        private readonly static BitmapImage VectrexImage = new BitmapImage(new System.Uri("pack://application:,,,/Resources/vectrex.png"));
        private readonly static BitmapImage UnknownImage = new BitmapImage(new System.Uri("pack://application:,,,/Resources/unknown.png"));
        private readonly static BitmapImage NESImage = new BitmapImage(new System.Uri("pack://application:,,,/Resources/nes.png"));
        private readonly static BitmapImage ZIPImage = new BitmapImage(new System.Uri("pack://application:,,,/Resources/ZIP.png"));
        private readonly static BitmapImage WaitingImage = new BitmapImage(new System.Uri("pack://application:,,,/Resources/waiting.png"));
        private readonly static BitmapImage GreenCheckImage = new BitmapImage(new System.Uri("pack://application:,,,/Resources/greencheck.png"));

        // Information about the Rom - the name, extension, directory, etc. is all in this object.
        public FileInfo FileInfo { get; set; }

        // Returns the Icon of the image, depending on what the file extension is.
        public BitmapImage Icon 
        {
            get
            {
                switch(FileInfo.Extension) 
                {
                    case (".vec"):
                        return VectrexImage;
                    case (".nes"):
                    case (".fds"):
                        return NESImage;
                    case (".zip"):
                        return ZIPImage;
                    default:
                        return UnknownImage;
                }
            }
        }

        // Returns the image that represents the sync status of this Rom.
        public BitmapImage SyncStatusImage
        {

            get
            {
                switch (SyncStatus)
                {
                    case -1:
                        return null;
                    case 0:
                        return WaitingImage;
                    case 1:
                        return GreenCheckImage;
                    default:
                        return null;
                }
            }

        }

        
        private int syncStatus;

        /// <summary>
        /// -1 = unset, 0 = unsynced, 1 = synced
        /// </summary>
        public int SyncStatus
        {
            get 
            {
                return syncStatus;
            }

            set 
            {
                syncStatus = value;
                if (syncStatus < -1 || syncStatus > 1)
                {
                    throw new ArgumentOutOfRangeException();
                }
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  

        // The CallerMemberName attribute that is applied to propertyName  
        // causes the property/method (in this case, property) name of the caller to used as the argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object x)
        {
            return x.GetHashCode() == this.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = 7;
            // Suitable nullity checks etc, of course :)
            hash = hash + FileInfo.Name.GetHashCode();
            return hash;
        }

        public Rom(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            SyncStatus = -1;
        }
    }
}