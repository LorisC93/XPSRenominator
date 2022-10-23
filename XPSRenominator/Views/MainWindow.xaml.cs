using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
        private readonly MeshAsciiLoader loader = new();
        private string? originalMeshAsciiName;
        private string? originalBonedictName;
        private string selectedTab = "Bones";

        public MainWindow()
        {
            InitializeComponent();
        }

        private List<TreeViewItem> cutBones = new List<TreeViewItem>();

        private IEnumerable<Bone> FilteredBones
        {
            get
            {
                bool containsCondition(Bone bone) => bone.OriginalName.Contains(beforeFilter.Text.ToLower()) && bone.TranslatedName.Contains(afterFilter.Text.ToLower());
                bool onlyUntranslatedCondition(Bone bone) => onlyUntranslated.IsChecked == false || bone.OriginalName == bone.TranslatedName;
                bool onlyConflictingCondition(Bone bone) => onlyConflicting.IsChecked == false || (bone.FromMeshAscii && loader.Bones.Where(b => b.FromMeshAscii && b != bone).Any(b => bone.TranslatedName == b.TranslatedName));

                return loader.Bones.Where(b => containsCondition(b) && onlyUntranslatedCondition(b) && onlyConflictingCondition(b));
            }
        }
        private IEnumerable<Mesh> FilteredMeshes
        {
            get
            {

                bool containsCondition(Mesh mesh) => mesh.OriginalName.Contains(beforeFilter.Text.ToLower()) && mesh.TranslatedName.Contains(afterFilter.Text.ToLower());
                bool onlyUntranslatedCondition(Mesh mesh) => onlyUntranslated.IsChecked == false || mesh.OriginalName == mesh.TranslatedName;
                bool onlyConflictingCondition(Mesh mesh) => onlyConflicting.IsChecked == false || loader.Meshes.Where(m => m != mesh).Any(b => mesh.TranslatedName == b.TranslatedName);

                return loader.Meshes.Where(b => containsCondition(b) && onlyUntranslatedCondition(b) && onlyConflictingCondition(b));
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
            void RenderTreeItem(Bone bone, TreeViewItem? item = null)
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
                foreach (Bone child in loader.Bones.Where(b => b.Parent == bone && b.FromMeshAscii))
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
                foreach (Bone bone in loader.Bones.Where(b => b.Parent == null && b.FromMeshAscii))
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

                    ComboBox meshRenderGroupComboBox = new() { ItemsSource = RenderGroup.List, SelectedItem = material.RenderGroup, Margin = new(0, 0, 5, 0) };
                    meshRenderGroupComboBox.Bind(ComboBox.SelectedItemProperty, material, "RenderGroup");
                    meshRenderGroupComboBox.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) 
                    {
                        RenderTextures();
                    };
                    groupBoxHeader.Children.Add(meshRenderGroupComboBox);

                    TextBox meshRenderParameter1 = new() { Text = material.RenderParameters[0].ToString(), Margin = new(5, 0, 2, 0), MinWidth = 25 };
                    TextBox meshRenderParameter2 = new() { Text = material.RenderParameters[1].ToString(), Margin = new(2, 0, 2, 0), MinWidth = 25 };
                    TextBox meshRenderParameter3 = new() { Text = material.RenderParameters[2].ToString(), Margin = new(2, 0, 0, 0), MinWidth = 25 };
                    meshRenderParameter1.Bind(TextBox.TextProperty, material, "RenderParameters[0]");
                    meshRenderParameter2.Bind(TextBox.TextProperty, material, "RenderParameters[1]");
                    meshRenderParameter3.Bind(TextBox.TextProperty, material, "RenderParameters[2]");
                    groupBoxHeader.Children.Add(meshRenderParameter1);
                    groupBoxHeader.Children.Add(meshRenderParameter2);
                    groupBoxHeader.Children.Add(meshRenderParameter3);

                    GroupBox meshNameGroupBox = new() { Header = groupBoxHeader, Tag = material, Padding = new(0, 2, 0, 2) };
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

                    foreach (Mesh mesh in loader.Meshes.Where(m => m.Material == material))
                    {
                        TextBlock meshTextBlock = new() { Text = mesh.TranslatedName, Tag = mesh };
                        meshTextBlock.Bind(TextBlock.TextProperty, mesh, "TranslatedName");
                        meshesPanel.Children.Add(meshTextBlock);
                    }

                    texturesGrid.Children.Add(meshesPanel);
                }
            });
        }


        private void DragEnterHandler(object sender, DragEventArgs e)
        {
            if (sender is Control control)
            {
                control.Background = Brushes.LightBlue;
                e.Handled = true;
            }
        }
        private void DragLeaveHandler(object sender, DragEventArgs e)
        {
            if (sender is Control control)
            {
                control.Background = null;
                e.Handled = true;
            }
        }

        private void MaterialPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.Source is TextBlock tb && tb.Tag is Mesh m)
            {
                DragDrop.DoDragDrop(MaterialsPanel, m, DragDropEffects.Move);
            }
        }

        private void MaterialGroup_Drop(object sender, DragEventArgs e)
        {
            GroupBox? target = sender as GroupBox;
            Mesh? source = e.Data.GetData(typeof(Mesh)) as Mesh;
            if (target?.Tag is Material material && source is Mesh mesh && mesh.Material != material)
            {
                if (loader.Meshes.Count(m => m.Material == mesh.Material) == 1)
                    MaterialManager.Materials.Remove(mesh.Material);

                mesh.Material = material;
            }
            DragLeaveHandler(sender, e);
            e.Handled = true;
            RenderTextures();
        }
        private void MaterialPanel_Drop(object sender, DragEventArgs e)
        {
            Mesh? source = e.Data.GetData(typeof(Mesh)) as Mesh;
            if (source is Mesh mesh)
            {
                if (loader.Meshes.Count(m => m.Material == mesh.Material) == 1)
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
            if (items.Count > 0 && items[0] is TreeViewItem item && item.Tag is Bone bone)
            {
                selectedBoneXPosition.Bind(TextBox.TextProperty, bone, "Position[0]");
                selectedBoneYPosition.Bind(TextBox.TextProperty, bone, "Position[1]");
                selectedBoneZPosition.Bind(TextBox.TextProperty, bone, "Position[2]");
            }
        }

        private bool CanDrop(TreeViewItem source, TreeViewItem? target) 
        {
            bool DeepContains(TreeViewItem container, TreeViewItem item) => container == item || container.Items.Cast<TreeViewItem>().Any(i => DeepContains(i, item));
            return target != null && !DeepContains(source, target) && source.Parent != target;
        }

        private void Reparent(TreeViewItem source, TreeViewItem target)
        {
            if (target?.Tag is Bone targetBone && source?.Tag is Bone draggedBone && source.Parent is TreeViewItem parent)
            {
                parent.Items.Remove(source);
                draggedBone.Parent = targetBone;
                target.Items.Add(source);
            }
        }

        private void TreeItem_Drop(object sender, DragEventArgs e)
        {
            List<TreeViewItem> sources = new(boneTree.SelectedItems); // e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;

            TreeViewItem target = (TreeViewItem)sender;
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
            cutBones.ForEach(t => { t.Foreground = Brushes.Black; t.FontWeight = FontWeights.Normal; } ) ;
            cutBones = new(boneTree.SelectedItems);
            cutBones.ForEach(t => { t.Foreground = Brushes.Gray; t.FontWeight = FontWeights.Bold; });
        }
        private void PasteBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = cutBones.Count > 0 && boneTree.SelectedItems.Count == 1 && cutBones.Any(t => CanDrop(t, boneTree.SelectedItems.First()));
        }
        private void PasteBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TreeViewItem target = boneTree.SelectedItems.First();
            cutBones.ForEach(source =>
            {
                if (CanDrop(source, target))
                {
                    Reparent(source, target);
                }

            });
            cutBones.ForEach(t => { t.Foreground = Brushes.Black; t.FontWeight = FontWeights.Normal; });
            cutBones.Clear();
        }
        private void NewBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = boneTree.SelectedItems.Count == 1;
        }
        private void NewBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            loader.AddBone(boneTree.SelectedItems.First().Tag as Bone);
            RenderBones();
        }
        private void MakeRootBoneCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = boneTree.SelectedItems.Count == 1;
        }
        private void MakeRootBoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (boneTree.SelectedItems.First().Tag is Bone bone)
            {
                loader.MakeRoot(bone);
                RenderBones();
            }
        }

        private void CloneMeshCommand_Executed(object sender, Mesh mesh)
        {
            loader.CloneMesh(mesh);
            RenderMeshes();
        }
        private void DeleteMeshCommand_Executed(object sender, Mesh mesh)
        {
            loader.DeleteMesh(mesh);
            RenderMeshes();
        }

        private void BoneTree_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            boneTreeScroll.ScrollToVerticalOffset(boneTreeScroll.VerticalOffset - e.Delta / 3);
        }

        private void LoadMeshAsciiFile(string fileName)
        {
            originalMeshAsciiName = string.Join("",fileName.SkipLast(".mesh.ascii".Length));
            loader.LoadAsciiFile(fileName);

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
            originalBonedictName = fileName;

            loader.LoadBoneFile(fileName, keepAll);

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
                FileName = originalBonedictName ?? "Bonedict"
            };
            if (sfd.ShowDialog() == true)
            {
                Progress.Maximum = loader.Bones.Where(b => b.OriginalName != b.TranslatedName).Count();
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;
                new Thread(() => loader.SaveBones(sfd.FileName, IncreaseProgress)).Start();
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
                FileName = originalMeshAsciiName != null ? originalMeshAsciiName + "_renamed" : "generic_item"
            };
            if (sfd.ShowDialog() == true)
            {
                Progress.Maximum = loader.Bones.Where(b => b.FromMeshAscii).Count() + loader.Meshes.Count;
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;

                new Thread(() =>
                {
                    if (!loader.SaveAscii(sfd.FileName, IncreaseProgress))
                    {
                        MessageBox.Show("conflicting bone and/or mesh names found, solve them before saving", "Confict found", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
                    }
                }).Start();
            }
        }

        private void UnloadAll(object sender, RoutedEventArgs e)
        {
            loader.Bones.Clear();
            loader.Meshes.Clear();
            MaterialManager.Materials.Clear();
            RenderBones();
            RenderMeshes();
            originalBonedictName = null;
            originalMeshAsciiName = null;
        }
        
        private void IncreaseProgress()
        {
            Progress.Dispatcher.Invoke(() =>
            {
                Progress.Value++;
                if (Progress.Value == Progress.Maximum)
                    Saving.Visibility = Visibility.Hidden;
            });
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
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
            loader.Bones.ForEach(b => b.ApplyRegex(regexOriginal.Text, regexResult.Text));
            loader.Meshes.ForEach(m => m.ApplyRegex(regexOriginal.Text, regexResult.Text));
            loader.Meshes.SelectMany(m => m.Material.Textures).ToList().ForEach(t => t.ApplyRegex(regexOriginal.Text, regexResult.Text));
            
        }

        private void ApplyRename(object sender, RoutedEventArgs e)
        {
            switch (selectedTab)
            {
                case "Bones":
                    FilteredBones.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
                case "Meshes":
                    FilteredMeshes.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
                case "Textures":
                    loader.Meshes.SelectMany(m => m.Material.Textures).Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
            }
            Regex_TextChanged(regexResult, null);
        }

        private void BonesorMeshesTab_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem tab)
            {
                selectedTab = tab.Header.ToString()!;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
