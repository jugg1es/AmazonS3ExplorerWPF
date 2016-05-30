using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AmazonS3ExplorerWPF
{

    public static class TreeCommands
    {
        public static readonly RoutedUICommand NewFolder = new RoutedUICommand
                      (
                              "NewFolder",
                              "NewFolder",
                              typeof(TreeCommands)
                      );
        public static readonly RoutedUICommand RenameFolder = new RoutedUICommand
                      (
                              "RenameFolder",
                              "RenameFolder",
                              typeof(TreeCommands)

                      );
        public static readonly RoutedUICommand DownloadFolder = new RoutedUICommand
                   (
                           "DownloadFolder",
                           "DownloadFolder",
                           typeof(TreeCommands)
                   );
        public static readonly RoutedUICommand DeleteFolder = new RoutedUICommand
                     (
                             "DeleteFolder",
                             "DeleteFolder",
                             typeof(TreeCommands)
                     );



    }
}
