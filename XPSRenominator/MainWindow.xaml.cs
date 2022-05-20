using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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
                bool onlyConflictingCondition(Bone bone) => onlyConflicting.IsChecked == false || loader.Bones.Count(b => bone.TranslatedName == b.TranslatedName) > 1;

                foreach (var bone in loader.Bones.Where(b => containsCondition(b) && onlyUntranslatedCondition(b) && onlyConflictingCondition(b)))
                {
                    BonesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

                    TextBlock originalNameTextBlock = new TextBlock { Text = bone.OriginalName };
                    Grid.SetRow(originalNameTextBlock, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(originalNameTextBlock, 0);
                    BonesGrid.Children.Add(originalNameTextBlock);

                    TextBlock arrowTextBlock = new TextBlock { Text = "➔" };
                    Grid.SetRow(arrowTextBlock, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(arrowTextBlock, 1);
                    arrowTextBlock.Margin = new Thickness(0, 0, 5, 0);
                    BonesGrid.Children.Add(arrowTextBlock);

                    TextBox translationTextBox = new TextBox { Text = bone.TranslatedName };
                    Grid.SetRow(translationTextBox, BonesGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(translationTextBox, 2);
                    translationTextBox.Bind(TextBox.TextProperty, bone, "TranslatedName");
                    BonesGrid.Children.Add(translationTextBox);

                    TextBlock translatingTextBlock = new TextBlock { Text = bone.TranslatingName };
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
                foreach (Bone child in loader.Bones.Where(b => b.Parent == bone))
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
                foreach (Bone bone in loader.Bones.Where(b => b.Parent == null))
                {
                    RenderTreeItem(bone);
                }
            });
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

        private void LoadFile(string fileName)
        {
            originalMeshAsciiName = fileName;
            loader.LoadAsciiFile(fileName);

            RenderBones();

            Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
            Progress.Dispatcher.Invoke(() => Progress.Visibility = Visibility.Visible);
        }

        private void LoadMeshFile(object sender, RoutedEventArgs e)
        {
            SavingMessage.Content = "Loading, please wait.";

            OpenFileDialog ofd = new OpenFileDialog()
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

                LoadFile(ofd.FileName);
            }
        }

        private void LoadBones(string fileName, bool keepAll = true)
        {
            originalBonedictName = fileName;

            loader.LoadBoneFile(fileName, keepAll);

            RenderBones();
        }

        private void LoadBonesFile(object sender, RoutedEventArgs e)
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
                Saving.Visibility = Visibility.Visible;
                Progress.Visibility = Visibility.Hidden;

                LoadBones(ofd.FileName);
            }
        }

        private void SaveBones(object sender, RoutedEventArgs e)
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
                Progress.Maximum = loader.Bones.Count;
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;
                new Thread(() => loader.SaveBones(sfd.FileName, IncreaseProgress)).Start();
            }
        }

        private void SaveMesh(object sender, RoutedEventArgs e)
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
                Progress.Maximum = loader.Bones.Count + loader.Meshes.Count;
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;

                new Thread(() =>
                {
                    if (!loader.SaveAscii(sfd.FileName, out string[] conflicting, IncreaseProgress))
                    {
                        MessageBox.Show('"' + conflicting[0] + "\" and \"" + conflicting[1] + "\" are conflicting, rename one of them before saving", "Confict found", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }).Start();
            }
        }

        private void UnloadAll(object sender, RoutedEventArgs e)
        {
            loader.Bones.Clear();
            loader.Meshes.Clear();
            RenderBones();
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

        private void RenominatorUsage(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Renominator is a program made to simplify the creation and application of BoneDict files.\n\n" +
                "First of all load a .mesh.ascii file, then you can proceed in different ways:\n\n" +
                "- Use the \"Save BoneDict\" button to export a BoneDict.txt file and edit it with the text editor of your choice.\n\n" +
                "- Edit the bone names directly using Renominator and export the result as a ready to be used BoneDict file using the \"Save BoneDict\" button.\n\n" +
                "- Edit the bone names directly using Renominator and export a new .mesh.ascii with renamed bones using the \"Save .mesh.ascii\" button.\n\n" +
                "- Import an already existing BoneDict file using the \"Load BoneDict\" button and export a new .mesh.ascii with renamed bones using the \"Save .mesh.ascii\" button.", "How to use Renominator?", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BoneDictExplanation(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("BoneDict are plain text files used to tell XPS how to rename bones.\n" +
                "XPS natively supports BoneDict files, all you have to do is place a file called exactly \"BoneDict.txt\" in the same directory as the XPS .exe.\n" +
                "The placed file will be automatically used the next time you open XPS.", "What is BoneDict?", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            fileNames?.ToList().ForEach(fileName =>
            {
                if (fileName.EndsWith(".ascii")) LoadFile(fileName);
                if (fileName.EndsWith(".txt")) LoadBones(fileName);
            });
        }

        private void Filter_TextChanged(object sender, EventArgs e)
        {
            RenderBones();
        }

        private void Regex_TextChanged(object sender, TextChangedEventArgs? e)
        {
            loader.Bones.ForEach(b => b.TranslatingName = null);

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
                }
                catch { }
            }
        }

        private void ApplyRename(object sender, RoutedEventArgs e)
        {
            #pragma warning disable CS8601 // Possible null reference assignment.
            loader.Bones.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName);
            #pragma warning restore CS8601 // Possible null reference assignment.
            Regex_TextChanged(regexResult, null);
        }

    }
}
