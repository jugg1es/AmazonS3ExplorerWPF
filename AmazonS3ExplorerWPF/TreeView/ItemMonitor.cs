﻿// hardcodet.net WPF TreeView control
// Copyright (c) 2008 Philipp Sumi, Evolve Software Technologies
// Contact and Information: http://www.hardcodet.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the Code Project Open License (CPOL);
// either version 1.0 of the License, or (at your option) any later
// version.
// 
// This software is provided "AS IS" with no warranties of any kind.
// The entire risk arising out of the use or performance of the software
// and source code is with you.
//
// THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AmazonS3ExplorerWPF
{
  /// <summary>
  /// Observes bound items, and updates the tree
  /// accordingly if items are being added or removed.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ItemMonitor<T> where T : class
  {
    #region fields

    /// <summary>
    /// Stores the currently observed child collections of
    /// monitored parent items, stored by their parent's
    /// key.
    /// </summary>
    private readonly Dictionary<string, INotifyCollectionChanged> childCollections =
      new Dictionary<string, INotifyCollectionChanged>();


    /// <summary>
    /// The tree that renders the observed items.
    /// </summary>
    private readonly TreeViewBase<T> tree;

    #endregion


    #region properties

    /// <summary>
    /// Gets the currently observed child collections of
    /// monitored parent items, stored by their parent's
    /// key.
    /// </summary>
    public Dictionary<string, INotifyCollectionChanged> ChildCollections
    {
      get { return childCollections; }
    }


    /// <summary>
    /// Gets the tree that renders the observed items.
    /// </summary>
    public TreeViewBase<T> Tree
    {
      get { return tree; }
    }

    #endregion


    #region collection change event bubbling

    /// <summary>
    /// Bubbles an <see cref="INotifyCollectionChanged.CollectionChanged"/>
    /// event of one of the observed collections. The sender is the
    /// changed collection.
    /// </summary>
    public event NotifyCollectionChangedEventHandler MonitoredCollectionChanged;

    #endregion


    #region construction

    /// <summary>
    /// Creates the monitor with the tree to be processed.
    /// </summary>
    /// <param name="tree">The tree that renders the monitored
    /// items.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="tree"/>
    /// is a null reference.</exception>
    public ItemMonitor(TreeViewBase<T> tree)
    {
      if (tree == null) throw new ArgumentNullException("tree");
      this.tree = tree;
    }

    #endregion


    #region register item

    /// <summary>
    /// Creates a change listener for a given item's
    /// child nodes and caches the collection.<br/>
    /// This requires the submitted <paramref name="childItems"/>
    /// collection to be of type <see cref="INotifyCollectionChanged"/>.
    /// If the collection does not implement this interface, a debug
    /// warning is issued without an exception.
    /// </summary>
    /// <param name="itemKey">The unique key of the parent
    /// item, as returned by <see cref="TreeViewBase{T}.GetItemKey"/>.
    /// </param>
    /// <param name="childItems">The item's childs as returned by
    /// <see cref="TreeViewBase{T}.GetChildItems"/>.</param>
    public void RegisterItem(string itemKey, ICollection<T> childItems)
    {
      lock (this)
      {
        INotifyCollectionChanged observable = childItems as INotifyCollectionChanged;
        if (observable != null)
        {
                childCollections.Add(itemKey, observable);
                observable.CollectionChanged += OnItemCollectionChanged;
            
          
        }
        else
        {
          //the collection cannot be monitored - issue a warning
          string msg =
            "Cannot observe childs of {0} instance '{1}': The child collection does not implement the required {2} interface!";
          msg = String.Format(msg, typeof (T).Name, itemKey, typeof (INotifyCollectionChanged).FullName);
          Debug.WriteLine(msg);
        }
      }
    }

    #endregion


    #region remove nodes

    /// <summary>
    /// Deregisters event listeners for all nodes.
    /// </summary>
    /// <param name="treeNodes">A collection of tree
    /// nodes to be removed.</param>
    public void RemoveNodes(ItemCollection treeNodes)
    {
      lock (this)
      {
        foreach (TreeViewItem treeNode in treeNodes)
        {
          UnregisterListeners(treeNode);
        }
      }
    }


    /// <summary>
    /// Removes listener for an item that is represented
    /// by  a given <paramref name="node"/> from the cache, and also
    /// processes all the node's descendants recursively.
    /// </summary>
    /// <param name="node">The node to be removed.</param>
    public void UnregisterListeners(TreeViewItem node)
    {
      lock (this)
      {
        //try to get the represented item
        T item = node.Header as T;
        if (item == null) return;

        //get the item's observed child collection and
        //deregister it
        string itemKey = tree.GetItemKey(item);
        RemoveCollectionFromCache(itemKey);

        foreach (TreeViewItem childNode in node.Items)
        {
          //recursively deregister descendants by
          //removing all child nodes
          UnregisterListeners(childNode);
        }
      }
    }

    #endregion


    #region clear cache

    /// <summary>
    /// Deregisters the all listeners and clears
    /// the cache.
    /// </summary>
    public void Clear()
    {
      lock (this)
      {
        foreach (KeyValuePair<string, INotifyCollectionChanged> pair in childCollections)
        {
          pair.Value.CollectionChanged -= OnItemCollectionChanged;
        }

        childCollections.Clear();
      }
    }

    #endregion


    #region handle collection change

    /// <summary>
    /// Invokes if one of the observed collections is being changed. Triggers
    /// updates of the tree control.
    /// </summary>
    private void OnItemCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (!tree.Dispatcher.CheckAccess())
      {
        //make sure we're on the right thread
        NotifyCollectionChangedEventHandler handler = OnItemCollectionChanged;
        tree.Dispatcher.BeginInvoke(DispatcherPriority.Normal, handler, null, new object[] {sender, e});
        return;
      }

      lock (this)
      {
        //get the collection
        ICollection<T> collection = (ICollection<T>) sender;

        switch (e.Action)
        {
          case NotifyCollectionChangedAction.Add:
            HandleNewChildItems(collection, e);
            break;
          case NotifyCollectionChangedAction.Remove:
            HandleRemovedChildItems(collection, e);
            break;
          case NotifyCollectionChangedAction.Replace:
            TreeViewItem parentNode = GetParentNode((T) e.OldItems[0], collection);
            if (parentNode == null) return;

            //check if the node is expanded - if the "remove" part
            //clears all items, the node will be collapsed and not
            //automatically re-expanded in the "add" part
            bool expanded = parentNode.IsExpanded;
            //remove old items, than add new ones
            HandleRemovedChildItems(collection, e);
            HandleNewChildItems(collection, e);
            //re-expand if necessary
            if (expanded) parentNode.IsExpanded = true;
            break;
          case NotifyCollectionChangedAction.Reset:
            //we don't have any items ready - search the cache
            string parentKey = TryFindParentKey(sender);
            if (parentKey == null)
            {
              //if we cannot find an entry in the collection, quit
              //here
              string msg = "An observed child item collection issued a Reset event. ";
              msg += "However, the cache does not contain the collection that raised the ";
              msg += "event anymore";
              Debug.Fail(msg);
              return;
            }

            //clear all items
            ClearChilds(parentKey);
            break;
        }


        //bubble event
        if (MonitoredCollectionChanged != null)
        {
          MonitoredCollectionChanged(sender, e);
        }
      }
    }

    #endregion


    #region clear childs of item

    /// <summary>
    /// Removes all childs of a given parent node.
    /// </summary>
    /// <param name="parentItemKey"></param>
    private void ClearChilds(string parentItemKey)
    {
      //deregister and re-register the parent
      TreeViewItem parentNode = tree.TryFindNodeByKey(parentItemKey);

      if (parentNode == null)
      {
        //the parent node is longer available which should not be the case
        //-> clear the collection and quit
        RemoveCollectionFromCache(parentItemKey);

        string msg = "Could not clear childs of item '{0}': The tree does not ";
        msg += "contain a matching tree node for the item itself anymore.";
        msg = String.Format(msg, parentItemKey);
        Debug.Fail(msg);

        return;
      }


      foreach (TreeViewItem childNode in parentNode.Items)
      {
        //unregister items
        UnregisterListeners(childNode);
      }

      //clear nodes
      parentNode.Items.Clear();
      parentNode.IsExpanded = false;
    }

    #endregion


    #region handle removed items.

    /// <summary>
    /// Updates the tree if items were removed from an observed
    /// child collection. This might cause rendered tree nodes
    /// to be removed. In case lazy loading is enabled, the update
    /// of the UI may be as subtle as to remove an expander from
    /// a collapsed node if the represented item's childs were
    /// removed.
    /// </summary>
    /// <param name="observed">The observed collection.</param>
    /// <param name="e">The event arguments that provide the
    /// removed items.</param>
    private void HandleRemovedChildItems(ICollection<T> observed, NotifyCollectionChangedEventArgs e)
    {
      IList childs = e.OldItems;
      if (childs.Count == 0) return;

      //get the node of the parent item that contains the evented childs
      TreeViewItem parentNode = GetParentNode((T) childs[0], observed);
      if (parentNode == null) return;

      foreach (T childItem in childs)
      {
        string itemKey = tree.GetItemKey(childItem);

        //check if we have a corresponding open node
        //-> not necessarily the case if we're doing lazy loading
        TreeViewItem childNode;
        childNode = tree.TryFindItemNode(parentNode.Items, itemKey, false);
        if (childNode != null)
        {
          //unregister listeners
          UnregisterListeners(childNode);
          //remove node from UI
          parentNode.Items.Remove(childNode);
        }
      }

      //in case of lazy loading, the tree might contain a dummy node
      //(has not been expanded). However, it might be that it's now
      //completely empty...
      if (observed.Count == 0)
      {
        TreeUtil.ClearDummyChildNode(parentNode);
        parentNode.IsExpanded = false;
      }
    }

    #endregion


    #region handle added items

    /// <summary>
    /// Updates the tree with newly added items.
    /// </summary>
    /// <param name="observed">The observed collection.</param>
    /// <param name="e">Collection event args.</param>
    public void HandleNewChildItems(ICollection<T> observed, NotifyCollectionChangedEventArgs e)
    {
      IList childs = e.NewItems;
      if (childs.Count == 0) return;

      //get the node of the parent item that contains the evented childs
      TreeViewItem parentNode = GetParentNode((T) childs[0], observed);
      if (parentNode == null) return;


      //if the node is expanded or does not load lazily, or
      //already contains valid items, create nodes
      if (parentNode.IsExpanded || !tree.IsLazyLoading || !TreeUtil.ContainsDummyNode(parentNode))
      {
        foreach (T child in childs)
        {
          tree.CreateItemNode(child, parentNode.Items, null);
        }

        //refresh the node in order to apply styling (or any other
        //features that will be incorporated)
        parentNode.Items.Refresh();
      }
      else if (parentNode.Items.Count == 0)
      {
        //if the tree is in lazy loading mode and the item did
        //not contain any childs before, we have to add a dummy
        //node to render a expander
        parentNode.Items.Add(new TreeViewItem());
      }
    }

    #endregion


    #region get parent node for child collection

    /// <summary>
    /// Gets the tree node that represents the parent
    /// of a given item.
    /// </summary>
    /// <param name="childItem">A currently processed item that
    /// contains a parent.</param>
    /// <param name="collection">The collection that contains
    /// <paramref name="childItem"/>.</param>
    /// <returns>The parent tree node (UI control) that reprents the
    /// logical parent of <paramref name="childItem"/>.</returns>
    private TreeViewItem GetParentNode(T childItem, ICollection<T> collection)
    {
      T parent = tree.GetParentItem(childItem);
      TreeViewItem parentNode = parent == null ? null : tree.TryFindNode(parent);

      //if there is no parent according to the tree implementation,
      //the implementation is flawed
     /* if (parentNode == null)
      {
        INotifyCollectionChanged col = (INotifyCollectionChanged) collection;
        RemoveCollectionFromCache(col);

        string itemKey = tree.GetItemKey(childItem);
        string msg =
          "The tree does not contain the parent tree node for a monitored child item collection that contains item '{0}'. ";
        msg += "This can only happen if the node was removed without proper deregistration. ";
        msg += "The collection will be removed from the monitor.";
        msg = String.Format(msg, itemKey);
        Debug.Fail(msg);
      }
        */
      return parentNode;
    }

    #endregion


    #region remove collection from cache

    /// <summary>
    /// Removes a given collection from the internal cache
    /// and deregisters its event listener.
    /// </summary>
    /// <param name="itemKey">The item key under which the
    /// collection is stored in the cache.</param>
    private void RemoveCollectionFromCache(string itemKey)
    {
      INotifyCollectionChanged childs;
      if (childCollections.TryGetValue(itemKey, out childs))
      {
        childs.CollectionChanged -= OnItemCollectionChanged;
        childCollections.Remove(itemKey);
      }
    }


    /// <summary>
    /// Removes a given collection from the cache. This method is
    /// only called if something went wrong and a collection's item
    /// cannot be found anymore.
    /// </summary>
    /// <param name="col">The collection to be removed.</param>
    private void RemoveCollectionFromCache(INotifyCollectionChanged col)
    {
      string itemKey = null;
      foreach (KeyValuePair<string, INotifyCollectionChanged> pair in childCollections)
      {
        if (ReferenceEquals(pair.Value, col))
        {
          col.CollectionChanged -= OnItemCollectionChanged;
          itemKey = pair.Key;
          break;
        }
      }
      childCollections.Remove(itemKey);
    }

    #endregion


    #region find dictionary entry by value

    /// <summary>
    /// Searches the cache for a given collection.
    /// </summary>
    /// <returns>The key of the parent item that contains the
    /// submitted child collection. If no matching entry was found
    /// in the cache, a null reference is being returned.</returns>
    private string TryFindParentKey(object collection)
    {
      lock (this)
      {
        foreach (KeyValuePair<string, INotifyCollectionChanged> pair in childCollections)
        {
          if (ReferenceEquals(collection, pair.Value)) return pair.Key;
        }

        return null;
      }
    }

    #endregion

  }
}