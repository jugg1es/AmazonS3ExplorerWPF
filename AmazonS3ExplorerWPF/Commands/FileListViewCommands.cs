using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AmazonS3ExplorerWPF
{
    public static class FileListViewCommands
    {
        public static readonly RoutedUICommand DownloadFile = new RoutedUICommand
                      (
                              "DownloadFile",
                              "DownloadFile",
                              typeof(FileListViewCommands)
                      );
        public static readonly RoutedUICommand ViewFile = new RoutedUICommand
                      (
                              "ViewFile",
                              "ViewFile",
                              typeof(FileListViewCommands)
                      );
        public static readonly RoutedUICommand InvalidateFile = new RoutedUICommand
                              (
                                      "InvalidateFile",
                                      "InvalidateFile",
                                      typeof(FileListViewCommands)
                              );
        public static readonly RoutedUICommand DeleteFile = new RoutedUICommand
                             (
                                     "DeleteFile",
                                     "DeleteFile",
                                     typeof(FileListViewCommands)
                             );

        public static readonly RoutedUICommand RenameFile = new RoutedUICommand
                            (
                                    "RenameFile",
                                    "RenameFile",
                                    typeof(FileListViewCommands)
                            );

    }
}
