using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace AmazonS3ExplorerWPF
{

    public class AmazonFolderTree : TreeViewBase<FolderTreeViewModel>
    {
        /// <summary>
        /// Generates a unique identifier for a given
        /// item that is represented as a node of the
        /// tree.
        /// </summary>
        /// <param name="item">An item which is represented
        /// by a tree node.</param>
        /// <returns>A unique key that represents the item.</returns>
        public override string GetItemKey(FolderTreeViewModel item)
        {
            return item.Prefix;
        }


        /// <summary>
        /// Gets all child items of a given parent item. The
        /// tree needs this method to properly traverse the
        /// logic tree of a given item.<br/>
        /// Important: If you plan to have the tree automatically
        /// update itself if nested content is being changed, you
        /// the <see cref="TreeViewBase{T}.ObserveChildItems"/> property must be
        /// true, and the collection that is being returned
        /// needs to implement the <see cref="INotifyCollectionChanged"/>
        /// interface (e.g. by returning an collection of type
        /// <see cref="ObservableCollection{T}"/>.
        /// </summary>
        /// <param name="parent">A currently processed item that
        /// is being represented as a node of the tree.</param>
        /// <returns>All child items to be represented by the
        /// tree.<br/>
        /// If setting the <see cref="TreeViewBase{T}.ObserveChildItems"/>
        /// to true is supposed to work, the returned collection must 
        /// implement <see cref="INotifyCollectionChanged"/> .
        /// </returns>
        /// <remarks>If this is an expensive operation, you should
        /// override <see cref="TreeViewBase{T}.HasChildItems"/> which
        /// invokes this method by default.</remarks>
        public override ICollection<FolderTreeViewModel> GetChildItems(FolderTreeViewModel parent)
        {
            return parent.SubFolders;
        }

        /// <summary>
        /// Gets the parent of a given item, if available. If
        /// the item is a top-level element, this method is supposed
        /// to return a null reference.
        /// </summary>
        /// <param name="item">The currently processed item.</param>
        /// <returns>The parent of the item, if available.</returns>
        public override FolderTreeViewModel GetParentItem(FolderTreeViewModel item)
        {
            return item.ParentFolder;
        }


        /*
         * *******************************************************
         * The properties below are used by the sample application
         * but have no use in a productive tree.
         * You can safely ignore them
         * *******************************************************
         */


        #region debugging properties

        public AmazonFolderTree()
        {
            Monitor.MonitoredCollectionChanged += delegate { CountNodesAndCollections(); };
        }


        static AmazonFolderTree()
        {
            FrameworkPropertyMetadata md = new FrameworkPropertyMetadata(-1);
            TreeNodeCountProperty = DependencyProperty.Register("TreeNodeCount", typeof(int), typeof(AmazonFolderTree), md);

            md = new FrameworkPropertyMetadata(0);
            ObservedCollectionCountProperty =
              DependencyProperty.Register("ObservedCollectionCount", typeof(int), typeof(AmazonFolderTree), md);
        }


        public override void Refresh(TreeLayout layout)
        {
            base.Refresh(layout);
            CountNodesAndCollections();
        }


        protected override void OnNodeCollapsed(TreeViewItem treeNode)
        {
            base.OnNodeCollapsed(treeNode);
            CountNodesAndCollections();
        }


        protected override void OnNodeExpanded(TreeViewItem treeNode)
        {
            base.OnNodeExpanded(treeNode);
            CountNodesAndCollections();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldItems"></param>
        /// <param name="newItems"></param>
        protected override void OnItemsPropertyChanged(IEnumerable<FolderTreeViewModel> oldItems,
                                                       IEnumerable<FolderTreeViewModel> newItems)
        {
            base.OnItemsPropertyChanged(oldItems, newItems);
            CountNodesAndCollections();
        }


        #region TreeNodeCount dependency property

        /// <summary>
        /// Updates the debugging dependency properties.
        /// </summary>
        internal void CountNodesAndCollections()
        {
            int count = 0;
            foreach (TreeViewItem item in RecursiveNodeList)
            {
                count++;
            }
            TreeNodeCount = count;

            ObservedCollectionCount = Monitor.ChildCollections.Count;
        }


        /// <summary>
        /// Counts the currently rendered tree nodes.
        /// </summary>
        public static readonly DependencyProperty TreeNodeCountProperty;

        /// <summary>
        /// A property wrapper for the <see cref="TreeNodeCountProperty"/>
        /// dependency property:<br/>
        /// Counts the currently rendered tree nodes.
        /// </summary>
        public int TreeNodeCount
        {
            get { return (int)GetValue(TreeNodeCountProperty); }
            set { SetValue(TreeNodeCountProperty, value); }
        }

        #endregion


        #region ObservedCollectionCount dependency property

        /// <summary>
        /// Reflects the number of collections
        /// </summary>
        public static readonly DependencyProperty ObservedCollectionCountProperty;


        /// <summary>
        /// A property wrapper for the <see cref="ObservedCollectionCountProperty"/>
        /// dependency property:<br/>
        /// Reflects the number of collections
        /// </summary>
        public int ObservedCollectionCount
        {
            get { return (int)GetValue(ObservedCollectionCountProperty); }
            set { SetValue(ObservedCollectionCountProperty, value); }
        }

        #endregion


        #endregion
    }
}
