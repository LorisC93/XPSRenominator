﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace XPSRenominator.Controls
{
    public class MultiSelectTreeView : TreeView
    {
        public MultiSelectTreeView() : base()
        {
            AllowMultiSelection();
        }

        public event EventHandler<List<TreeViewItem>>? SelectedItemsChanged;
        public List<TreeViewItem> SelectedItems { get; } = new();

        private void AllowMultiSelection()
        {
            var isSelectionChangeActiveProperty = GetType().GetProperty("IsSelectionChangeActive", BindingFlags.NonPublic | BindingFlags.Instance);

            if (isSelectionChangeActiveProperty == null) return;

            SelectedItemChanged += (sender, e) =>
            {
                if (SelectedItem is not TreeViewItem treeViewItem) return;

                // suppress selection change notification
                // select all selected items
                // then restore selection change notifications
                var isSelectionChangeActive = isSelectionChangeActiveProperty.GetValue(this, null);
                isSelectionChangeActiveProperty.SetValue(this, true, null);

                SelectedItems.ForEach(item => item.IsSelected = false);

                // allow multiple selection when control key is pressed
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (!SelectedItems.Contains(treeViewItem))
                        SelectedItems.Add(treeViewItem);
                    else
                        SelectedItems.Remove(treeViewItem);
                }
                else
                {
                    if (!SelectedItems.Contains(treeViewItem))
                    {
                        SelectedItems.Clear();
                        SelectedItems.Add(treeViewItem);
                    }
                }
                SelectedItems.ForEach(item => item.IsSelected = true);
                isSelectionChangeActiveProperty.SetValue(this, isSelectionChangeActive, null);

                SelectedItemsChanged?.Invoke(this, SelectedItems);
            };
        }
    }
}
