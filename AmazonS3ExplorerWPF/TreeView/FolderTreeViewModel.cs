using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.ComponentModel;


namespace AmazonS3ExplorerWPF
{
   public  class FolderTreeViewModel : INotifyPropertyChanged
    {
        private FolderTreeViewModel _parentFolder;
        private SortableObservableCollection<FolderTreeViewModel> _subFolders;

        private string _displayName;
        private string _prefix;
        private List<AmazonS3FileObject> _children;
        private bool _refreshOnSelect = false;

        public string Prefix
        {
            get { return _prefix; }
        }
        

        #region View
        private bool _isSelected;
        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion

        public FolderTreeViewModel ParentFolder
        {
            get { return _parentFolder; }
            set { _parentFolder = value; }
        }

        public SortableObservableCollection<FolderTreeViewModel> SubFolders
        {
            get { return _subFolders; }
        }

        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        public List<AmazonS3FileObject> Children
        {
            get
            {
                return _children;
            }
        }

        public bool RefreshOnSelect
        {
            get
            {
                return _refreshOnSelect;
            }

            set
            {
                _refreshOnSelect = value;
            }
        }

        public FolderTreeViewModel(string prefix, string displayName)
        {
            _prefix = prefix;
            _displayName = displayName;
            _children = new List<AmazonS3FileObject>();

            _subFolders = new SortableObservableCollection<FolderTreeViewModel>();
            IsExpanded = true;
        }
        public void AddSubfolder(FolderTreeViewModel subFolderToAdd)
        {
            if (_subFolders.SingleOrDefault(p => p.Prefix == subFolderToAdd.Prefix) == null)
            {
                _subFolders.Add(subFolderToAdd);


                this.OnPropertyChanged("SubFolders");
            }
        }
        public void RemoveSubfolder(FolderTreeViewModel subFolderToRemove)
        {
            if (_subFolders.SingleOrDefault(p => p.Prefix == subFolderToRemove.Prefix) != null)
            {
                _subFolders.Remove(subFolderToRemove);
                this.OnPropertyChanged("SubFolders");
            }
        }

        public void ClearSubfolders()
        {
            _subFolders.Clear();
            this.OnPropertyChanged("SubFolders");

        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


    }
    public class AmazonS3FileObject
    {
        public S3Object FileObject { get; set; }
        public string Filename { get; set; }
    }
}
