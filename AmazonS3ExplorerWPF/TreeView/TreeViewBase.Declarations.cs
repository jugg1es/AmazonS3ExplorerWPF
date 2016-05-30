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
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;


namespace AmazonS3ExplorerWPF
{
  /// <summary>
  /// Encapsulates dependency properties etc.
  /// </summary>
  [DefaultProperty("Items")]
  [DefaultEvent("SelectedItemChanged")]
  public abstract partial class TreeViewBase<T>
  {

    //THIS IS A PARTIAL CLASS - MAIN CONTENT IS IN
    //TreeViewBase.cs


    #region SelectedItemChanged event

    /// <summary>
    /// A custom routed event which is fired if the
    /// <see cref="SelectedItemProperty"/> of the tree
    /// was changed.
    /// </summary>
    public static RoutedEvent SelectedItemChangedEvent;

    /// <summary>
    /// A custom event which is fired if the <see cref="SelectedItem"/>
    /// of the tree was changed.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Raised if the SelectedItem property is changed either programmatically or through the user interface.")]
    public event RoutedTreeItemEventHandler<T> SelectedItemChanged
    {
      add { AddHandler(SelectedItemChangedEvent, value); }
      remove { RemoveHandler(SelectedItemChangedEvent, value); }
    }

    /// <summary>
    /// Fires the <see cref="SelectedItemChangedEvent"/> routed
    /// event.
    /// </summary>
    /// <param name="newItem">The currently selected tree item,
    /// or null, if no item is selected.</param>
    /// <param name="oldItem">The previously selected item, if
    /// any.</param>
    protected void RaiseSelectedItemChangedEvent(T newItem, T oldItem)
    {
      RoutedTreeItemEventArgs<T> args;
      args = new RoutedTreeItemEventArgs<T>(newItem, oldItem);
      args.Source = this;
      RaiseEvent(args);
    }

    #endregion


    #region Items dependency property

    /// <summary>
    /// Gets or sets the items to be bound to three.
    /// </summary>
    public static readonly DependencyProperty ItemsProperty;

    /// <summary>
    /// A property wrapper for the <see cref="ItemsProperty"/>
    /// dependency property:<br/>
    /// Gets or sets the items to be bound to three.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("The root items that provide the top level of the tree.")]
    public virtual IEnumerable<T> Items
    {
      get { return (IEnumerable<T>) GetValue(ItemsProperty); }
      set { SetValue(ItemsProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="ItemsProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="Items"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void ItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>) d;
      IEnumerable<T> oldValue = (IEnumerable<T>) e.OldValue;
      IEnumerable<T> newValue = (IEnumerable<T>) e.NewValue;

      //don't do anything if the control is being created
      if (owner.IsInitialized)
      {
        owner.OnItemsPropertyChanged(oldValue, newValue);
      }
    }


    /// <summary>
    /// Renders the tree if the <see cref="Items"/> property
    /// was changed. If <see cref="PreserveLayoutOnRefresh"/>
    /// is true, the current layout will be reapplied.
    /// </summary>
    /// <param name="oldItems">The items that are currently bound
    /// to the tree or null, if no data has been assigned yet.</param>
    /// <param name="newItems">The new items that will be bound to
    /// the tree or null, if the tree is being cleared.</param>
    protected virtual void OnItemsPropertyChanged(IEnumerable<T> oldItems, IEnumerable<T> newItems)
    {
      //get the layout if we preserve it
      TreeLayout layout = PreserveLayoutOnRefresh ? currentLayout : null;
      RenderTree(newItems, layout);
    }

    #endregion


    #region SelectedItem dependency property

    /// <summary>
    /// Gets or sets the currently selected item, if any. Setting a null
    /// reference deselects the currently selected node.
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty;

    /// <summary>
    /// A property wrapper for the <see cref="SelectedItemProperty"/>
    /// dependency property:<br/>
    /// Gets or sets the currently selected item, if any. Setting a null
    /// reference deselects the currently selected node.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("The tree's currently selected item, if any.")]
    public virtual T SelectedItem
    {
      get { return (T) GetValue(SelectedItemProperty); }
      set { SetValue(SelectedItemProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="SelectedItemProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="SelectedItem"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void SelectedItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>) d;      
      T newValue = (T) e.NewValue;
      T oldValue = (T) e.OldValue;
      TreeView tree = owner.Tree;

      //don't do anything if the control is being created or does
      //not have a tree attached
      if (!owner.IsInitialized || tree == null) return;
    
      //if the root node is collapsed, expand it
      if (owner.RootNode != null) owner.RootNode.IsExpanded = true;

      owner.OnSelectedItemPropertyChanged(newValue, oldValue);
    }



    /// <summary>
    /// Handles updates of the <see cref="SelectedItem"/> property
    /// by adjusting the currently selected node. If the node that represents
    /// <paramref name="newValue"/> has not yet been created, all
    /// the item's parent nodes will be expanded and displayed on the UI.
    /// </summary>
    /// <param name="oldValue">The previously selected item, or a null
    /// reference, if no item was selected before.</param>
    /// <param name="newValue">The item to be selected on the tree. If this
    /// value is null, the root node will be selected if possible. Otherwise,
    /// the current selection will be cleared.</param>
    protected virtual void OnSelectedItemPropertyChanged(T newValue, T oldValue)
    {
      //don't handle tree events if we're repositioning the
      //selection
      ignoreItemChangeEvents = true;

      try
      {
        //get the item's key
        string itemKey = newValue == null ? null : GetItemKey(newValue);

        if (newValue == null)
        {
          ResetNodeSelection();
        }
        else if (Tree.SelectedItem != null && ((TreeViewItem)Tree.SelectedItem).Header == newValue)
        {
          //the item is already selected - we're fine - just bring it into view
          TreeViewItem item = (TreeViewItem)Tree.SelectedItem;
          item.BringIntoView();
        }
        else
        {
          //if the tree lazily loads nodes, the ancestors and the node
          //must be created first
          try
          {
            SelectItemNode(newValue);
          }
          catch
          {
            //reset selection
            SelectedItem = null;

            //rethrow exception
            throw;
          }
        }

        //update layout
        currentLayout.SelectedItemId = itemKey;

        //collapse all other items
        ApplyAutoCollapse();

        //bubble custom event
        if (!ReferenceEquals(newValue, oldValue))
        {
          RaiseSelectedItemChangedEvent(newValue, oldValue);
        }
      }
      finally
      {
        ignoreItemChangeEvents = false;
      }
    }

    #endregion


    #region IsLazyLoading dependency property

    /// <summary>
    /// Gets or sets whether tree nodes are being created on demand. If set to
    /// true (default value), nodes are being created as soon as they are going
    /// to be displayed the first time because their parent node is being expanded.
    /// </summary>
    public static readonly DependencyProperty IsLazyLoadingProperty;

    /// <summary>
    /// A property wrapper for the <see cref="IsLazyLoadingProperty"/>
    /// dependency property:<br/>
    /// Gets or sets whether tree nodes are being created on demand. If set to
    /// true (default value), nodes are being created as soon as they are going
    /// to be displayed the first time because their parent node is being expanded.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Whether to delay creation of child nodes until a parent node is expanded.")]
    public virtual bool IsLazyLoading
    {
      get { return (bool) GetValue(IsLazyLoadingProperty); }
      set { SetValue(IsLazyLoadingProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="IsLazyLoadingProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="IsLazyLoading"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void IsLazyLoadingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //don't do anything if the control is being created
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      if (!owner.IsInitialized) return;

      //recreate the three
      owner.Refresh(owner.GetTreeLayout());
    }

    #endregion


    #region ClearCollapsedNodes dependency property

    /// <summary>
    /// If enabled along with lazy loading, the tree automatically
    /// discards tree nodes if their parent is being collapsed.
    /// This keeps the memory footprint at a minimum (only visible nodes exist
    /// in memory), but re-expanding a node also requires recreation of its
    /// child nodes. This property defaults to true.<br/>
    /// Important: This feature is only applied if <see cref="IsLazyLoading"/>
    /// is true as well.
    /// </summary>
    public static readonly DependencyProperty ClearCollapsedNodesProperty;
    
    
    /// <summary>
    /// Resolves whether collapses nodes should be cleared or
    /// not. This depends on both <see cref="IsLazyLoading"/>
    /// and <see cref="ClearCollapsedNodes"/> dependency
    /// properties.
    /// </summary>
    public virtual bool ClearCollapsedNodesResolved
    {
      get { return IsLazyLoading && ClearCollapsedNodes; }
    }


    /// <summary>
    /// A property wrapper for the <see cref="ClearCollapsedNodesProperty"/>
    /// dependency property:<br/>
    /// If enabled along with lazy loading, the tree automatically
    /// discards tree nodes if their parent is being collapsed.
    /// This keeps the memory footprint at a minimum (only visible nodes exist
    /// in memory), but re-expanding a node also requires recreation of its
    /// child nodes. This property defaults to true.<br/>
    /// Important: This feature is only applied if <see cref="IsLazyLoading"/>
    /// is true as well.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Removes collapsed nodes from the tree if lazy loading is active.")]
    public bool ClearCollapsedNodes
    {
      get { return (bool) GetValue(ClearCollapsedNodesProperty); }
      set { SetValue(ClearCollapsedNodesProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="ClearCollapsedNodesProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="ClearCollapsedNodes"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void ClearCollapsedNodesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //don't do anything if the control is being created
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      if (!owner.IsInitialized) return;

      //recreate the three
      owner.Refresh(owner.GetTreeLayout());
    }

    #endregion


    #region ObserveChildItems dependency property

    /// <summary>
    /// If set to true, the control observes not only the directly bound
    /// <see cref="Items"/> collection, but also all rendered child
    /// collections for changes in order to reflect updates of the data
    /// source.<br/>
    /// If this property is set to false, you can always update the
    /// tree by invoking <see cref="Refresh()"/> or one of its overloads.
    /// This property also does not affect the behaviour of the main
    /// <see cref="Items"/> property. Changing <see cref="Items"/> always
    /// recreates and fully updates the tree.
    /// </summary>
    public static readonly DependencyProperty ObserveChildItemsProperty;

    /// <summary>
    /// A property wrapper for the <see cref="ObserveChildItemsProperty"/>
    /// dependency property:<br/>
    /// If set to true, the control observes not only the directly bound
    /// <see cref="Items"/> collection, but also all rendered child
    /// collections for changes in order to reflect updates of the data
    /// source.<br/>
    /// If this property is set to false, you can always update the
    /// tree by invoking <see cref="Refresh()"/> or one of its overloads.
    /// This property also does not affect the behaviour of the main
    /// <see cref="Items"/> property. Changing <see cref="Items"/> always
    /// recreates and fully updates the tree.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Whether to monitor child collections of rendered nodes for changes or not.")]
    public virtual bool ObserveChildItems
    {
      get { return (bool) GetValue(ObserveChildItemsProperty); }
      set { SetValue(ObserveChildItemsProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="ObserveChildItemsProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="ObserveChildItems"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void ObserveChildItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //don't do anything if the control is being created
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      if (!owner.IsInitialized) return;

      //recreate the three
      owner.Refresh(owner.GetTreeLayout());
    }

    #endregion


    #region Tree dependency property

    /// <summary>
    /// The underlying WPF <see cref="TreeView"/>
    /// control.
    /// </summary>
    public static readonly DependencyProperty TreeProperty;


    /// <summary>
    /// A property wrapper for the <see cref="TreeProperty"/>
    /// dependency property:<br/>
    /// Gets or sets the underlying WPF <see cref="TreeView"/> control.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("The underlying WPF TreeView that renders the assigned data.")]
    public virtual TreeView Tree
    {
      get { return (TreeView)GetValue(TreeProperty); }
      set { SetValue(TreeProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="TreeProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="TreeView"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void TreePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      TreeView oldValue = (TreeView) e.OldValue;
      TreeView newValue = (TreeView)e.NewValue;
    
      if (oldValue != null)
      {
        //deregister event listeners
        oldValue.SelectedItemChanged -= owner.OnSelectedItemChanged;
        oldValue.MouseRightButtonUp -= owner.OnRightMouseButtonUp;
      }

      //set the tree as the control content
      owner.Content = newValue;
      //only assign a style if it's not null
      if (newValue != null && owner.TreeStyle != null) newValue.Style = owner.TreeStyle;

      if (newValue != null)
      {
        //register event listeners
        newValue.SelectedItemChanged += owner.OnSelectedItemChanged;
        newValue.MouseRightButtonUp += owner.OnRightMouseButtonUp;
      }

      //don't do anything if the control is being created
      if (!owner.IsInitialized) return;

      //render the tree
      owner.Refresh();
    }

    #endregion


    #region TreeStyle dependency property

    /// <summary>
    /// The style to be attached to the control's <see cref="TreeProperty"/>.
    /// </summary>
    public static readonly DependencyProperty TreeStyleProperty;


    /// <summary>
    /// A property wrapper for the <see cref="TreeStyleProperty"/>
    /// dependency property:<br/>
    /// The style to be attached to the control's <see cref="TreeProperty"/>.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("The style to be attached to the internal TreeView control.")]
    public virtual Style TreeStyle
    {
      get { return (Style)GetValue(TreeStyleProperty); }
      set { SetValue(TreeStyleProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="TreeStyleProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="TreeStyle"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void TreeStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      Style newValue = (Style)e.NewValue;

      //assign new style
      if (owner.Tree != null && newValue != null) owner.Tree.Style = newValue;
    }

    #endregion


    #region RootNode dependency property

    /// <summary>
    /// A custom root node, which can be declared for the tree. If this
    /// property is not null, All items of the tree will be rendered
    /// as childs of this item.
    /// </summary>
    public static readonly DependencyProperty RootNodeProperty;


    /// <summary>
    /// A property wrapper for the <see cref="RootNodeProperty"/>
    /// dependency property:<br/>
    /// A custom root node, which can be declared for the tree. If this
    /// property is not null, All items of the tree will be rendered
    /// as childs of this item.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("A virtual root node that displays the assigned items.")]
    public TreeViewItem RootNode
    {
      get { return (TreeViewItem)GetValue(RootNodeProperty); }
      set { SetValue(RootNodeProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="RootNodeProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="RootNode"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void RootNodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //just recreate the tree - this takes care of everything
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      TreeViewItem newValue = (TreeViewItem) e.NewValue;

      //don't do anything if the control is being created
      if (!owner.IsInitialized) return;

      owner.Refresh(owner.GetTreeLayout());

      //apply sorting on root or tree (if the new value is null)
      owner.ApplySorting(newValue, null);
    }

    #endregion


    #region PreserveLayoutOnRefresh dependency property

    /// <summary>
    /// This property defines whether the tree's layout (selected,
    /// expanded nodes) should be preserved as good as possible,
    /// if the tree is being recreated due to a new data source
    /// or a refresh.
    /// </summary>
    public static readonly DependencyProperty PreserveLayoutOnRefreshProperty;

    /// <summary>
    /// A property wrapper for the <see cref="PreserveLayoutOnRefreshProperty"/>
    /// dependency property:<br/>
    /// Gets or sets  whether the tree's layout (selected,
    /// expanded nodes) should be preserved as good as possible,
    /// if the tree is being recreated due to a new data source
    /// or a refresh.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Keeps the layout if possible, if the control is refreshed.")]
    public bool PreserveLayoutOnRefresh
    {
      get { return (bool)GetValue(PreserveLayoutOnRefreshProperty); }
      set { SetValue(PreserveLayoutOnRefreshProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="PreserveLayoutOnRefreshProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="PreserveLayoutOnRefresh"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void PreserveLayoutOnRefreshPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //currently not in use
    }

    #endregion


    #region TreeNodeStyle dependency property

    /// <summary>
    /// A style which is explicitly applied to every tree node except
    /// the custom <see cref="RootNode"/>.
    /// </summary>
    public static readonly DependencyProperty TreeNodeStyleProperty;

    /// <summary>
    /// A property wrapper for the <see cref="TreeNodeStyleProperty"/>
    /// dependency property:<br/>
    /// A style which is explicitly applied to every <see cref="TreeViewItem"/>
    /// except the custom <see cref="RootNode"/>.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("The style to be assigned to the tree's item nodes.")]
    public virtual Style TreeNodeStyle
    {
      get { return (Style)GetValue(TreeNodeStyleProperty); }
      set { SetValue(TreeNodeStyleProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="TreeNodeStyleProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="TreeNodeStyle"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void TreeNodeStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //don't do anything if the control is being created
      TreeViewBase<T> owner = (TreeViewBase<T>)d;
      if (!owner.IsInitialized || owner.Tree == null) return;
     
      //assign the style to every node of the tree
      foreach (TreeViewItem node in owner.RecursiveNodeList)
      {
        owner.ApplyNodeStyle(node, (T)node.Header);
      }
    }

    #endregion


    #region NodeSortDescriptions dependency property

    /// <summary>
    /// A collection of <see cref="SortDescription"/> objects which is applied
    /// on all nodes, the <see cref="RootNode"/> (unless it already contains
    /// sort descriptions), and the <see cref="Tree"/> itself.<br/>
    /// If you need more control over sort descriptions, you can override
    /// the virtual <see cref="ApplySorting"/> method.
    /// </summary>
    public static readonly DependencyProperty NodeSortDescriptionsProperty;

    /// <summary>
    /// A property wrapper for the <see cref="NodeSortDescriptionsProperty"/>
    /// dependency property:<br/>
    /// A collection of <see cref="SortDescription"/> objects which is applied
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Sorting directives for the tree's nodes.")]
    public virtual IEnumerable<SortDescription> NodeSortDescriptions
    {
      get { return (IEnumerable<SortDescription>) GetValue(NodeSortDescriptionsProperty); }
      set { SetValue(NodeSortDescriptionsProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="NodeSortDescriptionsProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="NodeSortDescriptions"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void NodeSortDescriptionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //don't do anything if the control is being created
      TreeViewBase<T> owner = (TreeViewBase<T>) d;
      if (!owner.IsInitialized || owner.Tree == null) return;

      //set sort parameters on tree (root == null) or root node
      owner.ApplySorting(owner.RootNode, null);

      //iterate over all nodes and apply sorting
      foreach (TreeViewItem item in owner.RecursiveNodeList)
      {
        owner.ApplySorting(item, (T) item.Header);
      }      
    }

    #endregion


    #region SelectNodesOnRightClick dependency property

    /// <summary>
    /// If set to true, treeview items are automatically selected on right clicks,
    /// which simplifies context menu handling.
    /// </summary>
    public static readonly DependencyProperty SelectNodesOnRightClickProperty;

    /// <summary>
    /// A property wrapper for the <see cref="SelectNodesOnRightClickProperty"/>
    /// dependency property:<br/>
    /// If set to true, treeview items are automatically selected on right clicks,
    /// which simplifies context menu handling.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Whether right-clicked nodes should be selected or not.")]
    public virtual bool SelectNodesOnRightClick
    {
      get { return (bool)GetValue(SelectNodesOnRightClickProperty); }
      set { SetValue(SelectNodesOnRightClickProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="SelectNodesOnRightClickProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="SelectNodesOnRightClick"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void SelectNodesOnRightClickPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //not in use
    }

    #endregion


    #region NodeContextMenu dependency property

    /// <summary>
    /// Defines a context menu to be assigned to open on all nodes of
    /// the tree. If this property is set, the context menu will be
    /// displayed if a node of the tree is being right-clicked, *and*
    /// no custom context menu has been assigned to the node.<br/>
    /// When handling menu-related events, the clicked node that
    /// caused the event can be determined by accessing the menu's
    /// <see cref="ContextMenu.PlacementTarget"/> property.
    /// </summary>
    public static readonly DependencyProperty NodeContextMenuProperty;


    /// <summary>
    /// A property wrapper for the <see cref="NodeContextMenuProperty"/>
    /// dependency property:<br/>
    /// Defines a context menu to be assigned to open on all nodes of
    /// the tree. If this property is set, the context menu will be
    /// displayed if a node of the tree is being right-clicked, *and*
    /// no custom context menu has been assigned to the node.<br/>
    /// When handling menu-related events, the clicked node that
    /// caused the event can be determined by accessing the menu's
    /// <see cref="ContextMenu.PlacementTarget"/> property.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("A custom context menu which is displayed for all nodes.")]
    public virtual ContextMenu NodeContextMenu
    {
      get { return (ContextMenu)GetValue(NodeContextMenuProperty); }
      set { SetValue(NodeContextMenuProperty, value); }
    }


    /// <summary>
    /// Handles changes on the <see cref="NodeContextMenuProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="NodeContextMenu"/> property wrapper, updates should be handled here.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void NodeContextMenuPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //currently not in use
    }

    #endregion


    #region AutoCollapse dependency property

    /// <summary>
    /// If set to true, the tree will automatically collapse all nodes
    /// that do not need to be expanded in order to display the
    /// <see cref="SelectedItem"/>.
    /// </summary>
    [Category("hardcodet.net")]
    [Description("Collapses all nodes that are not needed to show the selected item.")]
    public static readonly DependencyProperty AutoCollapseProperty;


    /// <summary>
    /// A property wrapper for the <see cref="AutoCollapseProperty"/>
    /// dependency property:<br/>
    /// If set to true, the tree will automatically collapse all nodes
    /// that do not need to be expanded in order to display the
    /// <see cref="SelectedItem"/>.
    /// </summary>
    public bool AutoCollapse
    {
      get { return (bool) GetValue(AutoCollapseProperty); }
      set { SetValue(AutoCollapseProperty, value); }
    }


    /// <summary>
    /// A static callback listener which is being invoked if the
    /// <see cref="AutoCollapseProperty"/> dependency property has
    /// been changed. Invokes the <see cref="OnAutoCollapsePropertyChanged"/>
    /// instance method of the changed instance.
    /// </summary>
    /// <param name="d">The currently processed owner of the property.</param>
    /// <param name="e">Provides information about the updated property.</param>
    private static void AutoCollapsePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TreeViewBase<T> owner = (TreeViewBase<T>) d;

      //don't do anything if the control is being created
      if (owner.IsInitialized)
      {
        owner.OnAutoCollapsePropertyChanged(e);
      }
    }


    /// <summary>
    /// Handles changes of the <see cref="AutoCollapseProperty"/> dependency property. As
    /// WPF internally uses the dependency property system and bypasses the
    /// <see cref="AutoCollapse"/> property wrapper, updates of the property's value
    /// should be handled here.<br/>
    /// If set to true, the tree will automatically collapse all nodes
    /// that do not need to be expanded in order to display the
    /// <see cref="SelectedItem"/>.
    /// </summary>
    /// <param name="e">Provides information about the updated property.</param>
    protected virtual void OnAutoCollapsePropertyChanged(DependencyPropertyChangedEventArgs e)
    {
      bool newValue = (bool) e.NewValue;
      if (newValue)
      {
        //only react if the value is being set to true
        ApplyAutoCollapse();
      }
    }

    #endregion

    
    #region static constructor

    /// <summary>
    /// Inits the control's properties and events.
    /// </summary>
    static TreeViewBase()
    {
      //register and event listener that gets bubbled tree node expansion events
      EventManager.RegisterClassHandler(typeof(TreeViewBase<T>), TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeNodeExpanded));

      //register and event listener that gets bubbled tree node expansion events
      EventManager.RegisterClassHandler(typeof(TreeViewBase<T>), TreeViewItem.CollapsedEvent, new RoutedEventHandler(OnTreeNodeCollapsed));


      //register item change event
      SelectedItemChangedEvent = EventManager.RegisterRoutedEvent("SelectedItemChanged", RoutingStrategy.Bubble,
                                         typeof (RoutedTreeItemEventHandler<T>),
                                         typeof (TreeViewBase<T>));

      //register UI dependency properties
      FrameworkPropertyMetadata md = new FrameworkPropertyMetadata(true, IsLazyLoadingPropertyChanged);
      IsLazyLoadingProperty = DependencyProperty.Register("IsLazyLoading", typeof (bool), typeof (TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(true, ClearCollapsedNodesPropertyChanged);
      ClearCollapsedNodesProperty = DependencyProperty.Register("ClearCollapsedNodes", typeof (bool), typeof (TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(true, ObserveChildItemsPropertyChanged);
      ObserveChildItemsProperty = DependencyProperty.Register("ObserveChildItems", typeof (bool), typeof (TreeViewBase<T>), md);                                                      

      md = new FrameworkPropertyMetadata(null, TreePropertyChanged);
      TreeProperty = DependencyProperty.Register("Tree", typeof(TreeView), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(null, TreeStylePropertyChanged);
      TreeStyleProperty = DependencyProperty.Register("TreeStyle", typeof(Style), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(null, RootNodePropertyChanged);
      RootNodeProperty = DependencyProperty.Register("RootNode", typeof(TreeViewItem), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(true, PreserveLayoutOnRefreshPropertyChanged);
      PreserveLayoutOnRefreshProperty = DependencyProperty.Register("PreserveLayoutOnRefresh", typeof(bool), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(null, TreeNodeStylePropertyChanged);
      TreeNodeStyleProperty = DependencyProperty.Register("TreeNodeStyle", typeof(Style), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(null, NodeSortDescriptionsPropertyChanged);
      NodeSortDescriptionsProperty = DependencyProperty.Register("NodeSortDescriptions", typeof (IEnumerable<SortDescription>), typeof (TreeViewBase<T>), md);                                                      

      md = new FrameworkPropertyMetadata(false, SelectNodesOnRightClickPropertyChanged);
      SelectNodesOnRightClickProperty = DependencyProperty.Register("SelectNodesOnRightClick", typeof(bool), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(null, NodeContextMenuPropertyChanged);
      NodeContextMenuProperty = DependencyProperty.Register("NodeContextMenu", typeof(ContextMenu), typeof(TreeViewBase<T>), md);

      md = new FrameworkPropertyMetadata(false, AutoCollapsePropertyChanged);
      AutoCollapseProperty = DependencyProperty.Register("AutoCollapse", typeof (bool), typeof (TreeViewBase<T>), md);                                                      

      //register item dependency properties
      md = new FrameworkPropertyMetadata(null, SelectedItemPropertyChanged);
      SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof (T), typeof (TreeViewBase<T>), md);                                                      
      
      md = new FrameworkPropertyMetadata(null, ItemsPropertyChanged);
      ItemsProperty = DependencyProperty.Register("Items", typeof (IEnumerable<T>), typeof (TreeViewBase<T>), md);                                                      
    }

    #endregion

  }
}
