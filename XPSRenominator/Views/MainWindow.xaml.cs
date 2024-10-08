﻿using ColorPicker;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;
using XPSRenominator.Controllers;
using XPSRenominator.Models;
using Material = XPSRenominator.Models.Material;
using MessageBox = System.Windows.MessageBox;
using Selector = System.Windows.Controls.Primitives.Selector;

namespace XPSRenominator;

/// <summary> Logica di interazione per MainWindow.xaml </summary>
public partial class MainWindow : Window
{
    private readonly MeshAsciiLoader _loader = new();
    private string? _originalMeshAsciiName;
    private string? _originalPoseName;
    private string? _originalBonedictName;
    private TabItem _selectedTab;

    public MainWindow()
    {
        InitializeComponent();
        _selectedTab = TabBones;
    }

    private List<TreeViewItem> _cutBones = [];
    private bool _canDoDragDropOperation = true;
    private Point3D? copiedPosition = null;

    private IEnumerable<Bone> FilteredBones
    {
        get
        {
            bool ContainsCondition(Translatable bone) => bone.OriginalName.Contains(BeforeFilter.Text.ToLower()) && bone.TranslatedName.Contains(AfterFilter.Text.ToLower());
            bool OnlyUntranslatedCondition(Translatable bone) => OnlyUntranslated.IsChecked == false || bone.OriginalName == bone.TranslatedName;
            bool OnlyConflictingCondition(Bone bone) => OnlyConflicting.IsChecked == false || (bone.FromMeshAscii && _loader.Bones.Where(b => b.FromMeshAscii && b != bone).Any(b => bone.TranslatedName == b.TranslatedName));

            return _loader.Bones.Where(b => ContainsCondition(b) && OnlyUntranslatedCondition(b) && OnlyConflictingCondition(b));
        }
    }
    private IEnumerable<Mesh> FilteredMeshes
    {
        get
        {
            bool ContainsCondition(Translatable mesh) => mesh.OriginalName.Contains(BeforeFilter.Text.ToLower()) && mesh.TranslatedName.Contains(AfterFilter.Text.ToLower());
            bool OnlyUntranslatedCondition(Translatable mesh) => OnlyUntranslated.IsChecked == false || mesh.OriginalName == mesh.TranslatedName;
            bool OnlyConflictingCondition(Translatable mesh) => OnlyConflicting.IsChecked == false || _loader.Meshes.Where(m => m != mesh).Any(b => mesh.TranslatedName == b.TranslatedName);

            return _loader.Meshes.Where(b => ContainsCondition(b) && OnlyUntranslatedCondition(b) && OnlyConflictingCondition(b));
        }
    }

    private bool ShouldRenderBone(Bone bone) => FilteredBones.Contains(bone) || _loader.Bones.Where(b => b.Parent == bone).Any(ShouldRenderBone);

    private void Refresh(bool force = false)
    {
        if(force || _selectedTab == TabBones && (force || BoneTree.Items.Count == 0))
            RenderBoneTree();
        if(_selectedTab == TabMaterials && (force || MaterialsPanel.Children.Count == 0))
            RenderMaterials();
    }

    private void RenderBoneTree()
    {
        void RenderTreeItem(Bone bone, ItemsControl? parentTree = null)
        {
            if (!ShouldRenderBone(bone)) return;

            StackPanel header = new()
            {
                Orientation = Orientation.Horizontal
            };

            header.Children.Add(new TextBlock
            {
                Text = $"[{(bone.FromMeshAscii ? "" : "*")}{bone.OriginalName}]", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 5, 0)
            });

            TextBox translationTextBox = new() { Text = bone.TranslatedName, Margin = new Thickness(0, 0, 5, 0) };
            translationTextBox.Bind(TextBox.TextProperty, bone, "TranslatedName");
            translationTextBox.GotFocus += (sender, e) => _canDoDragDropOperation = false;
            translationTextBox.LostFocus += (sender, e) => _canDoDragDropOperation = true;
            translationTextBox.AllowDrop = false;
            header.Children.Add(translationTextBox);
                
            TextBlock translatingTextBlock = new()
            {
                Text = bone.TranslatingName,
                Foreground = Brushes.Red
            };
            translatingTextBlock.Bind(TextBlock.TextProperty, bone, "TranslatingName");
            header.Children.Add(translatingTextBlock);

            //TextBlock originalName = new();
            //originalName.Bind(TextBlock.TextProperty, bone, "OriginalName");
            //header.Children.Add(originalName);
                

            //TextBox posX = new() { Margin = new(5, 0, 0, 0), Width = 50 };
            //posX.Bind(TextBox.TextProperty, bone, "Position[0]");
            //TextBox posY = new() { Margin = new(5, 0, 0, 0), Width = 50 };
            //posY.Bind(TextBox.TextProperty, bone, "Position[1]");
            //TextBox posZ = new() { Margin = new(5, 0, 0, 0), Width = 50 };
            //posZ.Bind(TextBox.TextProperty, bone, "Position[2]");
            //header.Children.Add(posX);
            //header.Children.Add(posY);
            //header.Children.Add(posZ);

            TreeViewItem treeItem = new()
            {
                Header = header,
                Tag = bone,
                IsExpanded = true,
                AllowDrop = true
            };
            treeItem.DragEnter += DragEnterHandler;
            treeItem.DragOver += (sender, e) => {
                e.Effects = BoneTree.SelectedItems.Any(t => CanDrop(t, (TreeViewItem)sender)) ? DragDropEffects.Move : DragDropEffects.None;
                e.Handled = true;
            };
            treeItem.DragLeave += DragLeaveHandler;
            treeItem.Drop += TreeItem_Drop;
            var children = _loader.Bones.Where(b => b.Parent == bone && b.FromMeshAscii);
            foreach (var child in children) RenderTreeItem(child, treeItem);
            (parentTree ?? BoneTree).Items.Add(treeItem);
        }

        BoneTree.Dispatcher.Invoke(() =>
        {
            BoneTree.Items.Clear();
            foreach (var bone in _loader.Bones.Where(b => b.IsRoot)) RenderTreeItem(bone);
        });
    }


    private void UpdateMaterialRender(Material material)
    {
        var group = MaterialsPanel.Children.OfType<Expander>().SingleOrDefault(e => e.Tag == material);
        if (!MaterialManager.Materials.Contains(material))
            MaterialsPanel.Children.Remove(group);
        else
            RenderMaterial(material, group);
    }

    private void RenderMaterials()
    {
        MaterialsPanel.Dispatcher.Invoke(() =>
        {
            MaterialsPanel.Children.Clear();

            MaterialManager.Materials.ForEach(m => RenderMaterial(m));
        });
    }

    private void RenderMaterial(Material material, Expander? group = null)
    {
        if (group == null)
        {
            group = new Expander { IsExpanded = true };
            MaterialsPanel.Children.Add(group);
        }

        StackPanel groupBoxHeader = new() { Orientation = Orientation.Horizontal, AllowDrop = true };

        TextBlock materialName = new() { Text = material.Textures[0].TranslatedName, Margin = new Thickness(5, 0, 2, 0) };
        materialName.Bind(TextBlock.TextProperty, material.Textures[0], "TranslatedName");

        DoubleUpDown meshRenderParameter1 = new()
        {
            Text = material.RenderParameters[0].ToString(CultureInfo.InvariantCulture),
            Margin = new Thickness(5, 0, 2, 0), MinWidth = 25, FormatString = "F2", Minimum = 0, Increment = 0.1,
            CultureInfo = CultureInfo.InvariantCulture
        };
        DoubleUpDown meshRenderParameter2 = new()
        {
            Text = material.RenderParameters[1].ToString(CultureInfo.InvariantCulture),
            Margin = new Thickness(2, 0, 2, 0), MinWidth = 25, FormatString = "F2", Minimum = 0, Increment = 0.1,
            CultureInfo = CultureInfo.InvariantCulture
        };
        DoubleUpDown meshRenderParameter3 = new()
        {
            Text = material.RenderParameters[2].ToString(CultureInfo.InvariantCulture),
            Margin = new Thickness(2, 0, 0, 0), MinWidth = 25, FormatString = "F2", Minimum = 0, Increment = 0.1,
            CultureInfo = CultureInfo.InvariantCulture
        };
        meshRenderParameter1.Bind(InputBase.TextProperty, material, "RenderParameters[0]");
        meshRenderParameter2.Bind(InputBase.TextProperty, material, "RenderParameters[1]");
        meshRenderParameter3.Bind(InputBase.TextProperty, material, "RenderParameters[2]");
        groupBoxHeader.Children.Add(materialName);
        groupBoxHeader.Children.Add(meshRenderParameter1);
        groupBoxHeader.Children.Add(meshRenderParameter2);
        groupBoxHeader.Children.Add(meshRenderParameter3);

        CheckBox alphaEnabledCheckBox = new() { Content = "Alpha Enabled", IsChecked = material.AlphaEnabled, Margin = new Thickness(5, 0, 0, 0) };
        alphaEnabledCheckBox.Bind(ToggleButton.IsCheckedProperty, material, "AlphaEnabled");
        groupBoxHeader.Children.Add(alphaEnabledCheckBox);

        group.Header = groupBoxHeader;
        group.Tag = material;
        group.Padding = new Thickness(0, 2, 0, 2);
        group.DragEnter += DragEnterHandler;
        group.DragLeave += DragLeaveHandler;
        group.Drop += MaterialGroup_Drop;

        meshRenderParameter1.Bind(IsEnabledProperty, group, "IsExpanded");
        meshRenderParameter2.Bind(IsEnabledProperty, group, "IsExpanded");
        meshRenderParameter3.Bind(IsEnabledProperty, group, "IsExpanded");
        alphaEnabledCheckBox.Bind(IsEnabledProperty, group, "IsExpanded");

        Grid texturesGrid = new();
        texturesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        texturesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        texturesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        texturesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        texturesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        group.Content = texturesGrid;

        foreach (var textureType in RenderGroupUtils.TextureTypes)
        {
            var meshes = _loader.Meshes.Where(m => m.Material == material).ToList();

            material.Textures.TryAdd(textureType, new Texture());

            material.Textures.TryGetValue(textureType, out var texture);

            texturesGrid.RowDefinitions.Add(new RowDefinition());

            TextBlock textureKind = new() { Text = textureType.Code(), Margin = new Thickness(0, 0, 5, 0) };
            Grid.SetRow(textureKind, texturesGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(textureKind, 0);
            textureKind.Foreground = Brushes.Gray;
            textureKind.VerticalAlignment = VerticalAlignment.Center;
            texturesGrid.Children.Add(textureKind);

            TextBox translationTextBox = new() { Text = texture!.TranslatedName };
            Grid.SetRow(translationTextBox, texturesGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(translationTextBox, 1);
            translationTextBox.Bind(TextBox.TextProperty, texture, "TranslatedName");
            translationTextBox.VerticalAlignment = VerticalAlignment.Center;
            texturesGrid.Children.Add(translationTextBox);

            if (textureType is TextureType.Lightmap or TextureType.Specular && meshes.Any(m => m.UvLayers > 1))
            {
                IntegerUpDown uvLayer = new() { Text = texture!.UvLayer.ToString(CultureInfo.InvariantCulture), Minimum = 0, Maximum = meshes.Select(m => m.UvLayers - 1).Max()};
                Grid.SetRow(uvLayer, texturesGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(uvLayer, 2);
                uvLayer.Bind(InputBase.TextProperty, texture, "UvLayer");
                uvLayer.VerticalAlignment = VerticalAlignment.Center;
                texturesGrid.Children.Add(uvLayer);
            }

            TextBlock translatingTextBlock = new() { Text = texture.TranslatingName };
            Grid.SetRow(translatingTextBlock, texturesGrid.RowDefinitions.Count - 1);
            Grid.SetColumn(translatingTextBlock, 3);
            translatingTextBlock.Foreground = Brushes.Red;
            translatingTextBlock.VerticalAlignment = VerticalAlignment.Center;
            translatingTextBlock.Bind(TextBlock.TextProperty, texture, "TranslatingName");
            texturesGrid.Children.Add(translatingTextBlock);
        }

        StackPanel meshesPanel = new() { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRowSpan(meshesPanel, texturesGrid.RowDefinitions.Count);
        Grid.SetColumn(meshesPanel, 4);

        foreach (Mesh mesh in _loader.Meshes.Where(m => m.Material == material))
        {
            StackPanel meshLine = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            PortableColorPicker colorPicker = new()
                { Width = 15, Height = 15, Margin = new Thickness(2, 0, 2, 0), ShowAlpha = false, SelectedColor = mesh.Vertices.First().Color };
            colorPicker.ColorChanged += (sender, e) => { mesh.Vertices.ForEach(v => v.Color = colorPicker.SelectedColor); };
            meshLine.Children.Add(colorPicker);

            ContextMenu contextMenu = new();
            MenuItem copyName = new() { Header = "Copy name from Material" };
            MenuItem swapUv = new() { Header = "Swap Uv" };
            MenuItem viewWeights = new() { Header = "View Bone Weights" };
            MenuItem clone = new() { Header = "Clone" };
            MenuItem delete = new() { Header = "Delete" };
            MenuItem exclude = new() { Header = mesh.Exclude ? "Include" : "Exclude" };
            copyName.Click += (_, _) => mesh.TranslatedName = mesh.Material.Textures[0].TranslatedName.Replace('_','-');
            swapUv.Click += (_, _) => mesh.SwapUv();
            viewWeights.Click += (_, _) =>
            {
                MessageBox.Show(string.Join('\n', mesh.Vertices
                    .SelectMany(v => v.Bones)
                    .Select(b => b.Bone.TranslatedName + ": " + b.Weight)));
            };
            clone.Click += (_, _) =>
            {
                _loader.CloneMesh(mesh);
                UpdateMaterialRender(mesh.Material);
            };
            delete.Click += (_, _) =>
            {
                _loader.DeleteMesh(mesh);
                UpdateMaterialRender(mesh.Material);
            };
            exclude.Click += (sender, e) =>
            {
                mesh.Exclude = !mesh.Exclude;
                UpdateMaterialRender(mesh.Material);
            };
            contextMenu.Items.Add(copyName);
            contextMenu.Items.Add(swapUv);
            contextMenu.Items.Add(viewWeights);
            contextMenu.Items.Add(clone);
            contextMenu.Items.Add(delete);
            contextMenu.Items.Add(exclude);

            meshLine.Children.Add(new TextBlock
            {
                Text = $"[{mesh.OriginalName}]",
                FontWeight = mesh.Exclude ? FontWeights.Regular : FontWeights.Bold,
                FontStyle = mesh.Exclude ? FontStyles.Italic : FontStyles.Normal,
                Margin = new Thickness(2, 0, 2, 0),
                Tag = mesh,
                ContextMenu = contextMenu
            });

            ComboBox optionalVisibility = new() { Margin = new Thickness(0, 0, 5, 0), SelectedValuePath = "value", DisplayMemberPath = "text" };
            optionalVisibility.SelectionChanged += (_, e) =>
            {
                e.Handled = true;
                if(mesh.IsOptional) _loader.Meshes.Where(m => m.OptionalItemName == mesh.OptionalItemName).ToList().ForEach(m => m.IsOptionalItemVisible = mesh.IsOptionalItemVisible);
            };
            optionalVisibility.Items.Add(new { text = "+", value = true});
            optionalVisibility.Items.Add(new { text = "-", value = false});
            optionalVisibility.Bind(Selector.SelectedValueProperty, mesh, "IsOptionalItemVisible");
            optionalVisibility.Bind(Selector.IsEnabledProperty, mesh, "IsOptional", BindingMode.OneWay);
            optionalVisibility.Bind(Selector.ToolTipProperty, mesh, "TranslatedName");
            meshLine.Children.Add(optionalVisibility);
            
            TextBox optionalName = new() { Margin = new Thickness(0, 0, 5, 0), MinWidth = 50 };
            optionalName.Bind(TextBox.TextProperty, mesh, "OptionalItemName");
            optionalName.Bind(TextBox.ToolTipProperty, mesh, "TranslatedName");
            meshLine.Children.Add(optionalName);

            meshLine.Children.Add(new TextBlock { Text = ".", Margin = new Thickness(0, 0, 5, 0) });

            TextBox translationTextBox = new() { Margin = new Thickness(0, 0, 5, 0), MinWidth = 50 };
            translationTextBox.Bind(TextBox.TextProperty, mesh, "TranslatedNameWithoutOptional");
            translationTextBox.Bind(TextBox.ToolTipProperty, mesh, "TranslatedName");
            meshLine.Children.Add(translationTextBox);

            TextBlock translatingTextBlock = new() { Foreground = Brushes.Red };
            translatingTextBlock.Bind(TextBlock.TextProperty, mesh, "TranslatingName");
            meshLine.Children.Add(translatingTextBlock);

            meshesPanel.Children.Add(meshLine);
        }

        texturesGrid.Children.Add(meshesPanel);
    }


    private void DragEnterHandler(object sender, DragEventArgs e)
    {
        if (sender is not Control control) return;
        control.Background = Brushes.LightBlue;
        e.Handled = true;
    }
    private void DragLeaveHandler(object sender, DragEventArgs e)
    {
        if (sender is not Control control) return;
        control.Background = null;
        e.Handled = true;
    }

    private void MaterialPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (e is { LeftButton: MouseButtonState.Pressed, Source: TextBlock { Tag: Mesh m } })
        {
            DragDrop.DoDragDrop(MaterialsPanel, m, DragDropEffects.Move);
        }
    }

    private void MaterialGroup_Drop(object sender, DragEventArgs e)
    {
        var target = sender as ContentControl;
        var source = e.Data.GetData(typeof(Mesh)) as Mesh;
        if (target?.Tag is Material material && source is not null && source.Material != material)
        {
            ChangeMaterial(source, material);
        }
        DragLeaveHandler(sender, e);
        e.Handled = true;
    }

    private void MaterialPanel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(Mesh)) is Mesh mesh)
        {
            ChangeMaterial(mesh, (Material)mesh.Material.Clone());
        }

        DragLeaveHandler(sender, e);
        e.Handled = true;
    }

    private void ChangeMaterial(Mesh mesh, Material material)
    {
        if (!MaterialManager.Materials.Contains(material))
            MaterialManager.Materials.Add(material);
        if (_loader.Meshes.Count(m => m.Material == mesh.Material) == 1)
            MaterialManager.Materials.Remove(mesh.Material);

        var oldMaterial = mesh.Material;
        mesh.Material = material;

        UpdateMaterialRender(oldMaterial);
        UpdateMaterialRender(material);
    }

    private void BoneTree_MouseMove(object sender, MouseEventArgs e)
    {
        try
        {
            if (e.LeftButton == MouseButtonState.Pressed && BoneTree.SelectedItems.Count > 0 && _canDoDragDropOperation)
            {
                Dispatcher.BeginInvoke(new Action(() => DragDrop.DoDragDrop(BoneTree, BoneTree.SelectedItems, DragDropEffects.All)),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void BoneTree_SelectedItemsChanged(object sender, List<TreeViewItem> items)
    {
        if (items.Count <= 0 || items[0] is not { Tag: Bone bone }) return;
        SelectedBoneXPosition.Bind(TextBox.TextProperty, bone, "Position.X");
        SelectedBoneYPosition.Bind(TextBox.TextProperty, bone, "Position.Y");
        SelectedBoneZPosition.Bind(TextBox.TextProperty, bone, "Position.Z");

        if (OnlySelected.IsChecked == true)
            Regex_Changed(sender, null);
    }

    private static bool CanDrop(ItemsControl source, TreeViewItem? target) 
    {
        bool DeepContains(ItemsControl container, TreeViewItem item) => container == item || container.Items.Cast<TreeViewItem>().Any(i => DeepContains(i, item));
        return !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && target != null && !DeepContains(source, target) && source.Parent != target;
    }

    private static void Reparent(FrameworkElement source, ItemsControl target)
    {
        if (target.Tag is not Bone targetBone || source.Tag is not Bone draggedBone || source.Parent is not ItemsControl parent) return;
        parent.Items.Remove(source);
        draggedBone.Parent = targetBone;
        target.Items.Add(source);
    }

    private void TreeItem_Drop(object sender, DragEventArgs e)
    {
        var sources = new List<TreeViewItem>(BoneTree.SelectedItems); // e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;

        var target = (TreeViewItem)sender;
        sources.ForEach(source =>
        {
            if (CanDrop(source, target)) 
            {
                Reparent(source, target);
            }

        });
        DragLeaveHandler(sender, e);
        e.Handled = true;
    }
    private void CutBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = BoneTree.SelectedItems.Count > 0;
    }
    private void CutBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        _cutBones.ForEach(t => { t.Foreground = Brushes.Black; t.FontWeight = FontWeights.Normal; } ) ;
        _cutBones = new List<TreeViewItem>(BoneTree.SelectedItems);
        _cutBones.ForEach(t => { t.Foreground = Brushes.Gray; t.FontWeight = FontWeights.Bold; });
    }
    private void PasteBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _cutBones.Count > 0 && BoneTree.SelectedItems.Count == 1 && _cutBones.Any(t => CanDrop(t, BoneTree.SelectedItems.First()));
    }
    private void PasteBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var target = BoneTree.SelectedItems.First();
        _cutBones.ForEach(source =>
        {
            if (CanDrop(source, target))
            {
                Reparent(source, target);
            }

        });
        _cutBones.ForEach(t => { t.Foreground = Brushes.Black; t.FontWeight = FontWeights.Normal; });
        _cutBones.Clear();
    }
    
    private void CopyBonePositionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = BoneTree.SelectedItems.Count == 1;
    }
    private void CopyBonePositionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (BoneTree.SelectedItems[0].Tag is not Bone bone) return;
        copiedPosition = bone.Position;
    }
    private void PasteBonePositionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = copiedPosition != null && BoneTree.SelectedItems.Count == 1;
    }
    private void PasteBonePositionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (BoneTree.SelectedItems[0].Tag is not Bone bone || copiedPosition == null) return;
        bone.Position = copiedPosition.Value;
    }

    private void NewBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = BoneTree.SelectedItems.Count == 1;
    }
    private void NewBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        _loader.AddBone(BoneTree.SelectedItems.First().Tag as Bone);
        RenderBoneTree();
    }
    private void MakeRootBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = BoneTree.SelectedItems.Count == 1;
    }
    private void MakeRootBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (BoneTree.SelectedItems[0].Tag is not Bone bone) return;
        _loader.MakeRoot(bone);
        RenderBoneTree();
    }
    private void MirrorBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute =  BoneTree.SelectedItems.Count == 1 &&
                        BoneTree.SelectedItems.First().Tag is Bone bone1 &&
                       (bone1.TranslatedName.Contains("right") || bone1.TranslatedName.Contains("left"));
    }
    private void MirrorBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        _loader.MirrorBoneTranslation((BoneTree.SelectedItems.First().Tag as Bone)!);
        RenderBoneTree();
    }
    private void AppendCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = BoneTree.SelectedItems.Count == 1;
    }
    private void AppendCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (BoneTree.SelectedItems.First().Tag is not Bone bone) return;

        OpenFileDialog ofd = new()
        {
            CheckFileExists = true,
            Title = "Select the .mesh.ascii file to edit",
            Filter = "XPS .mesh.ascii|*.mesh.ascii",
            Multiselect = false
        };
        if (ofd.ShowDialog() != true) return;
        Saving.Visibility = Visibility.Visible;
        Progress.Visibility = Visibility.Hidden;

        if(ofd.FileName.EndsWith(".mesh.ascii"))
            LoadMeshAsciiFile(ofd.FileName, bone);
    }

    private void BoneTree_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        BoneTreeScroll.ScrollToVerticalOffset(BoneTreeScroll.VerticalOffset - e.Delta / 3.0);
    }
    
    private void UpdateButtonVisibilities()
    {
        SaveMeshButton.Visibility = string.IsNullOrEmpty(_originalMeshAsciiName) && string.IsNullOrEmpty(_originalPoseName) ? Visibility.Collapsed : Visibility.Visible;
        SaveBoneDictButton.Visibility = string.IsNullOrEmpty(_originalMeshAsciiName) &&
                                        string.IsNullOrEmpty(_originalPoseName) &&
                                        string.IsNullOrEmpty(_originalBonedictName)
            ? Visibility.Collapsed
            : Visibility.Visible;
        UnloadAllButton.Visibility = string.IsNullOrEmpty(_originalMeshAsciiName) &&
                                     string.IsNullOrEmpty(_originalPoseName) &&
                                     string.IsNullOrEmpty(_originalBonedictName)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
    private void LoadMeshAsciiFile(string fileName, Bone? appendTo = null)
    {
        _originalMeshAsciiName ??= string.Join("",fileName.SkipLast(".mesh.ascii".Length));
        UpdateButtonVisibilities();

        _loader.LoadAsciiFile(fileName, appendTo);

        Refresh(true);

        Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
        Progress.Dispatcher.Invoke(() => Progress.Visibility = Visibility.Visible);
    }

    private void LoadPoseFile(string fileName)
    {
        _originalPoseName ??= string.Join("",fileName.SkipLast(".pose".Length));
        UpdateButtonVisibilities();

        _loader.LoadPoseFile(fileName);

        Refresh(true);

        Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
        Progress.Dispatcher.Invoke(() => Progress.Visibility = Visibility.Visible);
    }

    private void LoadMeshFileDialog(object sender, RoutedEventArgs e)
    {
        SavingMessage.Content = "Loading, please wait.";

        OpenFileDialog ofd = new()
        {
            CheckFileExists = true,
            Title = "Select the .mesh.ascii file or .pose to edit",
            Filter = "XPS .mesh.ascii|*.mesh.ascii|XPS .pose|*.pose",
            Multiselect = false
        };
        if (ofd.ShowDialog() != true) return;
        Saving.Visibility = Visibility.Visible;
        Progress.Visibility = Visibility.Hidden;

        if(ofd.FileName.EndsWith(".mesh.ascii"))
            LoadMeshAsciiFile(ofd.FileName);
        if(ofd.FileName.EndsWith(".pose"))
            LoadPoseFile(ofd.FileName);
    }

    private void LoadBonesFile(string fileName, bool keepAll = true)
    {
        _originalBonedictName = fileName;
        UpdateButtonVisibilities();
        
        Progress.Value = 0;
        Saving.Visibility = Visibility.Visible;
        _loader.LoadBoneFile(fileName, keepAll, n => Progress.Maximum = n, IncreaseProgress);

        RenderBoneTree();
    }

    private void LoadBonesFileDialog(object sender, RoutedEventArgs e)
    {
        SavingMessage.Content = "Loading, please wait.";

        OpenFileDialog ofd = new()
        {
            CheckFileExists = true,
            Title = "Select the bone list names to load",
            Filter = "BoneDict file|*.txt",
            Multiselect = false,
            InitialDirectory = "F:\\3D-Models\\_Bonedict"
        };
        if (ofd.ShowDialog() == true)
        {
            LoadBonesFile(ofd.FileName);
        }
    }

    private void SaveBonesFileDialog(object sender, RoutedEventArgs e)
    {
        var fileName = Path.GetFileNameWithoutExtension(_originalBonedictName ?? "Bonedict");
        var directory = Path.GetDirectoryName(_originalBonedictName ?? "Bonedict");
        SavingMessage.Content = "Saving, please wait.";

        SaveFileDialog sfd = new()
        {
            Title = "Select where to save the bone list names",
            AddExtension = true,
            Filter = "BoneDict file|*.txt",
            FileName = fileName,
            InitialDirectory = directory
        };
        if (sfd.ShowDialog() != true) return;
        _originalBonedictName = sfd.FileName;
        Progress.Maximum = _loader.Bones.Count(b => b.OriginalName != b.TranslatedName);
        Progress.Value = 0;
        Saving.Visibility = Visibility.Visible;
        new Thread(() => _loader.SaveBonedict(sfd.FileName, IncreaseProgress)).Start();
    }

    private void SaveMeshFileDialog(object sender, RoutedEventArgs e)
    {

        SavingMessage.Content = "Saving, please wait.";

        var fileName = Path.GetFileNameWithoutExtension(_originalMeshAsciiName ?? _originalPoseName ?? "generic_item");
        var directory = Path.GetDirectoryName(_originalMeshAsciiName ?? _originalPoseName ?? "generic_item");
        if (!fileName.EndsWith("_renamed")) fileName += "_renamed";

        SaveFileDialog sfd = new()
        {
            Title = "Select where to save the resulting mesh",
            AddExtension = true,
            Filter = "XPS .mesh.ascii|*.mesh.ascii|XPS .pose|*.pose",
            FilterIndex = string.IsNullOrEmpty(_originalPoseName) ? 1 : 2,
            FileName = fileName,
            InitialDirectory = directory
        };
        if (sfd.ShowDialog() != true) return;
        Progress.Maximum = _loader.Meshes.Count(m => !m.Exclude) + _loader.Meshes.Where(m => !m.Exclude).SelectMany(mesh => mesh.UsedBones).Distinct().Count();
        Progress.Value = 0;
        Saving.Visibility = Visibility.Visible;

        new Thread(() =>
        {
            if (sfd.FileName.EndsWith(".mesh.ascii"))
            {
                _originalMeshAsciiName ??= sfd.FileName;
                if (_loader.SaveAscii(sfd.FileName, IncreaseProgress)) return;
                MessageBox.Show("conflicting bone and/or mesh names found, or invalid Materials, solve them before saving", "Conflict found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.OK);
                Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
            }
            else if (sfd.FileName.EndsWith(".pose"))
            {
                _originalPoseName ??= sfd.FileName;
                _loader.SavePose(sfd.FileName, IncreaseProgress);
            }
        }).Start();
    }

    private void UnloadAll(object sender, RoutedEventArgs e)
    {
        _loader.Bones.Clear();
        _loader.Meshes.Clear();
        MaterialManager.Materials.Clear();
        RenderBoneTree();
        RenderMaterials();
        _originalBonedictName = null;
        _originalMeshAsciiName = null;
        _originalPoseName = null;
        SaveMeshButton.Visibility = Visibility.Collapsed;
        SaveBoneDictButton.Visibility = Visibility.Collapsed;
        UnloadAllButton.Visibility = Visibility.Collapsed;
    }
        
    private void IncreaseProgress()
    {
        Progress.Dispatcher.Invoke(() =>
        {
            Progress.Value++;
            if (Progress.Value >= Progress.Maximum)
                Saving.Visibility = Visibility.Hidden;
        });
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
        fileNames?.ToList().ForEach(fileName =>
        {
            if (fileName.EndsWith(".mesh.ascii")) LoadMeshAsciiFile(fileName);
            if (fileName.EndsWith(".pose")) LoadPoseFile(fileName);
            if (fileName.EndsWith(".txt")) LoadBonesFile(fileName);
        });
    }
        
    private static async Task<bool> UserKeepsTyping(TextBox t, int delay = 500) {
        var txt = t.Text;
        await Task.Delay(delay);
        return txt != t.Text;
    }

    private async void Filter_Changed(object sender, EventArgs e)
    {
        if(sender is TextBox t && await UserKeepsTyping(t)) return;

        Refresh(true);
    }

    private async void Regex_Changed(object sender, EventArgs? e)
    {
        if (sender is TextBox t && await UserKeepsTyping(t)) return;

        bool IsIncluded(Bone b)
        {
            if (OnlySelected.IsChecked != true) return true;
            if (Children.IsChecked == true)
                return BoneTree.SelectedItems.Any(item => item.Tag == b) || (b.Parent != null && IsIncluded(b.Parent));
            return BoneTree.SelectedItems.Any(item => item.Tag == b);
        }

        var renameIndexes = new Dictionary<string, int>();
        var groupIndexes = _loader.FindBoneGroups(RegexOriginal.Text, IsIncluded);

        _loader.Bones.ToList().ForEach(b =>
            b.ApplyRegex(RegexOriginal.Text, RegexResult.Text, renameIndexes, groupIndexes.GetValueOrDefault(b) ?? new List<int>(), b2 => !IsIncluded((Bone)b2)));
        renameIndexes = new Dictionary<string, int>();
        _loader.Meshes.ForEach(m =>
            m.ApplyRegex(RegexOriginal.Text, RegexResult.Text, renameIndexes, _ => RenameMeshes.IsChecked == false));
        renameIndexes = new Dictionary<string, int>();
        _loader.Meshes.SelectMany(m => m.Material.Textures.Values).ToList().ForEach(tex =>
            tex.ApplyRegex(RegexOriginal.Text, RegexResult.Text, renameIndexes, _ => RenameMeshes.IsChecked == true));
    }

    private void ApplyRename(object sender, RoutedEventArgs e)
    {
        if (_selectedTab == TabBones)
            FilteredBones.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
        if (_selectedTab == TabMaterials)
        {
            if (RenameMeshes.IsChecked == true)
                FilteredMeshes.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
            else
                _loader.Meshes.SelectMany(m => m.Material.Textures.Values).Where(b => b.TranslatingName != null).ToList()
                    .ForEach(b => b.TranslatedName = b.TranslatingName!);
        }

        Regex_Changed(RegexResult, null);
    }

    private void TabSelected(object sender, RoutedEventArgs e)
    {
        if (sender is not TabControl { SelectedItem: TabItem tab }) return;
        _selectedTab = tab;
        OnlySelected.Visibility = _selectedTab == TabBones ? Visibility.Visible : Visibility.Collapsed;
        Children.Visibility = _selectedTab == TabBones ? Visibility.Visible : Visibility.Collapsed;
        RenameMeshes.Visibility = _selectedTab == TabBones ? Visibility.Collapsed : Visibility.Visible;
        Refresh();
    }
}