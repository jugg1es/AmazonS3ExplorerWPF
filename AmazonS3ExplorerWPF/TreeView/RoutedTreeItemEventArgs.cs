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


using System.Windows;

namespace AmazonS3ExplorerWPF
{

  /// <summary>
  /// Event handler signature for the
  /// <see cref="TreeViewBase{T}.SelectedItemChangedEvent"/>
  /// routed event.
  /// </summary>
  /// <typeparam name="T">The type of the tree's items.</typeparam>
  /// <param name="sender">The event source.</param>
  /// <param name="e">Provides both new and old processed item.</param>
  public delegate void RoutedTreeItemEventHandler<T>(object sender, RoutedTreeItemEventArgs<T> e) where T : class;

  /// <summary>
  /// Event arguments for the <see cref="TreeViewBase{T}.SelectedItemChangedEvent"/>
  /// routed event.
  /// </summary>
  /// <typeparam name="T">The type of the tree's items.</typeparam>
  public class RoutedTreeItemEventArgs<T> : RoutedEventArgs where T : class
  {
    private readonly T newItem;
    private readonly T oldItem;

    /// <summary>
    /// The currently selected item that caused the event. If
    /// the tree's <see cref="TreeViewBase{T}.SelectedItem"/>
    /// property is null, so is this parameter.
    /// </summary>
    public T NewItem
    {
      get { return newItem; }
    }


    /// <summary>
    /// The previously selected item, if any. Might be null
    /// if no item was selected before.
    /// </summary>
    public T OldItem
    {
      get { return oldItem; }
    }


    /// <summary>
    /// Creates the event args.
    /// </summary>
    /// <param name="newItem">The selected item, if any.</param>
    /// <param name="oldItem">The previously selected item, if any.</param>
    public RoutedTreeItemEventArgs(T newItem, T oldItem)
      : base(TreeViewBase<T>.SelectedItemChangedEvent)
    {
      this.newItem = newItem;
      this.oldItem = oldItem;
    }

  }
}
