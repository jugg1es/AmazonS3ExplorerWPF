using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;


namespace AmazonS3ExplorerWPF.Controls
{
    /// <summary>
    /// Interaction logic for AmazonFileManagerControl.xaml
    /// </summary>
    public partial class AmazonFileManagerControl : UserControl
    {
        private string InvalidCharacters = "&@:,\\<>^";
        private string _selectedBucket;
        private AmazonS3Service _amazon;
        private string _bucketSubFolder = string.Empty;
        private bool _cancelOperation = false;
        private Queue<GetRequestDescription> _getBucketQueue = new Queue<GetRequestDescription>();




        #region DPs
        public bool IsWorking
        {
            get { return (bool)this.GetValue(IsWorkingProperty); }
            set { this.SetValue(IsWorkingProperty, value); }
        }
        public static readonly DependencyProperty IsWorkingProperty = DependencyProperty.Register(
          "IsWorking", typeof(bool), typeof(AmazonFileManagerControl),
          new PropertyMetadata(false));

        public SortableObservableCollection<FolderTreeViewModel> RemoteFolders
        {
            get { return (SortableObservableCollection<FolderTreeViewModel>)this.GetValue(RemoteFoldersProperty); }
            set { this.SetValue(RemoteFoldersProperty, value); }
        }
        public static readonly DependencyProperty RemoteFoldersProperty = DependencyProperty.Register(
            "RemoteFolders", typeof(SortableObservableCollection<FolderTreeViewModel>), typeof(AmazonFileManagerControl),
            new PropertyMetadata(new SortableObservableCollection<FolderTreeViewModel>()));

        #endregion

        


        public AmazonFileManagerControl()
        {
            InitializeComponent();
        }

        public void LoadBucketContents(AmazonS3Service amazonClient, string bucket)
        {
            _amazon = amazonClient;
            _amazon.FileProgressChanged -= _amazon_FileProgressChanged;
            _amazon.FileProgressChanged += _amazon_FileProgressChanged;
            _selectedBucket = bucket;
            _amazon.CurrentBucketName = _selectedBucket;
            GetBucketContents();
        }

        private void _amazon_FileProgressChanged(string remoteFilename, long currentBytes, long totalBytes, int percentDone)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => ctrlLoading.AdornerSubProgress = percentDone));
        }

        private void ctrlLoading_CancelOperation()
        {
            if (_amazon != null)
                _amazon.CancelOperation();
            _cancelOperation = true;
        }

        #region Tree Operations
        private void AmazonFolderTree_SelectedItemChanged(object sender, RoutedTreeItemEventArgs<FolderTreeViewModel> e)
        {
            BindFileList();
        }

        private void RefreshFolder_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelected() == null) return;

            GetBucketContents();

        }

        private FolderTreeViewModel GetSelected()
        {
            if (FolderTree.SelectedItem != null)
            {
                return (FolderTreeViewModel)FolderTree.SelectedItem;
            }
            return null;
        }
        private string GetInputString(string title, string oldString)
        {
            TextEditWindow winEdit = new TextEditWindow(title, oldString);
            winEdit.Owner = Window.GetWindow(this);
            if (winEdit.ShowDialog() == true)
            {
                return winEdit.TextValue;
            }
            return null;
        }

        private bool IsValidName(string name, bool isFolder)
        {
            if (isFolder)
            {
                if (name.Contains(" "))
                {
                    MessageBox.Show(Window.GetWindow(this), "Cannot use a space in folder names", "Invalid");
                    return false;
                }
            }

            for (int i = 0; i < InvalidCharacters.Length; i++)
            {
                if (name.IndexOf(InvalidCharacters[i].ToString()) != -1)
                {
                    MessageBox.Show(Window.GetWindow(this), "Invalid character: " + InvalidCharacters[i].ToString(), "Invalid");
                    return false;
                }
            }
            return true;
        }

        private void EnqueueGetRequest(FolderTreeViewModel selectedToRefresh, bool autoSelect = true)
        {
            _getBucketQueue.Enqueue(new GetRequestDescription()
            {
                ItemToRefresh = selectedToRefresh,
                AutoSelect = autoSelect
            });

        }
        private void ProcessNextGetRequest()
        {
            if (_getBucketQueue.Count == 0) return;

            GetRequestDescription nextRequest = _getBucketQueue.Dequeue();
            GetBucketContents(nextRequest.ItemToRefresh, nextRequest.AutoSelect);
        }



        private void GetBucketContents(FolderTreeViewModel prevSelected = null, bool autoSelect = true)
        {
            string prefixToGet = null;
            if (prevSelected != null) prefixToGet = prevSelected.Prefix;            
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                if (string.IsNullOrEmpty(prefixToGet))
                {

                    List<S3Object> objects = _amazon.GetObjectsInBucket(_bucketSubFolder);
                    FolderTreeViewModel node = new FolderTreeViewModel("", _selectedBucket);
                    GenerateFolderHierarchy(node, objects);
                    e.Result = node;
                }
                else
                {
                    if (prefixToGet[prefixToGet.Length - 1] != '/') prefixToGet += "/";
                    List<S3Object> objects = _amazon.GetObjectsInBucket(prefixToGet);
                    e.Result = objects;
                }


            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                ctrlLoading.AdornerShowsCancel = false;
                this.IsWorking = false;
                if (e.Error == null)
                {
                    if (e.Result is FolderTreeViewModel)
                    {
                        FolderTreeViewModel rootNode = (FolderTreeViewModel)e.Result;
                        if (string.IsNullOrEmpty(_bucketSubFolder))
                        {
                            this.RemoteFolders = new SortableObservableCollection<FolderTreeViewModel>() { rootNode };
                            FolderTree.SelectedItem = rootNode;
                            rootNode.IsExpanded = true;
                            FolderTree.Refresh();
                        }
                        else
                        {
                            this.RemoteFolders = new SortableObservableCollection<FolderTreeViewModel>() { rootNode.SubFolders[0] };
                            rootNode.SubFolders[0].ParentFolder = null;
                            FolderTree.SelectedItem = rootNode.SubFolders[0];
                            rootNode.SubFolders[0].IsExpanded = true;
                            FolderTree.Refresh();
                        }


                    }
                    else if (e.Result is List<S3Object> && prevSelected != null)
                    {
                        prevSelected.ClearSubfolders();
                        prevSelected.Children.Clear();
                        GenerateFolderHierarchy(prevSelected, (List<S3Object>)e.Result);
                        FolderTree.Refresh();
                        if (autoSelect)
                        {
                            FolderTree.SelectedItem = prevSelected;
                            TreeViewItem item = FolderTree.TryFindNode(prevSelected);
                            item.IsExpanded = true;
                            item.IsSelected = true;
                        }
                    }
                    BindFileList();
                    ProcessNextGetRequest();
                }
                else
                {
                    MessageBox.Show(Window.GetWindow(this), "Error occured: " + e.Error.Message, "Error");
                }
            };
            worker.RunWorkerAsync();
            ctrlLoading.AdornerShowsCancel = true;
            this.IsWorking = true;

        }
        private void BindFileList()
        {
            listFiles.ItemsSource = null;
            if (FolderTree.SelectedItem != null)
            {
                listFiles.IsEnabled = true;
                listFiles.ItemsSource = FolderTree.SelectedItem.Children;

                if (FolderTree.SelectedItem.RefreshOnSelect == true)
                {
                    GetBucketContents(FolderTree.SelectedItem);
                    FolderTree.SelectedItem.RefreshOnSelect = false;
                }
            }
            else
            {
                listFiles.IsEnabled = false;
            }
        }


        private void GenerateFolderHierarchy(FolderTreeViewModel currentNode, List<S3Object> folderContents)
        {
            List<string> addedPrefixes = new List<string>();
            foreach (var obj in folderContents)
            {
                string relevantKey = obj.Key;
                if (string.IsNullOrEmpty(currentNode.Prefix) == false)
                    relevantKey = obj.Key.Substring(currentNode.Prefix.Length + 1);
                if (string.IsNullOrEmpty(relevantKey)) continue;
                if (relevantKey.Contains('/'))
                {
                    string prefix = relevantKey.Substring(0, relevantKey.IndexOf('/'));
                    if (string.IsNullOrEmpty(prefix)) continue;
                    if (addedPrefixes.Contains(prefix) == false)
                        addedPrefixes.Add(prefix);
                }
                else
                    currentNode.Children.Add(new AmazonS3FileObject()
                    {
                        Filename = relevantKey,
                        FileObject = obj
                    });
            }

            foreach (var str in addedPrefixes)
            {
                string displayName = str;
                int lastIndex = str.LastIndexOf('/');
                if (lastIndex > 0)
                    displayName = str.Substring(0, lastIndex);
                string fullPrefix = str;
                if (string.IsNullOrEmpty(currentNode.Prefix) == false)
                {
                    fullPrefix = currentNode.Prefix + "/" + str;
                }
                FolderTreeViewModel subFolder = new FolderTreeViewModel(fullPrefix, displayName);
                currentNode.AddSubfolder(subFolder);
                subFolder.ParentFolder = currentNode;
                List<S3Object> subFolderItems = folderContents.Where(p => p.Key.IndexOf(fullPrefix + "/") == 0).ToList();
                GenerateFolderHierarchy(subFolder, subFolderItems);
            }

        }


        private void TreeCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FolderTreeViewModel item = FolderTree.SelectedItem;
            switch (((RoutedUICommand)e.Command).Name)
            {
                case "NewFolder":
                    NewFolder(item);
                    break;
                case "RenameFolder":
                    RenameFolder(item);
                    break;
                case "DownloadFolder":
                    DownloadFolder(item);
                    break;
                case "DeleteFolder":
                    DeleteFolder(item);
                    break;
            }
        }

        private void TreeCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (((RoutedUICommand)e.Command).Name == "RenameFolder" || ((RoutedUICommand)e.Command).Name == "DeleteFolder")
            {
                FolderTreeViewModel item = FolderTree.SelectedItem;
                if (string.IsNullOrEmpty(item.Prefix))
                {
                    e.CanExecute = false;
                    return;
                }
            }


            e.CanExecute = true;
        }
        private void DownloadFolder(FolderTreeViewModel item)
        {
            if (item == null) return;
            if (MessageBox.Show(Window.GetWindow(this), "Do you want to download this entire folder?",
                  "Download folder?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var openFolder = new CommonOpenFileDialog();

                openFolder.AllowNonFileSystemItems = true;
                openFolder.Multiselect = false;
                openFolder.IsFolderPicker = true;
                openFolder.Title = "Select target folder";

                if (openFolder.ShowDialog(Window.GetWindow(this)) != CommonFileDialogResult.Ok) return;

                string targetDir = openFolder.FileNames.ToArray()[0];
                string downloadLocation = targetDir;

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    string remotePath = item.Prefix;
                    if (string.IsNullOrEmpty(remotePath) == false) remotePath = item.Prefix + "/";

                    List<S3Object> itemsToDownload = _amazon.GetObjectsInBucket(remotePath);

                    int currentCount = 0;
                    ParallelOptions options = new ParallelOptions();
                    options.MaxDegreeOfParallelism = 2;
                    System.Threading.Tasks.Parallel.ForEach(itemsToDownload, options, toDownload =>
                    {
                        if (_cancelOperation) return;
                        currentCount++;
                        int percentDone = (int)Math.Round(((double)(currentCount) / (double)itemsToDownload.Count) * 100, 0);
                        worker.ReportProgress(percentDone);
                        if (toDownload.Size > 0)
                        {
                            string relativeKey = toDownload.Key.Substring(remotePath.Length);
                            DirectoryInfo targetSaveFolder = new DirectoryInfo(downloadLocation);
                            List<string> dirParts = relativeKey.Split('/').ToList();
                            for (var i = 0; i < dirParts.Count - 1; i++)
                            {
                                var d = dirParts[i];
                                if (string.IsNullOrEmpty(d) == false)
                                    targetSaveFolder = targetSaveFolder.CreateSubdirectory(d);
                            }

                            string localFile = System.IO.Path.Combine(targetSaveFolder.FullName, dirParts[dirParts.Count - 1]);
                            _amazon.DownloadFile(toDownload.Key, localFile);
                        }
                    });

                    _cancelOperation = false;
                };
                worker.RunWorkerCompleted += (s, e) =>
                {
                    ctrlLoading.AdornerShowsProgress = false;
                    ctrlLoading.AdornerShowsCancel = false;
                    this.IsWorking = false;
                    if (e.Error == null)
                    {
                        MessageBox.Show(Window.GetWindow(this), "Download Complete", "Complete");
                    }
                    else
                    {
                        MessageBox.Show(Window.GetWindow(this), "Error occured: " + e.Error.Message, "Error");
                    }
                };
                worker.WorkerReportsProgress = true;
                worker.ProgressChanged += (s, e) =>
                {
                    ctrlLoading.AdornerProgress = e.ProgressPercentage;
                };
                worker.RunWorkerAsync();
                ctrlLoading.AdornerShowsProgress = true;
                ctrlLoading.AdornerShowsCancel = true;
                this.IsWorking = true;
            }
        }

        private void NewFolder(FolderTreeViewModel item)
        {
            string folderName = GetInputString("New Folder", "");
            if (folderName == null) return;
            if (MessageBox.Show(Window.GetWindow(this), "Are you sure you want to create a new folder under the selected node?",
               "Create folder?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            string remotePath = string.IsNullOrEmpty(item.Prefix) ? item.Prefix + folderName : item.Prefix + "/" + folderName;

            if (IsValidName(remotePath, true) == false) return;
            _amazon.CreateFolder(remotePath);
            GetBucketContents(item);

        }
        private void RenameFolder(FolderTreeViewModel item)
        {
            if (string.IsNullOrEmpty(item.Prefix))
            {
                MessageBox.Show(Window.GetWindow(this), "Cannot rename root node", "Error");
                return;
            }

            string newName = GetInputString("Rename", item.DisplayName);
            if (newName == null) return;

            string truncatedPrefix = item.Prefix;
            if (truncatedPrefix.Contains("/")) truncatedPrefix = truncatedPrefix.Substring(0, truncatedPrefix.LastIndexOf('/'));

            string remotePath = string.IsNullOrEmpty(truncatedPrefix) ? truncatedPrefix + newName : truncatedPrefix + "/" + newName;

            if (IsValidName(remotePath, true) == false) return;

            if (MessageBox.Show(Window.GetWindow(this), "Are you sure you want to rename this folder?",
               "Rename folder?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            doFolderRename(item, remotePath);
        }
        private void doFolderRename(FolderTreeViewModel item, string newName)
        {
            string originalPrefix = item.Prefix[item.Prefix.Length - 1] != '/' ? item.Prefix + "/" : item.Prefix;
            string newPrefix = newName[newName.Length - 1] != '/' ? newName + "/" : newName;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                List<S3Object> objects = _amazon.GetObjectsInBucket(originalPrefix);

                foreach (var toCopy in objects)
                {
                    string newObjectKey = newPrefix + toCopy.Key.Substring(originalPrefix.Length);
                    _amazon.CopyFile(toCopy.Key, newObjectKey);
                    _amazon.DeleteFile(toCopy.Key);
                }
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                this.IsWorking = false;
                if (e.Error == null)
                {
                    FolderTreeViewModel parentFolder = FolderTree.GetParentItem(item);
                    if (parentFolder != null)
                        GetBucketContents(parentFolder);
                    else
                        GetBucketContents(null);
                }
                else
                    MessageBox.Show(Window.GetWindow(this), "Error occured: " + e.Error.Message, "Error");

            };
            worker.RunWorkerAsync();
            this.IsWorking = true;

        }


        private void DeleteFolder(FolderTreeViewModel item)
        {
            if (MessageBox.Show(Window.GetWindow(this), "Are you sure you want to delete this folder and everything it contains?",
              "Delete folder?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (MessageBox.Show(Window.GetWindow(this), "Are you SURE?",
             "Delete folder?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _amazon.DeleteFolder(item.Prefix);
            FolderTreeViewModel parentFolder = FolderTree.GetParentItem(item);
            if (parentFolder != null)
                GetBucketContents(parentFolder);
            else
                GetBucketContents(null);
        }
        #endregion

        #region File List Operations
        private void ListFiles_Drop(object sender, DragEventArgs e)
        {

            if (RemoteFolders.Count == 0 || GetSelected() == null) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<AmazonUploadObject> itemsToUpload = GetUploadObjects(GetSelected(), items.ToList());
                UploadFiles(GetSelected(), itemsToUpload);
            }
        }

        private List<AmazonUploadObject> GetUploadObjects(FolderTreeViewModel f, List<string> items)
        {
            List<AmazonUploadObject> toUpload = new List<AmazonUploadObject>();

            foreach (string str in items)
            {
                if (File.Exists(str))
                {
                    FileInfo fInfo = new FileInfo(str);
                    AmazonUploadObject obj = new AmazonUploadObject()
                    {
                        LocalPath = str,
                        RemotePath = f.Prefix + "/" + fInfo.Name
                    };
                    toUpload.Add(obj);
                }
                else if (Directory.Exists(str))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(str);
                    string parentPath = string.IsNullOrEmpty(f.Prefix) ? f.Prefix + dInfo.Name + "/" : f.Prefix + "/" + dInfo.Name + "/";
                    toUpload.AddRange(CompileUploadObjects(parentPath, str));
                }
            }

            return toUpload;
        }
        private List<AmazonUploadObject> CompileUploadObjects(string remotePathParent, string directory)
        {
            List<AmazonUploadObject> currentList = new System.Collections.Generic.List<AmazonUploadObject>();
            foreach (string file in Directory.GetFiles(directory))
            {
                FileInfo fInfo = new FileInfo(file);
                currentList.Add(new AmazonUploadObject()
                {
                    LocalPath = file,
                    RemotePath = remotePathParent + fInfo.Name
                });
            }

            foreach (string dir in Directory.GetDirectories(directory))
            {
                DirectoryInfo dInfo = new DirectoryInfo(dir);
                string parentPath = remotePathParent + dInfo.Name + "/";
                currentList.AddRange(CompileUploadObjects(parentPath, dir));
            }
            return currentList;
        }

        private void UploadFiles(FolderTreeViewModel item, List<AmazonUploadObject> itemsToUpload)
        {
            if (item == null) return;
            if (itemsToUpload == null || itemsToUpload.Count == 0) return;

            if (MessageBox.Show(Window.GetWindow(this), string.Format("Do you want to upload ({0}) files to {1}?",
                itemsToUpload.Count, item.DisplayName),
                "Upload file(s)?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                int currentCount = 0;
                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = 2;
                System.Threading.Tasks.Parallel.ForEach(itemsToUpload, options, uploadItem =>
                {
                    if (_cancelOperation) return;
                    currentCount++;
                    int percentDone = (int)Math.Round(((double)(currentCount) / (double)itemsToUpload.Count) * 100, 0);
                    worker.ReportProgress(percentDone);
                    _amazon.UploadFile(uploadItem.RemotePath, uploadItem.LocalPath);
                });
                _cancelOperation = false;

            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                ctrlLoading.AdornerShowsProgress = false;
                ctrlLoading.AdornerShowsCancel = false;
                this.IsWorking = false;
                if (e.Error == null)
                {
                    GetBucketContents(item);
                }
                else
                {
                    MessageBox.Show(Window.GetWindow(this), "Error occured: " + e.Error.Message, "Error");
                }
            };
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (s, e) =>
            {
                ctrlLoading.AdornerProgress = e.ProgressPercentage;
            };
            worker.RunWorkerAsync();
            ctrlLoading.AdornerShowsProgress = true;
            ctrlLoading.AdornerShowsCancel = true;
            this.IsWorking = true;


        }

        private void DownloadFiles(List<AmazonS3FileObject> filesToDownload)
        {
            if (MessageBox.Show(Window.GetWindow(this), "Do you want to download the selected file(s)?",
                  "Download file(s)?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            var openFolder = new CommonOpenFileDialog();
            openFolder.AllowNonFileSystemItems = true;
            openFolder.Multiselect = false;
            openFolder.IsFolderPicker = true;
            openFolder.Title = "Select target folder";

            if (openFolder.ShowDialog(Window.GetWindow(this)) != CommonFileDialogResult.Ok) return;

            string targetDir = openFolder.FileNames.ToArray()[0];
            string downloadLocation = targetDir;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                int currentCount = 0;
                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = 2;
                System.Threading.Tasks.Parallel.ForEach(filesToDownload, options, toDownload =>
                {
                    if (_cancelOperation) return;
                    currentCount++;
                    int percentDone = (int)Math.Round(((double)(currentCount) / (double)filesToDownload.Count) * 100, 0);
                    worker.ReportProgress(percentDone);
                    if (toDownload.FileObject.Size > 0)
                    {
                        string localPath = System.IO.Path.Combine(downloadLocation, toDownload.Filename);
                        _amazon.DownloadFile(toDownload.FileObject.Key, localPath);
                    }
                });

                _cancelOperation = false;
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                ctrlLoading.AdornerShowsProgress = false;
                ctrlLoading.AdornerShowsCancel = false;
                this.IsWorking = false;
                if (e.Error == null)
                {
                    MessageBox.Show(Window.GetWindow(this), "Download Complete", "Complete");
                }
                else
                {
                    MessageBox.Show(Window.GetWindow(this), "Error occured: " + e.Error.Message, "Error");
                }
            };
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (s, e) =>
            {
                ctrlLoading.AdornerProgress = e.ProgressPercentage;
            };
            worker.RunWorkerAsync();
            ctrlLoading.AdornerShowsProgress = true;
            ctrlLoading.AdornerShowsCancel = true;
            this.IsWorking = true;


        }


        private void RenameFile(AmazonS3FileObject file)
        {
            string newName = GetInputString("Rename", file.Filename);
            if (newName == null) return;
            string newPath = newName;
            if (file.FileObject.Key.Contains('/'))
            {
                newPath = file.FileObject.Key.Substring(0, file.FileObject.Key.Length - file.Filename.Length) + newName;
            }

            if (IsValidName(newPath, true) == false) return;

            if (MessageBox.Show(Window.GetWindow(this), "Are you sure you want to rename this file?",
               "Rename file?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _amazon.CopyFile(file.FileObject.Key, newPath);
            _amazon.DeleteFile(file.FileObject.Key);

            GetBucketContents(FolderTree.SelectedItem);
        }

        private void ViewFile(AmazonS3FileObject file)
        {
            string downloadLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), file.Filename);

            try
            {
                _amazon.DownloadFile(file.FileObject.Key, downloadLocation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Window.GetWindow(this), "There was an error downloading the file: " + ex.Message.ToString(), "Error");
                return;
            }


            bool askToSave = true;
            Process p = new Process();
            p.StartInfo.FileName = downloadLocation;
            p.Start();
            try
            {

                p.WaitForExit();
            }
            catch (Exception ex) { askToSave = false; }

            if (askToSave)
            {
                if (MessageBox.Show(Window.GetWindow(this), "Do you want to reupload this file?", "Upload change?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _amazon.UploadFile(file.FileObject.Key, downloadLocation);
                    GetBucketContents(FolderTree.SelectedItem);
                }

            }


        }
     
        private void DeleteFiles(List<AmazonS3FileObject> files)
        {

            if (MessageBox.Show(Window.GetWindow(this), "Are you sure you want to delete these file(s)?", "Delete File(s)?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var f in files)
                {
                    _amazon.DeleteFile(f.FileObject.Key);

                }
                GetBucketContents(FolderTree.SelectedItem);
            }


        }


        private void FileListCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (listFiles.SelectedItem == null)
            {
                e.CanExecute = false;
                return;
            }
           
            if (((RoutedUICommand)e.Command).Name == "ViewFile" && listFiles.SelectedItems.Count != 1)
            {
                e.CanExecute = false;
                return;
            }
            if (((RoutedUICommand)e.Command).Name == "RenameFile" && listFiles.SelectedItems.Count != 1)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }
        private void FileListCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
           List<AmazonS3FileObject> items = new List<AmazonS3FileObject>();
            foreach (var i in listFiles.SelectedItems)
            {
                items.Add((AmazonS3FileObject)i);
            }

            switch (((RoutedUICommand)e.Command).Name)
            {
                case "DownloadFile":
                    DownloadFiles(items);
                    break;
                case "ViewFile":
                    ViewFile(items[0]);
                    break;
               
                case "DeleteFile":
                    DeleteFiles(items);
                    break;
                case "RenameFile":
                    RenameFile(items[0]);
                    break;
            }
        }

        #endregion
        private void CopyAmazonFile(FolderTreeViewModel source, List<AmazonS3FileObject> items, bool isCutting = false)
        {
            List<string> itemKeys = new List<string>();
            foreach (var f in items)
            {
                itemKeys.Add(f.FileObject.Key);
            }
            CopyAmazonStructure structure = new CopyAmazonStructure()
            {
                SourceParentPrefix = source.ParentFolder != null ? source.ParentFolder.Prefix : string.Empty,
                SourcePrefix = source.Prefix,
                Cutting = isCutting,
                OriginalKeysToCopy = itemKeys
            };
            Clipboard.SetData("amazonClipboardData", JsonConvert.SerializeObject(structure));
        }
        private void CopyAmazonFolder(FolderTreeViewModel item, bool isCutting = false)
        {
            CopyAmazonStructure structure = new CopyAmazonStructure()
            {
                SourceParentPrefix = item.ParentFolder != null ? item.ParentFolder.Prefix : string.Empty,
                SourcePrefix = item.Prefix,
                Cutting = isCutting,
                FolderPrefix = item.Prefix
            };
            Clipboard.SetData("amazonClipboardData", JsonConvert.SerializeObject(structure));
        }
        private void GetAllItemsUnderPrefix(FolderTreeViewModel node, string prefix, List<string> foundKeys)
        {
            if ((node.Prefix + "/").IndexOf(prefix) == 0)
            {
                foreach (var c in node.Children)
                {
                    foundKeys.Add(c.FileObject.Key);
                }
            }
            foreach (var n in node.SubFolders)
            {
                GetAllItemsUnderPrefix(n, prefix, foundKeys);
            }
        }
        private FolderTreeViewModel FindNodeByPrefix(FolderTreeViewModel node, string prefix)
        {
            if ((node.Prefix + "/").Equals(prefix + "/"))
            {
                return node;
            }
            foreach (var n in node.SubFolders)
            {
                FolderTreeViewModel foundNode = FindNodeByPrefix(n, prefix);
                if (foundNode != null)
                    return foundNode;
            }
            return null;
        }
        private List<SourceDestinationData> CompilePasteOperations(CopyAmazonStructure data, FolderTreeViewModel item)
        {
            string newPrefix = item.Prefix;

            List<SourceDestinationData> operationData = new List<SourceDestinationData>();

            List<string> keysInFolder = new List<string>();
            if (data.FolderPrefix != null)
            {
                FolderTreeViewModel node = RemoteFolders[0];
                GetAllItemsUnderPrefix(node, data.FolderPrefix + "/", keysInFolder);
                foreach (string fullKey in keysInFolder)
                {
                    string relativePath = fullKey.Substring(data.FolderPrefix.LastIndexOf('/') + 1);
                    operationData.Add(new SourceDestinationData()
                    {
                        Source = fullKey,
                        Destination = newPrefix + "/" + relativePath
                    });
                }
            }
            else
            {
                foreach (var key in data.OriginalKeysToCopy)
                {
                    string filename = key;
                    if (key.Contains("/"))
                        filename = key.Substring(key.LastIndexOf('/') + 1);
                    string newObjectKey = newPrefix + "/" + filename;
                    operationData.Add(new SourceDestinationData()
                    {
                        Source = key,
                        Destination = newObjectKey
                    });
                }
            }
            return operationData;

        }
        private void PasteClipboardFiles(FolderTreeViewModel item)
        {
            if (Clipboard.ContainsFileDropList())
            {
                List<string> copiedFiles = new List<string>();
                foreach (var f in Clipboard.GetFileDropList())
                {
                    copiedFiles.Add(f);
                }
                List<AmazonUploadObject> itemsToUpload = GetUploadObjects(GetSelected(), copiedFiles);
                UploadFiles(GetSelected(), itemsToUpload);
            }
        }
        private void PasteAmazonObject(FolderTreeViewModel item)
        {
            if (Clipboard.ContainsData("amazonClipboardData"))
            {
                CopyAmazonStructure data = JsonConvert.DeserializeObject<CopyAmazonStructure>(Clipboard.GetData("amazonClipboardData").ToString());

                if (data.FolderPrefix != null && data.Cutting == true)
                {
                    if ((item.Prefix + "/").IndexOf(data.SourcePrefix + "/") == 0)
                    {
                        MessageBox.Show(Window.GetWindow(this), "Target is same as source", "Error");
                        return;
                    }

                    if (string.IsNullOrEmpty(data.SourceParentPrefix) == false && data.SourceParentPrefix.Equals(item.Prefix))
                    {
                        MessageBox.Show(Window.GetWindow(this), "Target is same as source", "Error");
                        return;
                    }

                    if (item.Prefix.Equals(data.SourcePrefix))
                    {
                        MessageBox.Show(Window.GetWindow(this), "Target is same as source", "Error");
                        return;
                    }
                }

                if (MessageBox.Show(Window.GetWindow(this), "Do you really want to paste files here?",
                  "Paste?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }

                FolderTreeViewModel sourceToRefresh = null;
                if (data.FolderPrefix != null)
                    sourceToRefresh = FindNodeByPrefix(RemoteFolders[0], data.SourceParentPrefix);
                else
                    sourceToRefresh = FindNodeByPrefix(RemoteFolders[0], data.SourcePrefix);

                List<SourceDestinationData> operationData = CompilePasteOperations(data, item);

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {

                    int currentCount = 0;
                    ParallelOptions options = new ParallelOptions();
                    options.MaxDegreeOfParallelism = 2;
                    System.Threading.Tasks.Parallel.ForEach(operationData, options, operation =>
                    {
                        currentCount++;
                        int percentDone = (int)Math.Round(((double)(currentCount) / (double)operationData.Count) * 100, 0);
                        worker.ReportProgress(percentDone);

                        _amazon.CopyFile(operation.Source, operation.Destination);

                        if (data.Cutting)
                            _amazon.DeleteFile(operation.Source);

                    });

                    if (data.Cutting && data.FolderPrefix != null)
                        _amazon.DeleteFolder(data.FolderPrefix);

                };
                worker.RunWorkerCompleted += (s, e) =>
                {
                    ctrlLoading.AdornerShowsProgress = false;
                    this.IsWorking = false;


                    if (e.Error == null)
                    {
                        if (data.Cutting == true)
                        {
                            Clipboard.Clear();
                            EnqueueGetRequest(sourceToRefresh, false);
                            EnqueueGetRequest(item, true);
                            ProcessNextGetRequest();
                        }
                        else
                            GetBucketContents(item);
                    }
                    else
                    {
                        MessageBox.Show(Window.GetWindow(this), "Error occured: " + e.Error.Message, "Error");
                    }
                };
                worker.WorkerReportsProgress = true;
                worker.ProgressChanged += (s, e) =>
                {
                    ctrlLoading.AdornerProgress = e.ProgressPercentage;
                };
                worker.RunWorkerAsync();
                ctrlLoading.AdornerShowsProgress = true;
                this.IsWorking = true;

            }
        }

        private void StandardCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            if (e.OriginalSource.GetType().Equals(typeof(TreeViewItem)))
            {
                FolderTreeViewModel selected = FolderTree.SelectedItem;
                if (e.Command.Equals(ApplicationCommands.Cut))
                    CopyAmazonFolder(selected, true);
                else if (e.Command.Equals(ApplicationCommands.Copy))
                    CopyAmazonFolder(selected);
                else if (e.Command.Equals(ApplicationCommands.Paste))
                {
                    if (Clipboard.ContainsData("amazonClipboardData"))
                        PasteAmazonObject(selected);
                    else if (Clipboard.ContainsFileDropList())
                        PasteClipboardFiles(selected);
                }

            }
            else if (e.OriginalSource.GetType().Equals(typeof(ListViewItem)))
            {
                FolderTreeViewModel selected = FolderTree.SelectedItem;
                List<AmazonS3FileObject> items = new List<AmazonS3FileObject>();
                foreach (var i in listFiles.SelectedItems)
                {
                    items.Add((AmazonS3FileObject)i);
                }
                if (e.Command.Equals(ApplicationCommands.Cut))
                    CopyAmazonFile(selected, items, true);
                else if (e.Command.Equals(ApplicationCommands.Copy))
                    CopyAmazonFile(selected, items);
                else if (e.Command.Equals(ApplicationCommands.Paste))
                {
                    if (Clipboard.ContainsData("amazonClipboardData"))
                        PasteAmazonObject(selected);
                    else if (Clipboard.ContainsFileDropList())
                        PasteClipboardFiles(selected);
                }

            }
        }

        private void StandardCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (RemoteFolders.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            if (e.Command.Equals(ApplicationCommands.Paste))
            {

                if (Clipboard.ContainsData("amazonClipboardData") == false && Clipboard.ContainsFileDropList() == false)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            else if (e.Command.Equals(ApplicationCommands.Copy) || e.Command.Equals(ApplicationCommands.Cut))
            {
                if (e.Source is AmazonFolderTree && FolderTree.SelectedItem == RemoteFolders[0])
                {
                    e.CanExecute = false;
                    return;
                }
            }
            e.CanExecute = true;

            
        }

        
    }
}
