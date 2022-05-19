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
        private const char separator = ';';

        private string originalFileName = "";
        private List<string> originalLines = new();
        private readonly List<Bone> bones = new();

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
                bool onlyConflictingCondition(Bone bone) => onlyConflicting.IsChecked == false || bones.Count(b => bone.TranslatedName == b.TranslatedName) > 1;

                foreach (var bone in bones.Where(b => containsCondition(b) && onlyUntranslatedCondition(b) && onlyConflictingCondition(b)))
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
                foreach (Bone child in bones.Where(b => b.Parent == bone))
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
                foreach (Bone bone in bones.Where(b => b.Parent == null))
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

        private void TreeItem_Drop(object sender, DragEventArgs e)
        {
            TreeViewItem? target = sender as TreeViewItem;
            TreeViewItem? source = e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;
            if (target?.Tag is Bone targetBone && source?.Tag is Bone draggedBone && draggedBone.Parent != null && draggedBone != targetBone)
            {
                (source.Parent as TreeViewItem).Items.Remove(source);
                draggedBone.Parent = targetBone;
                target.Items.Add(source);
                // RenderTree();
            }
            TreeItem_DragLeave(sender, e);
            e.Handled = true;
        }

        private void LoadFile(string fileName)
        {
            originalFileName = fileName.Replace(".mesh.ascii", "");
            originalLines = File.ReadLines(fileName).ToList();
            int boneCount = int.Parse(originalLines.First().Split('#').First());

            //3 lines per bone
            //bone name
            //parent index
            //position

            bones.Clear();
            Dictionary<Bone, int> parentIndexes = new();
            for (int i = 1; i <= boneCount * 3; i += 3)
            {
                string name = originalLines[i].Clean();
                int parentIndex = int.Parse(originalLines[i + 1]);
                float[] position = originalLines[i + 2].Split(' ').Select(n => float.Parse(n)).ToArray();
                Bone bone = new Bone()
                {
                    OriginalName = name,
                    TranslatedName = name,
                    Position = position,
                    FromMeshAscii = true
                };
                parentIndexes.Add(bone, parentIndex);
                bones.Add(bone);
            }
            foreach (Bone bone in bones)
            {
                int parentIndex = parentIndexes[bone];
                bone.Parent = parentIndex == -1 ? null : bones[parentIndex];
            }

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
            new Thread(() =>
            {
                File.ReadLines(fileName).ToList().ForEach(boneLine =>
                {
                    if (!boneLine.StartsWith("#") && !string.IsNullOrWhiteSpace(boneLine))
                    {
                        string[] parts = boneLine.Split(separator).Select(part => part.Trim()).ToArray();
                        if (parts.Length == 2)
                        {
                            string originalName = parts[0].Clean();
                            string translation = parts[1].Clean();
                            Bone? bone = bones.Find(b => b.OriginalName == originalName);
                            if (bone != null)
                            {
                                bone.TranslatedName = translation;
                            }
                            else if (keepAll)
                            {
                                bones.Add(new Bone()
                                {
                                    OriginalName = originalName,
                                    TranslatedName = translation,
                                    FromMeshAscii = false
                                });
                            }
                        }
                    }
                });

                RenderBones();

                Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
                Progress.Dispatcher.Invoke(() => Progress.Visibility = Visibility.Visible);
            }).Start();
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

            SaveFileDialog sfd = new SaveFileDialog()
            {
                Title = "Select where to save the bone list names",
                AddExtension = true,
                Filter = "BoneDict file|*.txt",
                FileName = "BoneDict"
            };
            if (sfd.ShowDialog() == true)
            {
                Progress.Maximum = bones.Count;
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;
                new Thread(() =>
                {
                    using StreamWriter file = new(sfd.FileName, false);
                    bones.Where(b => b.OriginalName != b.TranslatedName).Select(b => b.OriginalName + separator + b.TranslatedName).ToList().ForEach(line =>
                    {
                        file.WriteLine(line);
                        IncreaseProgress();
                    });
                    Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
                }).Start();
            }
        }

        private bool ConfictExists(out string[] conficting)
        {
            IEnumerable<IGrouping<string, Bone>> groups = bones.Where(b => b.FromMeshAscii).GroupBy(b => b.TranslatedName);
            conficting = groups.Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            return conficting.Length > 0;
        }

        private void SaveMesh(object sender, RoutedEventArgs e)
        {
            if (ConfictExists(out string[] conficting))
            {
                MessageBox.Show('"' + conficting[0] + "\" and \"" + conficting[1] + "\" are conflicting, rename one of them before saving", "Confict found", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }

            SavingMessage.Content = "Saving, please wait.";

            SaveFileDialog sfd = new()
            {
                Title = "Select where to save the resulting mesh",
                AddExtension = true,
                Filter = "XPS .mesh.ascii|*.mesh.ascii",
                FileName = originalFileName + "_renamed"
            };
            if (sfd.ShowDialog() == true)
            {
                Progress.Maximum = originalLines.Count;
                Progress.Value = 0;
                Saving.Visibility = Visibility.Visible;
                new Thread(() =>
                {
                    using StreamWriter file = new(sfd.FileName, false);
                    file.WriteLine(bones.Count + " # bones");
                    bones.ForEach(b =>
                    {
                        file.WriteLine(b.TranslatedName);
                        file.WriteLine(b.Parent == null ? "-1" : bones.IndexOf(b.Parent).ToString());
                        file.WriteLine(string.Join(" ", b.Position));
                        IncreaseProgress();
                    });

                    originalLines.Skip(1 + bones.Count * 3).ToList().ForEach(line =>
                    {
                        file.WriteLine(line);
                        IncreaseProgress();
                    });
                    Saving.Dispatcher.Invoke(() => Saving.Visibility = Visibility.Hidden);
                }).Start();
            }
        }

        private void UnloadAll(object sender, RoutedEventArgs e)
        {
            bones.Clear();
            RenderBones();
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

        private void Regex_TextChanged(object sender, TextChangedEventArgs e)
        {
            bones.ForEach(b => b.TranslatingName = null);

            if (regexOriginal.Text.Length > 0)
            {
                try
                {
                    Regex r1 = new Regex(regexOriginal.Text);
                    bones.Where(b => r1.IsMatch(b.TranslatedName)).ToList().ForEach(b =>
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
            bones.Where(b => b.TranslatingName != null).ToList().ForEach(b => b.TranslatedName = b.TranslatingName);
            Regex_TextChanged(regexResult, null);
        }
    }
}
