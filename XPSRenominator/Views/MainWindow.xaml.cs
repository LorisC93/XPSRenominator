using ColorPicker;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using XPSRenominator.Controllers;
using XPSRenominator.Models;

namespace XPSRenominator
{

    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MeshAsciiLoader _loader = new();
        private string? _originalMeshAsciiName;
        private string? _originalBonedictName;
        private string _selectedTab = "Bones";

        public MainWindow()
        {
            InitializeComponent();
        }

        private List<TreeViewItem> _cutBones = new();

        private IEnumerable<Bone> FilteredBones
        {
            get
            {
                bool ContainsCondition(Translatable bone) => bone.OriginalName.Contains(beforeFilter.Text.ToLower()) && bone.TranslatedName.Contains(afterFilter.Text.ToLower());
                bool OnlyUntranslatedCondition(Translatable bone) => onlyUntranslated.IsChecked == false || bone.OriginalName == bone.TranslatedName;
                bool OnlyConflictingCondition(Bone bone) => onlyConflicting.IsChecked == false || (bone.FromMeshAscii && _loader.Bones.Where(b => b.FromMeshAscii && b != bone).Any(b => bone.TranslatedName == b.TranslatedName));

                return _loader.Bones.Where(b => ContainsCondition(b) && OnlyUntranslatedCondition(b) && OnlyConflictingCondition(b));
            }
        }
        private IEnumerable<Mesh> FilteredMeshes
        {
            get
            {
                bool ContainsCondition(Translatable mesh) => mesh.OriginalName.Contains(beforeFilter.Text.ToLower()) && mesh.TranslatedName.Contains(afterFilter.Text.ToLower());
                bool OnlyUntranslatedCondition(Translatable mesh) => onlyUntranslated.IsChecked == false || mesh.OriginalName == mesh.TranslatedName;
                bool OnlyConflictingCondition(Translatable mesh) => onlyConflicting.IsChecked == false || _loader.Meshes.Where(m => m != mesh).Any(b => mesh.TranslatedName == b.TranslatedName);

                return _loader.Meshes.Where(b => ContainsCondition(b) && OnlyUntranslatedCondition(b) && OnlyConflictingCondition(b));
            }
        }

        private void RenderBones()
        {
            BonesGrid.Dispatcher.Invoke(() =>
            {
                BonesGrid.Children.Clear();
                BonesGrid.RowDefinitions.Clear();

                foreach (var bone in FilteredBones)
                {
                    BonesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

                    TextBlock originalNameTextBlock = new() { Text = bone.OriginalName };
                    Grid.SetRow(originalNameTextBlock, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(originalNameTextBlock, 0);
                    BonesGrid.Children.Add(originalNameTextBlock);

                    TextBlock arrowTextBlock = new() { Text = "➔" };
                    Grid.SetRow(arrowTextBlock, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(arrowTextBlock, 1);
                    arrowTextBlock.Margin = new Thickness(0, 0, 5, 0);
                    BonesGrid.Children.Add(arrowTextBlock);

                    TextBox translationTextBox = new() { Text = bone.TranslatedName };
                    Grid.SetRow(translationTextBox, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(translationTextBox, 2);
                    translationTextBox.Bind(TextBox.TextProperty, bone, "TranslatedName");
                    BonesGrid.Children.Add(translationTextBox);

                    TextBlock translatingTextBlock = new() { Text = bone.TranslatingName };
                    Grid.SetRow(translatingTextBlock, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(translatingTextBlock, 3);
                    translatingTextBlock.Foreground = Brushes.Red;
                    translatingTextBlock.Bind(TextBlock.TextProperty, bone, "TranslatingName");
                    BonesGrid.Children.Add(translatingTextBlock);
                }
            });
            RenderTree();
        }

        private void RenderTree()
        {
            void RenderTreeItem(Bone bone, ItemsControl? item = null)
            {
                StackPanel header = new()
                {
                    Orientation = Orientation.Horizontal
                };
                TextBlock name = new();
                name.Bind(TextBlock.TextProperty, bone, "TranslatedName");
                //TextBox posX = new() { Margin = new(5, 0, 0, 0), Width = 50 };
                //posX.Bind(TextBox.TextProperty, bone, "Position[0]");
                //TextBox posY = new() { Margin = new(5, 0, 0, 0), Width = 50 };
                //posY.Bind(TextBox.TextProperty, bone, "Position[1]");
                //TextBox posZ = new() { Margin = new(5, 0, 0, 0), Width = 50 };
                //posZ.Bind(TextBox.TextProperty, bone, "Position[2]");
                header.Children.Add(name);
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
                    e.Effects = boneTree.SelectedItems.Any(t => CanDrop(t, (TreeViewItem)sender)) ? DragDropEffects.Move : DragDropEffects.None;
                    e.Handled = true;
                };
                treeItem.DragLeave += DragLeaveHandler;
                treeItem.Drop += TreeItem_Drop;
                foreach (Bone child in _loader.Bones.Where(b => b.Parent == bone && b.FromMeshAscii))
                {
                    RenderTreeItem(child, treeItem);
                }
                if (item != null)
                    item.Items.Add(treeItem);
                else
                    boneTree.Items.Add(treeItem);
            }

            boneTree.Dispatcher.Invoke(() =>
            {
                boneTree.Items.Clear();
                foreach (Bone bone in _loader.Bones.Where(b => b.Parent == null && b.FromMeshAscii))
                {
                    RenderTreeItem(bone);
                }
            });
        }

        private void RenderMeshes()
        {
            MeshesGrid.Dispatcher.Invoke(() =>
            {
                MeshesGrid.Children.Clear();
                MeshesGrid.RowDefinitions.Clear();

                foreach (Mesh mesh in FilteredMeshes)
                {
                    ContextMenu contextMenu = new();
                    MenuItem clone = new() { Header = "Clone" };
                    MenuItem delete = new() { Header = "Delete" };
                    clone.Click += (sender, e) => CloneMeshCommand_Executed(sender, mesh);
                    delete.Click += (sender, e) => DeleteMeshCommand_Executed(sender, mesh);
                    contextMenu.Items.Add(clone);
                    contextMenu.Items.Add(delete);

                    MeshesGrid.RowDefinitions.Add(new() { Height = new GridLength(20) });

                    TextBlock originalNameTextBlock = new() { Text = mesh.OriginalName, ContextMenu = contextMenu };
                    Grid.SetRow(originalNameTextBlock, MeshesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(originalNameTextBlock, 0);
                    MeshesGrid.Children.Add(originalNameTextBlock);

                    TextBlock arrowTextBlock = new() { Text = "➔" };
                    Grid.SetRow(arrowTextBlock, MeshesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(arrowTextBlock, 1);
                    arrowTextBlock.Margin = new Thickness(0, 0, 5, 0);
                    MeshesGrid.Children.Add(arrowTextBlock);

                    TextBox translationTextBox = new() { Text = mesh.TranslatedName };
                    Grid.SetRow(translationTextBox, MeshesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(translationTextBox, 2);
                    translationTextBox.Bind(TextBox.TextProperty, mesh, "TranslatedName");
                    MeshesGrid.Children.Add(translationTextBox);

                    TextBlock translatingTextBlock = new() { Text = mesh.TranslatingName };
                    Grid.SetRow(translatingTextBlock, MeshesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(translatingTextBlock, 3);
                    translatingTextBlock.Foreground = Brushes.Red;
                    translatingTextBlock.Bind(TextBlock.TextProperty, mesh, "TranslatingName");
                    MeshesGrid.Children.Add(translatingTextBlock);
                }
            });
            RenderTextures();
        }

        private void RenderTextures()
        {
            MaterialsPanel.Dispatcher.Invoke(() =>
            {
                MaterialsPanel.Children.Clear();

                foreach (Material material in MaterialManager.Materials)
                {
                    StackPanel groupBoxHeader = new() { Orientation = Orientation.Horizontal, AllowDrop = true };

                    ComboBox meshRenderGroupComboBox = new() { ItemsSource = RenderGroup.List, SelectedItem = material.RenderGroup, Margin = new Thickness(0, 0, 5, 0) };
                    meshRenderGroupComboBox.Bind(Selector.SelectedItemProperty, material, "RenderGroup");
                    meshRenderGroupComboBox.SelectionChanged += delegate
                    {
                        RenderTextures();
                    };
                    groupBoxHeader.Children.Add(meshRenderGroupComboBox);

                    TextBox meshRenderParameter1 = new() { Text = material.RenderParameters[0].ToString(CultureInfo.InvariantCulture), Margin = new Thickness(5, 0, 2, 0), MinWidth = 25 };
                    TextBox meshRenderParameter2 = new() { Text = material.RenderParameters[1].ToString(CultureInfo.InvariantCulture), Margin = new Thickness(2, 0, 2, 0), MinWidth = 25 };
                    TextBox meshRenderParameter3 = new() { Text = material.RenderParameters[2].ToString(CultureInfo.InvariantCulture), Margin = new Thickness(2, 0, 0, 0), MinWidth = 25 };
                    meshRenderParameter1.Bind(TextBox.TextProperty, material, "RenderParameters[0]");
                    meshRenderParameter2.Bind(TextBox.TextProperty, material, "RenderParameters[1]");
                    meshRenderParameter3.Bind(TextBox.TextProperty, material, "RenderParameters[2]");
                    groupBoxHeader.Children.Add(meshRenderParameter1);
                    groupBoxHeader.Children.Add(meshRenderParameter2);
                    groupBoxHeader.Children.Add(meshRenderParameter3);

                    GroupBox meshNameGroupBox = new() { Header = groupBoxHeader, Tag = material, Padding = new Thickness(0, 2, 0, 2) };
                    meshNameGroupBox.DragEnter += DragEnterHandler;
                    meshNameGroupBox.DragLeave += DragLeaveHandler;
                    meshNameGroupBox.Drop += MaterialGroup_Drop;
                    MaterialsPanel.Children.Add(meshNameGroupBox);

                    Grid texturesGrid = new();
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    meshNameGroupBox.Content = texturesGrid;

                    for (int i = 0; i < material.RenderGroup.SupportedTextureTypes.Count; i++)
                    {
                        if (material.Textures.Count <= i)
                        {
                            material.Textures.Add(new Texture());
                        }
                        Texture texture = material.Textures.ElementAt(i);

                        texturesGrid.RowDefinitions.Add(new RowDefinition() );

                        TextBlock textureTypeTextBlock = new() { Text = material.RenderGroup.SupportedTextureTypes[i].Code(), Margin = new(0, 0, 5, 0) };
                        Grid.SetRow(textureTypeTextBlock, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(textureTypeTextBlock, 0);
                        textureTypeTextBlock.Foreground = Brushes.Gray;
                        textureTypeTextBlock.VerticalAlignment = VerticalAlignment.Center;
                        texturesGrid.Children.Add(textureTypeTextBlock);

                        TextBox translationTextBox = new() { Text = texture.TranslatedName };
                        Grid.SetRow(translationTextBox, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(translationTextBox, 1);
                        translationTextBox.Bind(TextBox.TextProperty, texture, "TranslatedName");
                        translationTextBox.VerticalAlignment = VerticalAlignment.Center;
                        texturesGrid.Children.Add(translationTextBox);

                        TextBlock translatingTextBlock = new() { Text = texture.TranslatingName };
                        Grid.SetRow(translatingTextBlock, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(translatingTextBlock, 2);
                        translatingTextBlock.Foreground = Brushes.Red;
                        translatingTextBlock.VerticalAlignment = VerticalAlignment.Center;
                        translatingTextBlock.Bind(TextBlock.TextProperty, texture, "TranslatingName");
                        texturesGrid.Children.Add(translatingTextBlock);
                    }

                    StackPanel meshesPanel = new() { Orientation = Orientation.Vertical };
                    Grid.SetRowSpan(meshesPanel, texturesGrid.RowDefinitions.Count);
                    Grid.SetColumn(meshesPanel, 3);

                    foreach (Mesh mesh in _loader.Meshes.Where(m => m.Material == material))
                    {
                        StackPanel meshLine = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                        PortableColorPicker colorPicker = new() { Width = 15, Height = 15, Margin = new Thickness(0, 0, 2, 0), ShowAlpha = false, SelectedColor = mesh.Vertices.First().Color };
                        colorPicker.ColorChanged += (sender, e) => { mesh.Vertices.ForEach(v => v.Color = colorPicker.SelectedColor); };
                        meshLine.Children.Add(colorPicker);

                        TextBlock meshTextBlock = new() { Text = mesh.TranslatedName, Tag = mesh };
                        meshTextBlock.Bind(TextBlock.TextProperty, mesh, "TranslatedName");
                        meshLine.Children.Add(meshTextBlock);


                        meshesPanel.Children.Add(meshLine);
                    }

                    texturesGrid.Children.Add(meshesPanel);
                }
            });
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
            var target = sender as GroupBox;
            var source = e.Data.GetData(typeof(Mesh)) as Mesh;
            if (target?.Tag is Material material && source is not null && source.Material != material)
            {
                if (_loader.Meshes.Count(m => m.Material == source.Material) == 1)
                    MaterialManager.Materials.Remove(source.Material);

                source.Material = material;
            }
            DragLeaveHandler(sender, e);
            e.Handled = true;
            RenderTextures();
        }
        private void MaterialPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Mesh)) is Mesh mesh)
            {
                if (_loader.Meshes.Count(m => m.Material == mesh.Material) == 1)
                    MaterialManager.Materials.Remove(mesh.Material);

                mesh.Material = new Material();
                MaterialManager.Materials.Add(mesh.Material);
            }
            DragLeaveHandler(sender, e);
            e.Handled = true;
            RenderTextures();
        }

        private void BoneTree_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && boneTree.SelectedItems.Count > 0)
            {
                DragDrop.DoDragDrop(boneTree, boneTree.SelectedItems, DragDropEffects.All);
            }
        }

        private void BoneTree_SelectedItemsChanged(object sender, List<TreeViewItem> items)
        {
            if (items.Count <= 0 || items[0] is not { Tag: Bone bone }) return;
            selectedBoneXPosition.Bind(TextBox.TextProperty, bone, "Position[0]");
            selectedBoneYPosition.Bind(TextBox.TextProperty, bone, "Position[1]");
            selectedBoneZPosition.Bind(TextBox.TextProperty, bone, "Position[2]");
        }

        private static bool CanDrop(ItemsControl source, TreeViewItem? target) 
        {
            bool DeepContains(ItemsControl container, TreeViewItem item) => container == item || container.Items.Cast<TreeViewItem>().Any(i => DeepContains(i, item));
            return target != null && !DeepContains(source, target) && source.Parent != target;
        }

        private static void Reparent(FrameworkElement source, ItemsControl target)
        {
            if (target.Tag is not Bone targetBone || source.Tag is not Bone draggedBone || source.Parent is not TreeViewItem parent) return;
            parent.Items.Remove(source);
            draggedBone.Parent = targetBone;
            target.Items.Add(source);
        }

        private void TreeItem_Drop(object sender, DragEventArgs e)
        {
            List<TreeViewItem> sources = new(boneTree.SelectedItems); // e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;

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
            e.CanExecute = boneTree.SelectedItems.Count > 0;
        }
        private void CutBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _cutBones.ForEach(t => { t.Foreground = Brushes.Black; t.FontWeight = FontWeights.Normal; } ) ;
            _cutBones = new List<TreeViewItem>(boneTree.SelectedItems);
            _cutBones.ForEach(t => { t.Foreground = Brushes.Gray; t.FontWeight = FontWeights.Bold; });
        }
        private void PasteBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _cutBones.Count > 0 && boneTree.SelectedItems.Count == 1 && _cutBones.Any(t => CanDrop(t, boneTree.SelectedItems.First()));
        }
        private void PasteBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var target = boneTree.SelectedItems.First();
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
        private void NewBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = boneTree.SelectedItems.Count == 1;
        }
        private void NewBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _loader.AddBone(boneTree.SelectedItems.First().Tag as Bone);
            RenderBones();
        }
        private void MakeRootBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = boneTree.SelectedItems.Count == 1;
        }
        private void MakeRootBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (boneTree.SelectedItems.First().Tag is not Bone bone) return;
            _loader.MakeRoot(bone);
            RenderBones();
        }

        private void CloneMeshCommand_Executed(object sender, Mesh mesh)
        {
            _loader.CloneMesh(mesh);
            RenderMeshes();
        }
        private void DeleteMeshCommand_Executed(object sender, Mesh mesh)
        {
            _loader.DeleteMesh(mesh);
            RenderMeshes();
        }

        private void BoneTree_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            boneTreeScroll.ScrollToVerticalOffset(boneTreeScroll.VerticalOffset - e.Delta / 3);
        }

        private void LoadMeshAsciiFile(string fileName)
        {
            _originalMeshAsciiName ??= string.Join("",fileName.SkipLast(".mesh.ascii".Length));

            _loader.LoadAsciiFile(fileName);

            RenderBones();
            RenderMeshes();

            Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
            Progress.Dispatcher.Invoke(() => Progress.Visibility = Visibility.Visible);
        }

        private void LoadMeshFileDialog(object sender, RoutedEventArgs e)
        {
            SavingMessage.Content = "Loading, please wait.";

            OpenFileDialog ofd = new()
            {
                CheckFileExists = true,
                Title = "Select the .mesh.ascii file to edit",
                Filter = "XPS .mesh.ascii|*.mesh.ascii",
                Multiselect = false
            };
            if (ofd.ShowDialog() == true)
            {
                Saving.Visibility = Visibility.Visible;
                Progress.Visibility = Visibility.Hidden;

                LoadMeshAsciiFile(ofd.FileName);
            }
        }

        private void LoadBonesFile(string fileName, bool keepAll = true)
        {
            _originalBonedictName = fileName;

            _loader.LoadBoneFile(fileName, keepAll);

            RenderBones();
        }

        private void LoadBonesFileDialog(object sender, RoutedEventArgs e)
        {
            SavingMessage.Content = "Loading, please wait.";

            OpenFileDialog ofd = new()
            {
                CheckFileExists = true,
                Title = "Select the bone list names to load",
                Filter = "BoneDict file|*.txt",
                Multiselect = false
            };
            if (ofd.ShowDialog() == true)
            {
                LoadBonesFile(ofd.FileName);
            }
        }

        private void SaveBonesFileDialog(object sender, RoutedEventArgs e)
        {
            SavingMessage.Content = "Saving, please wait.";

            SaveFileDialog sfd = new()
            {
                Title = "Select where to save the bone list names",
                AddExtension = true,
                Filter = "BoneDict file|*.txt",
                FileName = _originalBonedictName ?? "Bonedict"
            };
            if (sfd.ShowDialog() == true)
            {
                Progress.Maximum = _loader.Bones.Count(b => b.OriginalName != b.TranslatedName);
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;
                new Thread(() => _loader.SaveBones(sfd.FileName, IncreaseProgress)).Start();
            }
        }

        private void SaveMeshFileDialog(object sender, RoutedEventArgs e)
        {
            SavingMessage.Content = "Saving, please wait.";

            SaveFileDialog sfd = new()
            {
                Title = "Select where to save the resulting mesh",
                AddExtension = true,
                Filter = "XPS .mesh.ascii|*.mesh.ascii",
                FileName = _originalMeshAsciiName != null ? _originalMeshAsciiName + "_renamed" : "generic_item"
            };
            if (sfd.ShowDialog() != true) return;
            Progress.Maximum = _loader.Bones.Count(b => b.FromMeshAscii) + _loader.Meshes.Count;
            Progress.Value = 0;
            Saving.Visibility = Visibility.Visible;

            new Thread(() =>
            {
                if (_loader.SaveAscii(sfd.FileName, IncreaseProgress)) return;
                MessageBox.Show("conflicting bone and/or mesh names found, solve them before saving", "Conflict found", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
            }).Start();
        }

        private void UnloadAll(object sender, RoutedEventArgs e)
        {
            _loader.Bones.Clear();
            _loader.Meshes.Clear();
            MaterialManager.Materials.Clear();
            RenderBones();
            RenderMeshes();
            _originalBonedictName = null;
            _originalMeshAsciiName = null;
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
                if (fileName.EndsWith(".txt")) LoadBonesFile(fileName);
            });
        }

        private void Filter_TextChanged(object sender, EventArgs e)
        {
            RenderBones();
            RenderMeshes();
        }

        private void Regex_TextChanged(object sender, TextChangedEventArgs? e)
        {
            _loader.Bones.ForEach(b => b.ApplyRegex(regexOriginal.Text, regexResult.Text));
            _loader.Meshes.ForEach(m => m.ApplyRegex(regexOriginal.Text, regexResult.Text));
            _loader.Meshes.SelectMany(m => m.Material.Textures).ToList().ForEach(t => t.ApplyRegex(regexOriginal.Text, regexResult.Text));
            
        }

        private void ApplyRename(object sender, RoutedEventArgs e)
        {
            switch (_selectedTab)
            {
                case "Bones":
                    FilteredBones.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
                case "Meshes":
                    FilteredMeshes.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
                case "Textures":
                    _loader.Meshes.SelectMany(m => m.Material.Textures).Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
            }
            Regex_TextChanged(regexResult, null);
        }

        private void BonesOrMeshesTab_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TabControl { SelectedItem: TabItem tab })
            {
                _selectedTab = tab.Header.ToString()!;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
