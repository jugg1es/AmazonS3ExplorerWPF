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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AmazonS3ExplorerWPF
{
  /// <summary>
  /// An abstract composite control that hosts a single
  /// <see cref="TreeView"/> control.
  /// </summary>
  /// <typeparam name="T">The type of the tree's items.</typeparam>
  public abstract partial class TreeViewBase<T> : UserControl where T:class
  {

    //THIS IS A PARTIAL CLASS - DEPENDENCY PROPERTIES ARE DECLARED IN
    //TreeViewBase.Declarations.cs


    #region fields / properties

    /// <summary>
    /// Helper flag which indicates that selection
    /// events from the tree should be ignored. Used
    /// during recreation of the tree.
    /// </summary>
    private bool ignoreItemChangeEvents = false;

    /// <summary>
    /// This flag is set to true as soon as the tree has
    /// been rendered the first time.
    /// </summary>
    private bool isTreeRendered = false;

    /// <summary>
    /// The tree's internal layout. Used to keep track of expanded
    /// nodes, even if they are virtualized.
    /// </summary>
    private TreeLayout currentLayout = new TreeLayout();

    /// <summary>
    /// Used to track modifications in child collections of rendered
    /// items if the <see cref="ObserveChildItems"/> dependency
    /// property is true.
    /// </summary>
    private ItemMonitor<T> monitor;

    /// <summary>
    /// Used to track modifications in child collections of rendered
    /// items if the <see cref="ObserveChildItems"/> dependency
    /// property is true.
    /// </summary>
    protected ItemMonitor<T> Monitor
    {
      get { return monitor; }
      set { monitor = value; }
    }

    #endregion


    #region tree initialization

    /// <summary>
    /// Inits the tree.
    /// </summary>
    public TreeViewBase()
    {
      Monitor = new ItemMonitor<T>(this);
    }

    /// <summary>
    /// Makes sure a tree is present, and renders the control
    /// with the assigned items.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      //if necessary, create a default tree, then render the first time
      if (!isTreeRendered)
      {
        if (Tree == null)
        {
          //set a default tree - this also triggers rendering
          Tree = new TreeView();
        }
        else
        {
          //everything has been created, but the tree has not
          //been rendered yet, because IsInitialized was false
          //-> render now
          Refresh(null);
        }
      }
    }

    #endregion


    #region refresh

    /// <summary>
    /// Triggers a refresh of the tree.
    /// </summary>
    public virtual void Refresh()
    {
      //get the layout if we preserve it
      TreeLayout layout = PreserveLayoutOnRefresh ? GetTreeLayout() : null;
      Refresh(layout);
    }


    /// <summary>
    /// Recreates the tree with a given tree layout.
    /// </summary>
    /// <param name="layout">Defines the layout of the tree.</param>
    public virtual void Refresh(TreeLayout layout)
    {
      RenderTree(Items, layout);
    }

    #endregion


    #region clean up on node on collapse

    /// <summary>
    /// Receives a bubbled collapsed event. This causes the tree
    /// to clear all items that have become invisible.
    /// </summary>
    private static void OnTreeNodeCollapsed(object sender, RoutedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>)sender;
      TreeViewItem treeNode = (TreeViewItem)e.OriginalSource;
      owner.OnNodeCollapsed(treeNode);

      //do NOT mark as handled - the event may bubble up the tree:
      //e.Handled = true;
    }


    /// <summary>
    /// Clears all subnodes of the currently active tree, if lazy loading
    /// is enabled.
    /// </summary>
    /// <param name="treeNode">The collapsed tree view node.</param>
    protected virtual void OnNodeCollapsed(TreeViewItem treeNode)
    {
      //do not process the root node
      if (ReferenceEquals(treeNode, RootNode)) return;

      //update the layout
      string itemKey = GetItemKey((T)treeNode.Header);
      currentLayout.ExpandedNodeIds.Remove(itemKey);

      //if we don't use lazy loading, the tree has been fully created
      //-> don't remove anything
      if (ClearCollapsedNodesResolved && treeNode.Items.Count > 0)
      {
        //deregisters listeners for all ancestors
        if (ObserveChildItems)
        {
          monitor.RemoveNodes(treeNode.Items);
        }
        
        //clear items and insert dummy
        treeNode.Items.Clear();
        treeNode.Items.Add(new TreeViewItem());
      }
    }

    #endregion


    #region create items on expansion

    /// <summary>
    /// A static event listener which is invoked if a tree's node is being
    /// expanded. This event is being observed because nodes may need to
    /// be created if lazy loading is active.
    /// </summary>
    /// <param name="sender">The processed <see cref="TreeViewBase{T}"/>
    /// control.</param>
    /// <param name="e">Event arguments. The <see cref="RoutedEventArgs.OriginalSource"/>
    /// property provides the expanded tree node.</param>
    private static void OnTreeNodeExpanded(object sender, RoutedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>)sender;
      TreeViewItem treeNode = (TreeViewItem)e.OriginalSource;
      owner.OnNodeExpanded(treeNode);

      //do NOT mark as handled - the event may bubble up the tree:
      //e.Handled = true;
    }


    /// <summary>
    /// Handles lazy creation of child nodes if a node is being expanded
    /// the first time.
    /// </summary>
    protected virtual void OnNodeExpanded(TreeViewItem treeNode)
    {
      //the node does not represent one of our bound items
      //(custom root or some injected stuff)
      T item = treeNode.Header as T;
      if (item == null) return;

      //update the layout
      string itemKey = GetItemKey(item);
      currentLayout.ExpandedNodeIds.Add(itemKey);

      //the tree has already been created - there is nothing more to do here
      if (!IsLazyLoading)
      {
        //the tree has already been created - there is nothing more to do here
        //however, if the node does not contain anything, don't show as expanded
        if (treeNode.Items.Count == 0)
        {
          treeNode.IsExpanded = false;
          currentLayout.ExpandedNodeIds.Remove(itemKey);
        }

        return;
      }

      //if we have a dummy node, remove it
      TreeUtil.ClearDummyChildNode(treeNode);

      //get the child items
      ICollection<T> childItems = GetChildItems(item);
      if (treeNode.Items.Count == 0)
      {
        foreach (T childItem in childItems)
        {
          //create child items with the current layout
          //-> this also re-expands subitems that were
          //expanded, but discarded with their ancestor
          CreateItemNode(childItem, treeNode.Items, currentLayout);
        }

        //refresh to apply sorting
        if (treeNode.Items.NeedsRefresh)
        {
          treeNode.Items.Refresh();
        }
      }

      if (treeNode.Items.Count == 0)
      {
        //collapse again if there was no data at all
        //unlikely, but the bound item's childs might have changed
        treeNode.IsExpanded = false;
        currentLayout.ExpandedNodeIds.Remove(itemKey);
      }
    }

    #endregion


    #region tree creation

    /// <summary>
    /// Renders the tree and optionally preserves its current layout.
    /// </summary>
    /// <param name="items">The items to be displayed on the tree.</param>
    /// <param name="layout">The layout to be applied on the tree.</param>
    private void RenderTree(IEnumerable<T> items, TreeLayout layout)
    {
      //clear monitored items
      monitor.Clear();

      //if there is no tree, there is nothing to render
      if (Tree == null) return;

      //set rendered flag
      isTreeRendered = true;

      //suppress selection change event if the tree is cleared
      ignoreItemChangeEvents = true;

      //clear all items (root, if available, will be re-added)
      Tree.Items.Clear();
      
      TreeViewItem root = RootNode;
      if (root != null)
      {
        //if we have a root node, clear and expand it
        root.Items.Clear();
        root.IsExpanded = true;
        Tree.Items.Add(root);
      }

      //if a null value was assigned (no items), we're done
      if (items == null) return;

      //recreate root item nodes (childs will be created automatically
      //according to the layout)
      List<TreeViewItem> rootList = new List<TreeViewItem>();
      foreach (T item in items)
      {
        CreateItemNode(item, rootList, layout);
      }

      //assign all items at once to the tree
      //-> render within root, or on the tree itself
      ItemCollection treeNodes = root != null ? root.Items : Tree.Items;
      foreach (TreeViewItem item in rootList) treeNodes.Add(item);

      //verify the selected node - if it does no longer exist, reset
      //the SelectedItem property
      TreeViewItem selectedNode = null;
      if (layout != null)
      {
        string itemId = layout.SelectedItemId;
        selectedNode = TryFindItemNode(Tree.Items, itemId, true);
      }
      if (selectedNode == null) SelectedItem = null;


      if (Tree.IsKeyboardFocusWithin && selectedNode != null)
      {
        //if the tree has the focus, this will auto-select the root node once
        //the tree is rendered - prevent this by explicitely setting the focus
        //to root, than to the selected item
        //-> both select are needed, depending on the currently selected item
        //(direct child of root or not makes a difference)
        if (root != null) Keyboard.Focus(root);
        Keyboard.Focus(selectedNode);
      }

      //reactivate events
      ignoreItemChangeEvents = false;

      //store layout or create a new one
      currentLayout = layout ?? new TreeLayout();
    }


    /// <summary>
    /// Creates a single <see cref="TreeViewItem"/> node that represents a
    /// given item of the <see cref="Items"/> collection and assigns the
    /// item to the node's <see cref="HeaderedItemsControl.Header"/>
    /// property.<br/>
    /// If the node's child item collection should be observed for changes
    /// (<see cref="ObserveChildItemsProperty"/>), the item's child collection
    /// is registered with the tree's <see cref="ItemMonitor{T}"/>.
    /// </summary>
    /// <param name="item">The item which is being represented by a tree node.</param>
    /// <param name="parentNodes">The parent collection that contains the created
    /// tree node item.</param>
    /// <param name="layout">Stores a predefined layout for the tree. May be null.</param>
    protected internal void CreateItemNode(T item, IList parentNodes, TreeLayout layout)
    {
      bool hasLayout = layout != null;

      //create a tree node and assign the represented item to the
      //node header
      TreeViewItem treeNode = CreateTreeViewItem(item);
      treeNode.Header = item;
      ApplyNodeStyle(treeNode, item);

      //get the unique ID of the item and its child items
      string itemKey = GetItemKey(item);
      
      //check node state
      bool isExpanded = hasLayout && layout.IsNodeExpanded(itemKey);
      bool renderChilds = isExpanded || !IsLazyLoading;
      bool hasChilds;

      //only invoke GetChildItems directly if we *need* the
      //collection. This is the case if the node is expanded or
      //collection monitoring is active
      ICollection<T> childItems = null;
      if (renderChilds || ObserveChildItems)
      {
        childItems = GetChildItems(item);
        hasChilds = childItems.Count > 0;
      }
      else
      {
        //invoke the potentially cheaper operation
        hasChilds = HasChildItems(item);
      }
 
      if (renderChilds)
      {
        //render childs if the node is expanded according to the
        //layout information, or if lazy loading is not active
        foreach (T childItem in childItems)
        {
          CreateItemNode(childItem, treeNode.Items, layout);
        }

        if (isExpanded) treeNode.IsExpanded = true;
      }
      else if (hasChilds)
      {
        //if the item has child nodes which we don't need to create right
        //now (not expanded and lazy loading is active), insert
        //a dummy node which results in an expansion indicator
        treeNode.Items.Add(new TreeViewItem());
      }

      if (hasLayout && itemKey.Equals(layout.SelectedItemId) || item == SelectedItem)
      {
        //select the item and notify
        treeNode.IsSelected = true;
      }

      //finally, if we should monitor the child collection, register it
      if (ObserveChildItems) monitor.RegisterItem(itemKey, childItems);

      //sort node contents
      ApplySorting(treeNode, item);

      //add node to the parent's items collection
      parentNodes.Add(treeNode);
    }


    /// <summary>
    /// Creates an empty <see cref="TreeViewItem"/>
    /// which will represent a given item. The default
    /// method just returns an empty <see cref="TreeViewItem"/>
    /// instance. Override it in order to further customize
    /// the item, or return a custom class that derives from
    /// <see cref="TreeViewItem"/>.
    /// </summary>
    /// <param name="item">The item which will be represented
    /// by the returned <see cref="TreeViewItem"/>.</param>
    /// <returns>A <see cref="TreeViewItem"/> which will represent
    /// the submitted <paramref name="item"/>.</returns>
    protected virtual TreeViewItem CreateTreeViewItem(T item)
    {
      return new TreeViewItem();
    }


    /// <summary>
    /// Copies the <see cref="SortDescription"/> elements of
    /// the <see cref="NodeSortDescriptions"/> collection to
    /// a currently processed tree node. This method is being
    /// invoked during the initialization of a given node.<br/>
    /// If the <see cref="ItemCollection.SortDescriptions"/>
    /// collection of the submitted <paramref name="node"/> is
    /// not empty, it will be cleared.<br/>
    /// This method is always being invoked, even if the
    /// <see cref="NodeSortDescriptions"/> dependency property
    /// is null. If you want to apply a custom sorting mechanism,
    /// simply override this method.
    /// </summary>
    /// <param name="node">The currently processed node, if any.
    /// This parameter is null if sort parameters should be set
    /// on the on the tree's <see cref="ItemsControl.Items"/>
    /// collection itself.
    /// </param>
    /// <param name="item">The item that is being represented
    /// by the node. This parameter is null if sort parameters
    /// should be set on the tree's <see cref="ItemsControl.Items"/>
    /// collection itself, or on the <see cref="RootNode"/>.</param>
    protected virtual void ApplySorting(TreeViewItem node, T item)
    {
      //check whether we're sorting on node or tree level
      ItemCollection col = node == null ? Tree.Items : node.Items;
      //clear existing sort directions, if there are any
      col.SortDescriptions.Clear();

      //copy new sort directions
      IEnumerable<SortDescription> descriptions = NodeSortDescriptions;
      if (descriptions == null) return;
      foreach (SortDescription sd in descriptions)
      {
        col.SortDescriptions.Add(sd);
      }
    }


    /// <summary>
    /// Applies the <see cref="TreeNodeStyle"/> to a given tree node.
    /// Override this method in order to provide custom styling
    /// for selected nodes of the tree.<br/>
    /// This default implementation only applies a style, if the
    /// <see cref="TreeNodeStyle"/> dependency property is not a
    /// null value, in order not to override default styles.
    /// </summary>
    /// <param name="treeNode">The node to be styled.</param>
    /// <param name="item">The bound item that is represented by the
    /// <paramref name="treeNode"/>.
    /// </param>
    protected virtual void ApplyNodeStyle(TreeViewItem treeNode, T item)
    {
      Style style = TreeNodeStyle;
      if (style != null) treeNode.Style = style;
    }


    /// <summary>
    /// Ensures that a node for a given item (and all its
    /// ancestors) has been created and selects it immediately.<br/>
    /// This method basically expands all ancestors of the submitted
    /// <paramref name="item"/> which makes sure the node
    /// will be available.
    /// </summary>
    /// <param name="item">The item that should be represented
    /// by an existing tree node.</param>
    /// <exception cref="InvalidOperationException">If the item's
    /// ancestor list does not lead back to the root items because
    /// the item does not belong to the tree, or the tree's rendered
    /// nodes and bound data are out of sync.</exception>
    protected void SelectItemNode(T item)
    { 
      string itemKey = GetItemKey(item);

      //make sure all of the item's ancestors which will be expanded
      //-> this ensures the item will be visible on the tree when
      //we render it.
      List<T> parentList = GetParentItemList(item);

      ItemCollection items = RootNode == null ? Tree.Items : RootNode.Items;
      foreach (T parent in parentList)
      {
        string parentKey = GetItemKey(parent);

        //expand the parent node, than move a level lower
        TreeViewItem parentNode = TryFindItemNode(items, parentKey, false);
        
        //if we don't find the parent, the tree's nodes are not up-to-date
        //which means we should probably refresh - write to debug output...
        if (parentNode == null)
        {
          string msg = "Cannot create a tree node for item {0} as its parent item {1} is not part of the tree's hierarchy. This is a strong indicator that data and the UI structure are out of sync.";
          msg = String.Format(msg, itemKey, parentKey);
          throw new InvalidOperationException(msg);
        }
        
        parentNode.IsExpanded = true;
        items = parentNode.Items;
      }

      //all ancestor nodes are expanded - now select the node
      TreeViewItem itemNode = TryFindItemNode(items, itemKey, false);
      if (itemNode == null)
      {
        //this could be the case if we received a root item, but the
        //tree's Items collection does not contain it.
        string msg = "Cannot select item '{0}' - the item does not exist in the hierarchy of the tree's bound items.";
        msg = String.Format(msg, itemKey);
        throw new InvalidOperationException(msg);
      }

      itemNode.IsSelected = true;
      itemNode.BringIntoView();
    }

    #endregion


    #region get layout

    /// <summary>
    /// Gets the tree's current layout of expanded / selected
    /// nodes.
    /// </summary>
    /// <returns>The tree layout.</returns>
    public virtual TreeLayout GetTreeLayout()
    {
      TreeLayout layout = new TreeLayout();
      T selected = SelectedItem;

      //set selected item
      if (selected != null) layout.SelectedItemId = GetItemKey(selected);

      //if there is no tree yet, we're done
      if (Tree != null)
      {
        //get nodes of all expanded nodes
        GetExpandedNodes(layout.ExpandedNodeIds, Tree.Items);
      }

      return layout;
    }


    /// <summary>
    /// Recursively determines all expanded nodes of the tree, and
    /// stores the qualified IDs of the underlying items in a list.
    /// </summary>
    /// <param name="nodeIds">The list to be populated.</param>
    /// <param name="nodes">The tree nodes to be processed recursively.</param>
    private void GetExpandedNodes(List<string> nodeIds, ItemCollection nodes)
    {
      foreach (TreeViewItem treeNode in nodes)
      {
        //if we're having a dummy node, break
        if (!ReferenceEquals(treeNode, RootNode) && treeNode.Header == null) break;

        if (treeNode.IsExpanded && treeNode.Header != null)
        {
          //cast safely - the root node's header might be anything
          T item = treeNode.Header as T;
          if (item != null) nodeIds.Add(GetItemKey(item));
        }

        //process recursively (always, even if the item is collapsed!)
        GetExpandedNodes(nodeIds, treeNode.Items);
      }
    }

    #endregion


    #region find nodes

    /// <summary>
    /// Gets a given node of the tree. Note that with lazy loading
    /// enabled, the tree returns null, if the corresponding tree
    /// node has not been created yet.
    /// </summary>
    /// <param name="item">The item that is being represented
    /// by the node to be looked up.</param>
    /// <returns>The node that corresponds to the item, if any.
    /// Otherwise null.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="item"/>
    /// is a null reference.</exception>
    public virtual TreeViewItem TryFindNode(T item)
    {
      if (item == null) throw new ArgumentNullException("item");

      //get item key and delegate to overload
      string itemKey = GetItemKey(item);
      return TryFindNodeByKey(itemKey);
    }


    /// <summary>
    /// Returns a node of the tree which represents a given item.
    /// Note that if lazy loading is enabled, the tree returns null,
    /// if the corresponding tree node has not been created yet.
    /// </summary>
    /// <param name="itemKey">The item identifier, as created by
    /// the <see cref="GetItemKey"/> method.</param>
    /// <returns>The node that matches the submitted key, if any.
    /// Otherwise null.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="itemKey"/>
    /// is a null reference.</exception>
    public virtual TreeViewItem TryFindNodeByKey(string itemKey)
    {
      if (itemKey == null) throw new ArgumentNullException("itemKey");
      return TryFindItemNode(Tree.Items, itemKey, true);
    }


    /// <summary>
    /// Recursively searches the tree for a node that represents
    /// a given item starting at any given level of the tree. Note that
    /// with lazy loading enabled, this method returns null if the
    /// matching node has not been created yet.
    /// </summary>
    /// <param name="treeNodes">The items to be browsed recursively.</param>
    /// <param name="itemKey">The unique node ID of the item.</param>
    /// <param name="recurse">Whether to limit the search to the <paramref name="treeNodes"/>
    /// collection or not. If true, the descendants of all items will be searched
    /// recursively.</param>
    /// <returns>The matching node, if any. Otherwise null.</returns>
    protected internal TreeViewItem TryFindItemNode(ItemCollection treeNodes, string itemKey, bool recurse)
    {
      foreach (TreeViewItem treeNode in treeNodes)
      {
        T nodeItem = treeNode.Header as T;
        if (nodeItem != null)
        {
          //the root item does not provide a matching header...
          string id = GetItemKey(nodeItem);
          if (itemKey == id) return treeNode;
        }

        //browse child items
        if (recurse)
        {
          TreeViewItem match = TryFindItemNode(treeNode.Items, itemKey, true);
          if (match != null) return match;
        }
      }

      return null;
    }

    #endregion


    #region node selection handling

    /// <summary>
    /// Resets the current selection by either selecting the
    /// <see cref="RootNode"/>, or - if no root is available -
    /// removing selection of the currently selected item.
    /// </summary>
    protected virtual void ResetNodeSelection()
    {
      //none of the Items should be selected - but we'll select a root node
      //if available
      if (RootNode != null)
      {
        RootNode.IsSelected = true;
      }
      else if (Tree.SelectedItem != null)
      {
        //if we have no root to select, clear the current selection
        TreeViewItem currentSelection = (TreeViewItem)Tree.SelectedItem;
        if (currentSelection != null) currentSelection.IsSelected = false;
      }
    }


    /// <summary>
    /// Updates the <see cref="SelectedItem"/> property if a new
    /// item has been selected. This causes a very small overhead, as
    /// a user-interaction causes the <see cref="SelectedItemPropertyChanged"/>
    /// method to actively reselect the node, but keeps the <see cref="SelectedItem"/>
    /// entity reference and the visual tree in sync.
    /// </summary>
    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      if (ignoreItemChangeEvents)
      {
        //ignore event if we're currently processing the tree structure
        e.Handled = true;
        return;
      }

      if (e.NewValue != null)
      {
        TreeViewItem node = (TreeViewItem)e.NewValue;
        T item = node.Header as T;

        SelectedItem = item;
      }

      e.Handled = true;
    }

    #endregion


    #region right mouse button click

    /// <summary>
    /// Intercepts right mouse button clicks an checks whether a tree
    /// node was clicked. If this is the case, the node will be selected
    /// in case it's not selected an the <see cref="SelectNodesOnRightClick"/>
    /// dependency property is set.<br/>
    /// If the <see cref="NodeContextMenu"/> property is set and no custom
    /// context menu was assigned to the item, the <see cref="NodeContextMenu"/>
    /// will be opened with its <see cref="ContextMenu.PlacementTarget"/> property
    /// set to the clicked tree node. Right clicks on a <see cref="RootNode"/>
    /// will be ignored.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnRightMouseButtonUp(object sender, MouseButtonEventArgs e)
    {
      //return if no node was clicked
      TreeViewItem item = e.Source as TreeViewItem;
      if (item == null) return;

      //activate item if necessary
      if (SelectNodesOnRightClick && item.IsSelected == false)
      {
        item.IsSelected = true;
      }

      //context menu handling: don't do anything if no context menu
      //was defined or one was assigned by custom code
      if (NodeContextMenu == null || item.ContextMenu != null) return;

      //also don't show a context menu if the root node was clicked
      if (ReferenceEquals(item, RootNode)) return;

      //temporarily assign the menu to the item - this ensures that
      //a the PlacementTarget property of the context menu points to
      //the item (can be evaluated in a click event or command handler)
      item.ContextMenu = NodeContextMenu;

      //open the context menu for the clicked item
      NodeContextMenu.PlacementTarget = item;
      NodeContextMenu.IsOpen = true;

      //mark as handled - let the event bubble on...
      e.Handled = true;

      //reset the context menu assignment
      item.ContextMenu = null;
    }

    #endregion


    #region iterate tree nodes

    /// <summary>
    /// Gets an enumerator that provides recursive browsing through
    /// all nodes of the tree. Note that this enumerator may not return
    /// nodes for all elements in the bound <see cref="Items"/> collection
    /// if lazy loading is enabled, but traverses the tree's existing
    /// nodes (<see cref="TreeViewItem"/> instances).<br/>
    /// </summary>
    public IEnumerable<TreeViewItem> RecursiveNodeList
    {
      get
      {
        ItemCollection nodes = RootNode == null ? Tree.Items : RootNode.Items;
        return TreeUtil.BrowseNodes(nodes);
      }
    }

    #endregion


    #region expand / collapse all nodes

    /// <summary>
    /// Expands all nodes of the tree. This means that nodes
    /// for all items will be created even if <see cref="IsLazyLoading"/>
    /// is set to true.
    /// </summary>
    public void ExpandAll()
    {
      if (RootNode != null) RootNode.IsExpanded = true;

      foreach (TreeViewItem item in RecursiveNodeList)
      {
        item.IsExpanded = true;
      }
    }

    /// <summary>
    /// Collapses all nodes of the tree. 
    /// </summary>
    /// <remarks>If <see cref="IsLazyLoading"/> is set to true,
    /// the footprint of the tree may be reduced by invoking
    /// <see cref="Refresh()"/>. This automatically discards all
    /// previously created nodes and only recreates the (visible)
    /// root nodes.</remarks>
    public void CollapseAll()
    {
      foreach (TreeViewItem item in RecursiveNodeList)
      {
        item.IsExpanded = false;
      }
    }


    /// <summary>
    /// Collapses all tree nodes that are not direct ancestors of
    /// the currently selected item's node. This method is being
    /// invoked every time the <see cref="SelectedItem"/> property
    /// is being changed, even if <see cref="AutoCollapse"/> is
    /// false.
    /// </summary>
    protected virtual void ApplyAutoCollapse()
    {
      if (Tree == null || !AutoCollapse) return;

      T selected = SelectedItem;
      ItemCollection items = RootNode == null ? Tree.Items : RootNode.Items;

      if (selected == null)
      {
        //if we don't have a selected item, just collapse the
        //root items
        foreach (TreeViewItem node in items)
        {
          node.IsExpanded = false;
        }
      }
      else
      {
        List<T> parents = GetParentItemList(selected);
        foreach (T parent in parents)
        {
          string parentKey = GetItemKey(parent);
          TreeViewItem parentNode = TryFindItemNode(items, parentKey, false);

          if (parentNode == null)
          {
            string msg = "Cannot collapse item '{0}' - the item does not exist in the hierarchy of the tree's bound items.";
            msg = String.Format(msg, parentKey);
            throw new InvalidOperationException(msg);
          }

          foreach (TreeViewItem item in items)
          {
            //collapse all items that are no ancestors
            if (item == parentNode) continue;
            item.IsExpanded = false;
          }

          //go a level deeper
          items = parentNode.Items;
        }

        //finally collapse the item and its siblings
        foreach (TreeViewItem item in items)
        {
          item.IsExpanded = false;
        }
      }
    }

    #endregion


    #region parent/child handling

    /// <summary>
    /// Checks whether a given item provides child items. This
    /// method is being invoked in order to determine whether
    /// to render an expander on a given node.<br/>
    /// The default implemenation just invokes
    /// <see cref="GetChildItems"/> and checks whether the returned
    /// collection is empty or not.
    /// </summary>
    /// <remarks>
    /// You should override this method if invoking
    /// <see cref="GetChildItems"/> is an expensive operation
    /// (e.g. because data needs to be retrieved from a web
    /// service). In case there is no possibility for a cheaper solution,
    /// you may just return true: In that case, an expander will
    /// be rendered and removed as soon as the user attempts to
    /// expand the node, if there are no child items available.<br />
    /// However: Overriding this method is pointless if
    /// <see cref="ObserveChildItems"/> is set to true. In that
    /// case, this method will not be used as
    /// <see cref="GetChildItems"/> is being invoked anyway to get
    /// the observed collection.
    /// </remarks>
    protected virtual bool HasChildItems(T parent)
    {
      return GetChildItems(parent).Count > 0;
    }

    /// <summary>
    /// Gets a list of all ancestors of a given item up to the
    /// root element, excluding the item itself. The root element
    /// is supposed to be contained at index 0, while the immediate
    /// parent is being placed at the end of the list.
    /// </summary>
    /// <param name="child">The processed item that marks the
    /// starting point.</param>
    /// <returns>A list of all the item's parents.</returns>
    protected virtual List<T> GetParentItemList(T child)
    {
      List<T> parents = new List<T>();
      T parentItem = GetParentItem(child);
      while (parentItem != null)
      {
        parents.Insert(0, parentItem);
        parentItem = GetParentItem(parentItem);
      }
      return parents;
    }


    /// <summary>
    /// Recursively checks whether an item has a given ancestor.
    /// </summary>
    /// <param name="child">A potential child element to be evaluated.</param>
    /// <param name="parent">The potential parent.</param>
    /// <returns>True in case the <paramref name="parent"/> item is either
    /// a direct or indirect parent of the <paramref name="child"/> item.</returns>
    /// <remarks>Beware: In case of circular references, calling this
    /// method results in a stack overflow.</remarks>
    public virtual bool IsChildOf(T child, T parent)
    {
      T directParent = GetParentItem(child);

      if (directParent == null)
        return false;
      else if (directParent == parent)
        return true;
      else
        return IsChildOf(directParent, parent);
    }

    #endregion


    #region abstract methods to be implemented

    /// <summary>
    /// Generates a unique identifier for a given
    /// item that is represented as a node of the
    /// tree.
    /// </summary>
    /// <param name="item">An item which is represented
    /// by a tree node.</param>
    /// <returns>A unique key that represents the item.</returns>
    public abstract string GetItemKey(T item);

    /// <summary>
    /// Gets all child items of a given parent item. The
    /// tree needs this method to properly traverse the
    /// logic tree of a given item.<br/>
    /// Important: If you plan to have the tree automatically
    /// update itself if nested content is being changed, you
    /// the <see cref="ObserveChildItems"/> property must be
    /// true, and the collection that is being returned
    /// needs to implement the <see cref="INotifyCollectionChanged"/>
    /// interface (e.g. by returning an collection of type
    /// <see cref="ObservableCollection{T}"/>.
    /// </summary>
    /// <param name="parent">A currently processed item that
    /// is being represented as a node of the tree.</param>
    /// <returns>All child items to be represented by the
    /// tree. The returned collection needs to implement
    /// <see cref="INotifyCollectionChanged"/> if the
    /// <see cref="ObserveChildItems"/> feature is supposed
    /// to work.</returns>
    /// <remarks>If this is an expensive operation, you should
    /// override <see cref="HasChildItems"/> which
    /// invokes this method by default.</remarks>
    public abstract ICollection<T> GetChildItems(T parent);


    /// <summary>
    /// Gets the parent of a given item, if available. If
    /// the item is a top-level element, this method is supposed
    /// to return a null reference.
    /// </summary>
    /// <param name="item">The currently processed item.</param>
    /// <returns>The parent of the item, if available.</returns>
    public abstract T GetParentItem(T item);

    #endregion

  }
}