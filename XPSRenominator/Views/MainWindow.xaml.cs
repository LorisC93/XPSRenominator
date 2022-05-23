using Microsoft.Win32;
using System;
using System.Linq;
using System.Text.RegularExpressions;
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

        private void RenderBones()
        {
            BonesGrid.Dispatcher.Invoke(() =>
            {
                BonesGrid.Children.Clear();
                BonesGrid.RowDefinitions.Clear();

                bool containsCondition(Bone bone) => bone.OriginalName.Contains(beforeFilter.Text.ToLower()) && bone.TranslatedName.Contains(afterFilter.Text.ToLower());
                bool onlyUntranslatedCondition(Bone bone) => onlyUntranslated.IsChecked == false || bone.OriginalName == bone.TranslatedName;
                bool onlyConflictingCondition(Bone bone) => onlyConflicting.IsChecked == false || (bone.FromMeshAscii && loader.Bones.Where(b => b.FromMeshAscii && b != bone).Any(b => bone.TranslatedName == b.TranslatedName));

                foreach (var bone in loader.Bones.Where(b => containsCondition(b) && onlyUntranslatedCondition(b) && onlyConflictingCondition(b)))
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
                TreeViewItem treeItem = new() { Header = bone.TranslatedName, Tag = bone, IsExpanded = true };
                treeItem.Bind(HeaderedItemsControl.HeaderProperty, bone, "TranslatedName");
                treeItem.AllowDrop = true;
                treeItem.DragEnter += TreeItem_DragEnter;
                treeItem.DragLeave += TreeItem_DragLeave;
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

                bool containsCondition(Mesh mesh) => mesh.OriginalName.Contains(beforeFilter.Text.ToLower()) && mesh.TranslatedName.Contains(afterFilter.Text.ToLower());
                bool onlyUntranslatedCondition(Mesh mesh) => onlyUntranslated.IsChecked == false || mesh.OriginalName == mesh.TranslatedName;
                bool onlyConflictingCondition(Mesh mesh) => onlyConflicting.IsChecked == false || loader.Meshes.Where(m => m != mesh).Any(b => mesh.TranslatedName == b.TranslatedName);

                foreach (Mesh mesh in loader.Meshes.Where(b => containsCondition(b) && onlyUntranslatedCondition(b) && onlyConflictingCondition(b)))
                {
                    MeshesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

                    TextBlock originalNameTextBlock = new() { Text = mesh.OriginalName };
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
            TexturesPanel.Dispatcher.Invoke(() =>
            {
                TexturesPanel.Children.Clear();

                foreach (Mesh mesh in loader.Meshes)
                {
                    StackPanel groupBoxHeader = new() { Orientation = Orientation.Horizontal };

                    ComboBox meshRenderGroupComboBox = new() { ItemsSource = RenderGroup.List, SelectedItem = mesh.Material.RenderGroup, Margin = new(0, 0, 5, 0) };
                    meshRenderGroupComboBox.Bind(ComboBox.SelectedItemProperty, mesh, "RenderGroup");
                    meshRenderGroupComboBox.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) 
                    {
                        RenderTextures();
                    };
                    groupBoxHeader.Children.Add(meshRenderGroupComboBox);

                    TextBlock meshNameTextBlock = new() { Text = mesh.TranslatedName };
                    meshNameTextBlock.Bind(TextBlock.TextProperty, mesh, "TranslatedName");
                    groupBoxHeader.Children.Add(meshNameTextBlock);

                    TextBox meshRenderParameter1 = new() { Text = mesh.Material.RenderParameters[0].ToString(), Margin = new(5, 0, 2, 0), MinWidth = 25 };
                    TextBox meshRenderParameter2 = new() { Text = mesh.Material.RenderParameters[1].ToString(), Margin = new(2, 0, 2, 0), MinWidth = 25 };
                    TextBox meshRenderParameter3 = new() { Text = mesh.Material.RenderParameters[2].ToString(), Margin = new(2, 0, 0, 0), MinWidth = 25 };
                    meshRenderParameter1.Bind(TextBox.TextProperty, mesh, "RenderParameters[0]");
                    meshRenderParameter2.Bind(TextBox.TextProperty, mesh, "RenderParameters[1]");
                    meshRenderParameter3.Bind(TextBox.TextProperty, mesh, "RenderParameters[2]");
                    groupBoxHeader.Children.Add(meshRenderParameter1);
                    groupBoxHeader.Children.Add(meshRenderParameter2);
                    groupBoxHeader.Children.Add(meshRenderParameter3);

                    GroupBox meshNameGroupBox = new() { Header = groupBoxHeader };
                    TexturesPanel.Children.Add(meshNameGroupBox);

                    Grid texturesGrid = new();
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    texturesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    meshNameGroupBox.Content = texturesGrid;

                    for (int i = 0; i < mesh.Material.RenderGroup.SupportedTextureTypes.Count; i++)
                    {
                        if (mesh.Material.Textures.Count <= i)
                        {
                            mesh.Material.Textures.Add(new Texture());
                        }
                        Texture texture = mesh.Material.Textures.ElementAt(i);

                        texturesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

                        TextBlock textureTypeTextBlock = new() { Text = mesh.Material.RenderGroup.SupportedTextureTypes[i].Code(), Margin = new(0, 0, 5, 0) };
                        Grid.SetRow(textureTypeTextBlock, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(textureTypeTextBlock, 0);
                        textureTypeTextBlock.Foreground = Brushes.Gray;
                        texturesGrid.Children.Add(textureTypeTextBlock);

                        TextBlock originalNameTextBlock = new() { Text = texture.OriginalName };
                        Grid.SetRow(originalNameTextBlock, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(originalNameTextBlock, 1);
                        originalNameTextBlock.Bind(TextBlock.TextProperty, texture, "OriginalName");
                        texturesGrid.Children.Add(originalNameTextBlock);

                        TextBlock arrowTextBlock = new() { Text = "➔" };
                        Grid.SetRow(arrowTextBlock, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(arrowTextBlock, 2);
                        arrowTextBlock.Margin = new Thickness(0, 0, 5, 0);
                        texturesGrid.Children.Add(arrowTextBlock);

                        TextBox translationTextBox = new() { Text = texture.TranslatedName };
                        Grid.SetRow(translationTextBox, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(translationTextBox, 3);
                        translationTextBox.Bind(TextBox.TextProperty, texture, "TranslatedName");
                        texturesGrid.Children.Add(translationTextBox);

                        TextBlock translatingTextBlock = new() { Text = texture.TranslatingName };
                        Grid.SetRow(translatingTextBlock, texturesGrid.RowDefinitions.Count - 1);
                        Grid.SetColumn(translatingTextBlock, 4);
                        translatingTextBlock.Foreground = Brushes.Red;
                        translatingTextBlock.Bind(TextBlock.TextProperty, texture, "TranslatingName");
                        texturesGrid.Children.Add(translatingTextBlock);
                    }
                }
            });
            RenderTree();
        }

        private void TreeItem_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is TreeViewItem treeItem)
            {
                treeItem.Background = Brushes.LightBlue;
                e.Handled = true;
            }
        }

        private void TreeItem_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is TreeViewItem treeItem)
            {
                treeItem.Background = null;
                e.Handled = true;
            }
        }

        private void BoneTree_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && boneTree.SelectedItem is TreeViewItem draggingItem)
            {
                DragDrop.DoDragDrop(boneTree.SelectedItem as TreeViewItem, draggingItem, DragDropEffects.Move);
            }
        }

        private void BoneTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (boneTree.SelectedItem is TreeViewItem item && item.Tag is Bone bone)
            {
                selectedBoneXPosition.Bind(TextBox.TextProperty, bone, "Position[0]");
                selectedBoneYPosition.Bind(TextBox.TextProperty, bone, "Position[1]");
                selectedBoneZPosition.Bind(TextBox.TextProperty, bone, "Position[2]");
            }
        }

        private void TreeItem_Drop(object sender, DragEventArgs e)
        {
            TreeViewItem? target = sender as TreeViewItem;
            TreeViewItem? source = e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;
            if (target?.Tag is Bone targetBone && source?.Tag is Bone draggedBone && draggedBone.Parent != null && draggedBone != targetBone && source.Parent is TreeViewItem parent)
            {
                parent.Items.Remove(source);
                draggedBone.Parent = targetBone;
                target.Items.Add(source);
            }
            TreeItem_DragLeave(sender, e);
            e.Handled = true;
        }

        private void AddBone(object sender, RoutedEventArgs e)
        {
            if (boneTree.SelectedItem is TreeViewItem item && item.Tag is Bone bone)
            {
                loader.AddBone(bone);
            }
            else
            {
                loader.AddBone();
            }
            RenderBones();
        }
        private void MakeRoot(object sender, RoutedEventArgs e)
        {
            if (boneTree.SelectedItem is TreeViewItem item && item.Tag is Bone bone)
            {
                loader.MakeRoot(bone);
                RenderBones();
            }
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
            loader.Bones.ForEach(b => b.TranslatingName = null);
            loader.Meshes.ForEach(b => b.TranslatingName = null);
            loader.Meshes.SelectMany(m => m.Material.Textures).ToList().ForEach(b => b.TranslatingName = null);

            if (regexOriginal.Text.Length > 0)
            {
                try
                {
                    Regex r1 = new(regexOriginal.Text);
                    loader.Bones.Where(b => r1.IsMatch(b.TranslatedName)).ToList().ForEach(b =>
                    {
                        try
                        {
                            b.TranslatingName = Regex.Replace(b.TranslatedName, regexOriginal.Text, regexResult.Text);
                        }
                        catch
                        {
                        }
                    });
                    loader.Meshes.Where(b => r1.IsMatch(b.TranslatedName)).ToList().ForEach(b =>
                    {
                        try
                        {
                            b.TranslatingName = Regex.Replace(b.TranslatedName, regexOriginal.Text, regexResult.Text);
                        }
                        catch
                        {
                        }
                    });
                    loader.Meshes.SelectMany(m => m.Material.Textures).Where(b => r1.IsMatch(b.TranslatedName)).ToList().ForEach(b =>
                    {
                        try
                        {
                            b.TranslatingName = Regex.Replace(b.TranslatedName, regexOriginal.Text, regexResult.Text);
                        }
                        catch
                        {
                        }
                    });
                }
                catch { }
            }
        }

        private void ApplyRename(object sender, RoutedEventArgs e)
        {
            switch (selectedTab)
            {
                case "Bones":
                    loader.Bones.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
                    break;
                case "Meshes":
                    loader.Meshes.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName!);
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

        private void LinkMaterials(object sender, RoutedEventArgs e)
        {
            foreach (Mesh m in loader.Meshes)
            {
                var first = loader.Meshes.First(m2 => m2.Material.Textures[0].TranslatedName == m.Material.Textures[0].TranslatedName);
                m.Material.RenderGroup = first.Material.RenderGroup;
                m.Material.RenderParameters = first.Material.RenderParameters;
                m.Material.Textures = first.Material.Textures.Select(t => new Texture() { OriginalName = t.OriginalName, TranslatedName = t.TranslatedName, TranslatingName = t.TranslatingName, UvLayer = t.UvLayer }).ToList();
            }
        }
    }
}
