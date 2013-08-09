﻿// Copyright (C) Wiesław Šoltés 2013. 
// All Rights Reserved

#region References

using CanvasDiagramEditor.Core;
using CanvasDiagramEditor.Controls;
using CanvasDiagramEditor.Editor;
using CanvasDiagramEditor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Printing;
using System.Windows.Markup;

#endregion

namespace CanvasDiagramEditor
{
    #region Aliases

    using MapPin = Tuple<string, string>;
    using MapWire = Tuple<object, object, object>;
    using MapWires = Tuple<object, List<Tuple<string, string>>>;
    using Selection = Tuple<bool, List<Tuple<object, object, object>>>;
    using History = Tuple<Stack<string>, Stack<string>>;
    using Diagram = Tuple<string, Tuple<Stack<string>, Stack<string>>>;
    using TreeDiagram = Stack<string>;
    using TreeDiagrams = Stack<Stack<string>>;
    using TreeProject = Tuple<string, Stack<Stack<string>>>;
    using TreeProjects = Stack<Tuple<string, Stack<Stack<string>>>>;
    using TreeSolution = Tuple<string, string, Stack<Tuple<string, Stack<Stack<string>>>>>;

    #endregion

    #region MainWindow

    public partial class MainWindow : Window
    {
        #region Fields

        private DiagramEditor Editor { get; set; }
        private string LogicDictionaryUri = "Views/LogicDictionary.xaml";

        private bool HaveKeyE = false;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            InitializeEditor();

            this.DiagramControl.Editor = this.Editor;
            this.DiagramControl.ZoomSlider = this.ZoomSlider;

            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.DiagramControl.PanScrollViewer.Focus();

            var table = new DiagramTable()
            {
                Id = 0,
                Revision1 = new Revision()
                {
                    Version = "",
                    Date = "",
                    Remarks = "",
                },
                Revision2 = new Revision()
                {
                    Version = "",
                    Date = "",
                    Remarks = "",
                },
                Revision3 = new Revision()
                {
                    Version = "",
                    Date = "",
                    Remarks = "",
                },
                Logo1 = null,
                Logo2 = null,
                Drawn = new Person()
                {
                    Name = "user",
                    Date = DateTime.Today.ToString("yyyy-MM-dd")
                },
                Checked = new Person()
                {
                    Name = "user",
                    Date = DateTime.Today.ToString("yyyy-MM-dd")
                },
                Approved = new Person()
                {
                    Name = "user",
                    Date = DateTime.Today.ToString("yyyy-MM-dd")
                },
                Title = "LOGIC DIAGRAM",
                SubTitle1 = "DIAGRAM TITLE",
                SubTitle2 = "",
                SubTitle3 = "",
                Rev = "0",
                Status = "-",
                Page = "-",
                Pages = "-",
                Project = "sample",
                OrderNo = "",
                DocumentNo = "",
                ArchiveNo = ""
            };

            TableGrid.SetData(this, table);
        }

        private void InitializeEditor()
        {
            var options = new DiagramEditorOptions();

            Editor = new DiagramEditor();
            Editor.CurrentOptions = options;

            Editor.CurrentOptions.Counter.ProjectCount = 1;
            Editor.CurrentOptions.Counter.DiagramCount = 1;

            Editor.UpdateDiagramProperties = () =>
            {
                var prop = options.CurrentProperties;

                prop.PageWidth = int.Parse(TextPageWidth.Text);
                prop.PageHeight = int.Parse(TextPageHeight.Text);

                prop.GridOriginX = int.Parse(TextGridOriginX.Text);
                prop.GridOriginY = int.Parse(TextGridOriginY.Text);
                prop.GridWidth = int.Parse(TextGridWidth.Text);
                prop.GridHeight = int.Parse(TextGridHeight.Text);
                prop.GridSize = int.Parse(TextGridSize.Text);

                prop.SnapX = double.Parse(TextSnapX.Text);
                prop.SnapY = double.Parse(TextSnapY.Text);
                prop.SnapOffsetX = double.Parse(TextSnapOffsetX.Text);
                prop.SnapOffsetY = double.Parse(TextSnapOffsetY.Text);
            };

            Editor.CurrentOptions.CurrentResources = this.Resources;

            Editor.CurrentOptions.CurrentTree = this.SolutionTree;
            Editor.CurrentOptions.CurrentCanvas = this.DiagramControl.DiagramCanvas;
            Editor.CurrentOptions.CurrentPathGrid = this.DiagramControl.PathGrid;

            EnableHistory.IsChecked = options.EnableHistory;
            EnableInsertLast.IsChecked = options.EnableInsertLast;
            EnableSnap.IsChecked = options.EnableSnap;
            SnapOnRelease.IsChecked = options.SnapOnRelease;

            Editor.CreateTreeSolutionItem = () =>
            {
                var solution = new SolutionTreeViewItem();

                solution.Header = ModelConstants.TagHeaderSolution;
                solution.ContextMenu = Editor.CurrentOptions.CurrentResources["SolutionContextMenuKey"] as ContextMenu;
                solution.MouseRightButtonDown += TreeViewItem_MouseRightButtonDown;
                solution.IsExpanded = true;

                return solution as ITreeItem;
            };

            Editor.CreateTreeProjectItem = () =>
            {
                var project = new SolutionTreeViewItem();

                project.Header = ModelConstants.TagHeaderProject;
                project.ContextMenu = Editor.CurrentOptions.CurrentResources["ProjectContextMenuKey"] as ContextMenu;
                project.MouseRightButtonDown += TreeViewItem_MouseRightButtonDown;
                project.IsExpanded = true;

                return project as ITreeItem;
            };

            Editor.CreateTreeDiagramItem = () =>
            {
                var diagram = new SolutionTreeViewItem();

                diagram.Header = ModelConstants.TagHeaderDiagram;
                diagram.ContextMenu = Editor.CurrentOptions.CurrentResources["DiagramContextMenuKey"] as ContextMenu;
                diagram.MouseRightButtonDown += TreeViewItem_MouseRightButtonDown;

                return diagram as ITreeItem;
            };

            Editor.SetThumbEvents = (ithumb) =>
            {
                var thumb = ithumb as ElementThumb;

                thumb.DragDelta += (sender, e) =>
                {
                    var canvas = Editor.CurrentOptions.CurrentCanvas;
                    var element = sender as IThumb;

                    double dX = e.HorizontalChange;
                    double dY = e.VerticalChange;

                    Editor.Drag(canvas, element, dX, dY);
                };

                thumb.DragStarted += (sender, e) =>
                {
                    var canvas = Editor.CurrentOptions.CurrentCanvas;
                    var element = sender as IThumb;

                    Editor.DragStart(canvas, element);
                };

                thumb.DragCompleted += (sender, e) =>
                {
                    var canvas = Editor.CurrentOptions.CurrentCanvas;
                    var element = sender as IThumb;

                    Editor.DragEnd(canvas, element);
                };
            };

            Editor.GridGenerate(false);
        }

        #endregion

        #region CheckBox Events

        private void EnableHistory_Click(object sender, RoutedEventArgs e)
        {
            Editor.CurrentOptions.EnableHistory = EnableHistory.IsChecked == true ? true : false;

            if (Editor.CurrentOptions.EnableHistory == false)
            {
                var canvas = Editor.CurrentOptions.CurrentCanvas;

                Editor.HistoryClear(canvas);
            }
        }

        private void EnableSnap_Click(object sender, RoutedEventArgs e)
        {
            Editor.CurrentOptions.EnableSnap = EnableSnap.IsChecked == true ? true : false;
        }

        private void SnapOnRelease_Click(object sender, RoutedEventArgs e)
        {
            Editor.CurrentOptions.SnapOnRelease = SnapOnRelease.IsChecked == true ? true : false;
        }

        private void EnableInsertLast_Click(object sender, RoutedEventArgs e)
        {
            Editor.CurrentOptions.EnableInsertLast = EnableInsertLast.IsChecked == true ? true : false;
        }

        private void EnablePage_Click(object sender, RoutedEventArgs e)
        {
            var diagram = this.DiagramControl;
            var visibility = diagram.Visibility;
            diagram.Visibility = visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EnablePageGrid_Click(object sender, RoutedEventArgs e)
        {
            var grid = this.DiagramControl.DiagramGrid;
            var visibility = grid.Visibility;
            grid.Visibility = visibility == Visibility.Collapsed ? Visibility.Visible :Visibility.Collapsed;
        }

        private void EnablePageTemplate_Click(object sender, RoutedEventArgs e)
        {
            var template = this.DiagramControl.DiagramTemplate;
            var visibility = template.Visibility;
            template.Visibility = visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Button Events

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            LogBaseSlider.Value = 1.9;
            ExpFactorSlider.Value = 1.3;
            ZoomSlider.Value = 1.0;
        }

        private void GenerateModel_Click(object sender, RoutedEventArgs e)
        {
            Editor.ModelUpdateSelectedDiagram();

            var solution = Editor.ModelGenerateSolution(System.IO.Directory.GetCurrentDirectory(), false);

            this.TextModel.Text = solution.Item1;
        }

        private void GenerateModelFromSelected_Click(object sender, RoutedEventArgs e)
        {
            var diagram = Editor.ModelGenerateFromSelected();

            this.TextModel.Text = diagram;
        }

        private void InsertModel_Click(object sender, RoutedEventArgs e)
        {
            var diagram = this.TextModel.Text;
            double offsetX = double.Parse(TextOffsetX.Text);
            double offsetY = double.Parse(TextOffsetY.Text);

            Editor.ModelInsert(diagram, offsetX, offsetY);
        }

        private void UpdateGrid_Click(object sender, RoutedEventArgs e)
        {
            Editor.GridGenerate(true);
        }

        #endregion

        #region TreeView Events

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as SolutionTreeViewItem;
            if (item != null)
            {
                item.IsSelected = true;
                item.Focus();
                item.BringIntoView();

                e.Handled = true;
            }
        }

        private void SolutionTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Editor == null)
                return;

            var canvas = Editor.CurrentOptions.CurrentCanvas;

            var oldItem = e.OldValue as SolutionTreeViewItem;
            var newItem = e.NewValue as SolutionTreeViewItem;

            bool isDiagram = Editor.TreeSwitchItems(canvas, oldItem, newItem);
            if (isDiagram == true)
            {
                this.DiagramControl.PanScrollViewer.Visibility = Visibility.Visible;
            }
            else
            {
                this.DiagramControl.PanScrollViewer.Visibility = Visibility.Collapsed;
            }
        }

        private void SolutionAddProject_Click(object sender, RoutedEventArgs e)
        {
            var solution = SolutionTree.SelectedItem as SolutionTreeViewItem;

            Editor.TreeAddProject(solution);
        }

        private void ProjectAddDiagram_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeAddNewItem();
        }

        private void DiagramAddDiagram_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeAddNewItem();
        }

        private void SolutionDeleteProject_Click(object sender, RoutedEventArgs e)
        {
            var project = SolutionTree.SelectedItem as SolutionTreeViewItem;

            Editor.TreeDeleteProject(project);
        }

        private void DiagramDeleteDiagram_Click(object sender, RoutedEventArgs e)
        {
            var diagram = SolutionTree.SelectedItem as SolutionTreeViewItem;

            Editor.TreeDeleteDiagram(diagram);
        }

        private void DiagramAddDiagramAndPaste_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeAddNewItemAndPaste();
        }

        #endregion

        #region File Menu Events

        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeCreateNewSolution();
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            Editor.OpenSolution();
        }

        private void FileSave_Click(object sender, RoutedEventArgs e)
        {
            Editor.SaveSolution();
        }

        private void FileOpenDiagram_Click(object sender, RoutedEventArgs e)
        {
            Editor.OpenDiagram();
        }

        private void FileSaveDiagram_Click(object sender, RoutedEventArgs e)
        {
            Editor.SaveDiagram();
        }

        private void FileOpenTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.TagsOpen();
        }

        private void FileSaveTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.TagsSave();
        }

        private void FileImportTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.TagsImport();
        }

        private void FileExportTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.TagsExport();
        }

        private void FileExportToDxf_Click(object sender, RoutedEventArgs e)
        {
            var table = TableGrid.GetData(this) as DiagramTable;

            Editor.DxfExport(ShortenStart.IsChecked.Value, ShortenEnd.IsChecked.Value, table);
        }

        private void FileInspectDxf_Click(object sender, RoutedEventArgs e)
        {
            InspectDxf();
        }

        private void FileImport_Click(object sender, RoutedEventArgs e)
        {
            var diagram = Editor.ModelImport();

            if (diagram != null)
            {
                this.TextModel.Text = diagram;
            }
        }

        private void FilePrint_Click(object sender, RoutedEventArgs e)
        {
            Print();
        }

        private void FilePrintHistory_Click(object sender, RoutedEventArgs e)
        {
            PrintHistory();
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Edit Menu Events

        private void EditUndo_Click(object sender, RoutedEventArgs e)
        {
            Editor.HistoryUndo();
        }

        private void EditRedo_Click(object sender, RoutedEventArgs e)
        {
            Editor.HistoryRedo();
        }

        private void EditCut_Click(object sender, RoutedEventArgs e)
        {
            Editor.EditCut();
        }

        private void EditCopy_Click(object sender, RoutedEventArgs e)
        {
            Editor.EditCopy();
        }

        private void EditPaste_Click(object sender, RoutedEventArgs e)
        {
            var point = new PointEx(0.0, 0.0);

            Editor.EditPaste(point);
        }

        private void EditDelete_Click(object sender, RoutedEventArgs e)
        {
            Editor.EditDelete();
        }

        private void EditSelectAll_Click(object sender, RoutedEventArgs e)
        {
            Editor.SelectAll();
        }

        private void EditDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            Editor.SelectNone();
        }

        private void EditSelectPrevious_Click(object sender, RoutedEventArgs e)
        {
            // use control Key to select many element
            bool deselect = !(Keyboard.Modifiers == ModifierKeys.Control);

            Editor.SelectPrevious(deselect);
        }

        private void EditSelectNext_Click(object sender, RoutedEventArgs e)
        {
            // use control Key to select many element
            bool deselect = !(Keyboard.Modifiers == ModifierKeys.Control);
            Editor.SelectNext(deselect);
        }

        private void EditSelectConnected_Click(object sender, RoutedEventArgs e)
        {
            Editor.SelectConnected();
        }

        private void EditClear_Click(object sender, RoutedEventArgs e)
        {
            Editor.ModelClear();
        }

        private void EditResetThumbTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.ModelResetThumbTags();
        }

        private void EditOptions_Click(object sender, RoutedEventArgs e)
        {
            TabOptions.IsSelected = true;
        }

        #endregion

        #region View Menu Events

        private void ViewProjectDiagrams_Click(object sender, RoutedEventArgs e)
        {
            ShowProjectDiagrams();
        }

        private void ViewSolutionDiagrams_Click(object sender, RoutedEventArgs e)
        {
            ShowSolutionDiagrams();
        }

        private void ViewDiagram_Click(object sender, RoutedEventArgs e)
        {
            ShowDiagram();
        }

        private void ViewDiagramSelectedElements_Click(object sender, RoutedEventArgs e)
        {
            ShowDiagramSelectedElements();
        }

        private void ViewDiagramHistory_Click(object sender, RoutedEventArgs e)
        {
            ShowDiagramHistory();
        }

        private void ViewPreviousDiagramProject_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeSelectPreviousItem(false);
        }

        private void ViewNextDiagramProjcet_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeSelectNextItem(false);
        }

        private void ViewPreviousDiagramSolution_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeSelectPreviousItem(true);
        }

        private void ViewNextDiagramSolution_Click(object sender, RoutedEventArgs e)
        {
            Editor.TreeSelectNextItem(true);
        }

        #endregion

        #region Tools Menu Events

        private void ToolsTagEditor_Click(object sender, RoutedEventArgs e)
        {
            ShowTagEditor();
        }

        private void ToolsTableEditor_Click(object sender, RoutedEventArgs e)
        {
            ShowTableEditor();
        }

        #endregion

        #region Window Key Events

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debug.Print("PreviewKeyDown sender: {0}, source: {1}", sender.GetType(), e.OriginalSource.GetType());

            if (!(e.OriginalSource is TextBox) &&
                Keyboard.Modifiers != ModifierKeys.Shift)
            {
                HandleKey(e);
            }
        }

        private void HandleKey(KeyEventArgs e)
        {
            var canvas = Editor.CurrentOptions.CurrentCanvas;
            bool isControl = HaveKeyE == true ? false : Keyboard.Modifiers == ModifierKeys.Control;
            //bool isControlShift = (Keyboard.Modifiers & ModifierKeys.Shift) > 0 && (Keyboard.Modifiers & ModifierKeys.Control) > 0;

            switch (e.Key)
            {
                // select previous solution tree item
                case Key.OemComma:
                    {
                        Editor.TreeSelectPreviousItem(isControl);
                    }
                    break;

                // select next solution tree item
                case Key.OemPeriod:
                    {
                        Editor.TreeSelectNextItem(isControl);
                    }
                    break;

                // select previous element
                case Key.OemOpenBrackets:
                    {
                        // use control Key to select many element
                        Editor.SelectPrevious(!isControl);
                    }
                    break;

                // select next element
                case Key.OemCloseBrackets:
                    {
                        // use control Key to select many element
                        Editor.SelectNext(!isControl);
                    }
                    break;

                // select connected elements
                case Key.OemPipe:
                    {
                        Editor.SelectConnected();
                    }
                    break;

                // add new project to selected solution
                // add new diagram to selected project
                // add new diagram after selected diagram and select new diagram
                case Key.J:
                    {
                        // insert new item and paste from clipboard
                        if (isControl == true)
                        {
                            Editor.TreeAddNewItemAndPaste();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;
                case Key.M:
                    {
                        // insert new item
                        if (isControl == true)
                        {
                            Editor.TreeAddNewItem();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // open solution
                case Key.O:
                    {
                        if (isControl == true)
                        {
                            Editor.OpenSolution();
                            e.Handled = true;
                            break;
                        }
                        else
                        {
                            // E + O -> insert OrGate
                            if (HaveKeyE == true)
                            {
                                HaveKeyE = false;
                                InsertOrGate(canvas);
                                e.Handled = true;
                                break;
                            }

                            // O -> insert Input
                            else
                            {
                                InsertOutput(canvas);
                                e.Handled = true;
                                break;
                            }
                        }
                    }

                // save solution
                case Key.S:
                    {
                        if (isControl == true)
                        {
                            Editor.SaveSolution();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // new solution
                case Key.N:
                    {
                        if (isControl == true)
                        {
                            Editor.TreeCreateNewSolution();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // open tags
                case Key.T:
                    {
                        if (isControl == true)
                        {
                            Editor.TagsOpen();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // import
                case Key.I:
                    {
                        if (isControl == true)
                        {
                            Editor.ModelImport();
                            e.Handled = true;
                            break;
                        }
                        else if (HaveKeyE == false)
                        {
                            InsertInput(canvas);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // export to dxf
                case Key.E:
                    {
                        if (isControl == true)
                        {
                            var table = TableGrid.GetData(this) as DiagramTable;
                            Editor.DxfExport(ShortenStart.IsChecked.Value, ShortenEnd.IsChecked.Value, table);
                            e.Handled = true;
                            break;
                        }
                        else
                        {
                            // enable element insert shortcuts -> E + some Key
                            HaveKeyE = true;
                            e.Handled = true;
                            break;
                        }
                    }

                // print
                case Key.P:
                    {
                        if (isControl == true)
                        {
                            Print();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // reset tags
                case Key.R:
                    {
                        Editor.ModelResetThumbTags();
                    }
                    break;

                // undo
                case Key.Z:
                    {
                        if (isControl == true)
                        {
                            Editor.HistoryUndo();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // redo
                case Key.Y:
                    {
                        if (isControl == true)
                        {
                            Editor.HistoryRedo();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // cut
                case Key.X:
                    {
                        if (isControl == true)
                        {
                            Editor.EditCut();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // copy
                case Key.C:
                    {
                        if (isControl == true)
                        {
                            Editor.EditCopy();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // paste
                case Key.V:
                    {
                        // paste from clipboard
                        if (isControl == true)
                        {
                            var point = new PointEx(0.0, 0.0);
                            Editor.EditPaste(point);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // select all
                case Key.A:
                    {
                        if (isControl == true)
                        {
                            Editor.SelectAll();
                            e.Handled = true;
                            break;
                        }

                        // E + A -> insert AndGate
                        if (HaveKeyE == true)
                        {
                            HaveKeyE = false;
                            InsertAndGate(canvas);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // delete
                case Key.Delete:
                    {
                        Editor.EditDelete();
                        e.Handled = true;
                    }
                    break;

                // move up
                case Key.Up:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            Editor.MoveUp(canvas);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // move down
                case Key.Down:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            Editor.MoveDown(canvas);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // move left
                case Key.Left:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            Editor.MoveLeft(canvas);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // move right
                case Key.Right:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            Editor.MoveRight(canvas);
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // tag editor
                case Key.F5:
                    {
                        ShowTagEditor();
                        e.Handled = true;
                    }
                    break;

                // table editor
                case Key.F6:
                    {
                        ShowTableEditor();
                        e.Handled = true;
                    }
                    break;

                // show diagram history
                case Key.H:
                    {
                        if (isControl == true)
                        {
                            ShowDiagramHistory();
                            e.Handled = true;
                        }
                    }
                    break;

                // show project diagrams
                case Key.F7:
                    {
                        ShowProjectDiagrams();
                        e.Handled = true;
                    }
                    break;

                // show solution diagrams
                case Key.F8:
                    {
                        ShowSolutionDiagrams();
                        e.Handled = true;
                    }
                    break;

                // deselect all & reset have E key flah
                case Key.Escape:
                    {
                        Editor.SelectNone();
                        HaveKeyE = false;
                    }
                    break;
            }
        }

        #endregion

        #region Insert

        private PointEx InsertPointInput = new PointEx(45.0, 30.0);
        private PointEx InsertPointOutput = new PointEx(930.0, 30.0);
        private PointEx InsertPointGate = new PointEx(325.0, 30.0);

        private void InsertInput(ICanvas canvas)
        {
            Editor.HistoryAdd(canvas, true);
            
            var element = Editor.InsertInput(canvas, InsertPointInput);
            Editor.SelectOneElement(element, true);
        }

        private void InsertOutput(ICanvas canvas)
        {
            Editor.HistoryAdd(canvas, true);

            var element = Editor.InsertOutput(canvas, InsertPointOutput);
            Editor.SelectOneElement(element, true);
        }

        private void InsertOrGate(ICanvas canvas)
        {
            Editor.HistoryAdd(canvas, true);

            var element = Editor.InsertOrGate(canvas, InsertPointGate);
            Editor.SelectOneElement(element, true);
        }

        private void InsertAndGate(ICanvas canvas)
        {
            Editor.HistoryAdd(canvas, true);

            var element = Editor.InsertAndGate(canvas, InsertPointGate);
            Editor.SelectOneElement(element, true);
        }

        #endregion

        #region Tag Editor

        private string DesignationFilter = "";
        private string SignalFilter = "";
        private string ConditionFilter = "";
        private string DescriptionFilter = "";

        private void ShowTagEditor()
        {
            var window = new TagEditorWindow();
            var control = window.TagEditorControl;

            if (Editor.CurrentOptions.Tags == null)
            {
                Editor.CurrentOptions.Tags = new List<object>();
            }

            var selected = GetSelectedInputOutputElements();

            if (selected.Count == 0)
            {
                var all = GetAllInputOutputElements();

                control.Selected = all;
            }
            else
            {
                control.Selected = selected;
            }

            control.Tags = Editor.CurrentOptions.Tags;

            window.Closing += (sender, e) =>
            {
                // save filters
                DesignationFilter = control.FilterByDesignation.Text;
                SignalFilter = control.FilterBySignal.Text;
                ConditionFilter = control.FilterByCondition.Text;
                DescriptionFilter = control.FilterByDescription.Text;
            };

            // load filters
            control.FilterByDesignation.Text = DesignationFilter;
            control.FilterBySignal.Text = SignalFilter;
            control.FilterByCondition.Text = ConditionFilter;
            control.FilterByDescription.Text = DescriptionFilter;

            // display tag editor window
            window.ShowDialog();
        }

        private List<IElement> GetAllInputOutputElements()
        {
            var all = Editor.GetElementsAll().Where(x =>
            {
                string uid = x.GetUid();
                return StringUtil.StartsWith(uid, ModelConstants.TagElementInput) ||
                    StringUtil.StartsWith(uid, ModelConstants.TagElementOutput);
            }).ToList();

            return all;
        }

        private List<IElement> GetSelectedInputOutputElements()
        {
            var selected = Editor.GetElementsSelected().Where(x =>
            {
                string uid = x.GetUid();
                return StringUtil.StartsWith(uid, ModelConstants.TagElementInput) ||
                    StringUtil.StartsWith(uid, ModelConstants.TagElementOutput);
            }).ToList();

            return selected;
        }

        #endregion

        #region Table Editor

        public void ShowTableEditor()
        {
            // SetLogo(1);
            // SetLogo(2);

            var window = new TableEditorWindow();

            // display table editor window
            window.ShowDialog();
        }

        public void SetLogo(int logoId)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Supported Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.bmp|" + 
                        "Png (*.png)|*.png|" + 
                        "Jpg (*.jpg;*.jpeg)|*.jpg;*.jpeg|" + 
                        "Tif (*.tif;*.tiff)|*.tif;*.tiff|" +
                        "Bmp (*.bmp)|*.bmp|" + 
                        "All Files (*.*)|*.*",
                Title = "Open Logo Image (115x80 @ 96dpi)"
            };

            var res = dlg.ShowDialog();
            if (res == true)
            {
                try
                {
                    var fileName = dlg.FileName;

                    var table = TableGrid.GetData(this) as DiagramTable;
                    if (table != null)
                    {
                        BitmapImage src = new BitmapImage();
                        src.BeginInit();
                        src.UriSource = new Uri(fileName, UriKind.RelativeOrAbsolute);
                        src.CacheOption = BitmapCacheOption.OnLoad;
                        src.EndInit();

                        if (logoId == 1)
                        {
                            table.Logo1 = src;
                            TableGrid.SetData(this, null);
                            TableGrid.SetData(this, table);
                        }
                        else if (logoId == 2)
                        {
                            table.Logo2 = src;
                            TableGrid.SetData(this, null);
                            TableGrid.SetData(this, table);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        #endregion

        #region Zoom Events

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded == false)
                return;

            Editor.CurrentOptions.ZoomLogBase = LogBaseSlider.Value;
            Editor.CurrentOptions.ZoomExpFactor = ExpFactorSlider.Value;

            double zoom = ZoomSlider.Value;

            zoom = Math.Round(zoom, 1);

            if (e.OldValue != e.NewValue)
            {
                this.DiagramControl.Zoom(zoom);
            }
        }

        #endregion

        #region Fixed Document

        private const double PageWidth = 1260;
        private const double PageHeight = 891;

        private void SetPrintStrokeSthickness(ResourceDictionary resources)
        {
            if (resources == null)
            {
                return;
            }

            resources[ResourceConstants.KeyLogicStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.LogicThicknessMm);
            resources[ResourceConstants.KeyWireStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.WireThicknessMm);
            resources[ResourceConstants.KeyElementStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.ElementThicknessMm);
            resources[ResourceConstants.KeyIOStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.IOThicknessMm);
            resources[ResourceConstants.KeyPageStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.PageThicknessMm);
        }

        private void SetPrintColors(ResourceDictionary resources)
        {
            if (resources == null)
            {
                return;
            }

            var backgroundColor = resources["LogicBackgroundColorKey"] as SolidColorBrush;
            backgroundColor.Color = Colors.White;

            var gridColor = resources["LogicGridColorKey"] as SolidColorBrush;
            gridColor.Color = Colors.Transparent;

            var pageColor = resources["LogicTemplateColorKey"] as SolidColorBrush;
            pageColor.Color = Colors.Black;

            var logicColor = resources["LogicColorKey"] as SolidColorBrush;
            logicColor.Color = Colors.Black;

            var logicSelectedColor = resources["LogicSelectedColorKey"] as SolidColorBrush;
            logicSelectedColor.Color = Colors.Black;

            var helperColor = resources["LogicTransparentColorKey"] as SolidColorBrush;
            helperColor.Color = Colors.Transparent;
        }

        private void SetElementResources(ResourceDictionary resources, bool fixedStrokeThickness)
        {
            // set print dictionary
            resources.Source = new Uri(LogicDictionaryUri, UriKind.Relative);

            if (fixedStrokeThickness == false)
            {
                SetPrintStrokeSthickness(resources);
            }

            // set print colors
            SetPrintColors(resources);
        }

        public FrameworkElement CreateDiagramElement(string diagram, 
            Size areaExtent, 
            Point origin, 
            Rect area,
            bool fixedStrokeThickness,
            ResourceDictionary resources)
        {
            var grid = new Grid()
            {
                ClipToBounds = true,
                Resources = resources
            };

            //SetElementResources(grid.Resources, fixedStrokeThickness);

            // set element template and content
            var template = new Control()
            {
                Template = grid.Resources["LandscapePageTemplateKey"] as ControlTemplate
            };

            var canvas = new DiagramCanvas()
            {
                Width = Editor.CurrentOptions.CurrentCanvas.Width,
                Height = Editor.CurrentOptions.CurrentCanvas.Height
            };

            Editor.ModelParseDiagram(diagram, canvas, null, 0, 0, false, false, false, true);

            grid.Children.Add(template);
            grid.Children.Add(canvas);

            // set diagram table
            var table = TableGrid.GetData(this);
            TableGrid.SetData(grid, table);

            // set shorten flags
            LineEx.SetShortenStart(grid, ShortenStart.IsChecked.Value);
            LineEx.SetShortenEnd(grid, ShortenEnd.IsChecked.Value);

            return grid;
        }

        public FixedDocument CreateFixedDocument(IEnumerable<string> diagrams, 
            Size areaExtent, 
            Size areaOrigin, 
            bool fixedStrokeThickness)
        {
            var origin = new Point(areaOrigin.Width, areaOrigin.Height);
            var area = new Rect(origin, areaExtent);
            var scale = Math.Min(areaExtent.Width / PageWidth, areaExtent.Height / PageHeight);

            // create fixed document
            var fixedDocument = new FixedDocument() { Name = "diagrams" };

            SetElementResources(fixedDocument.Resources, fixedStrokeThickness);

            //fixedDocument.DocumentPaginator.PageSize = new Size(areaExtent.Width, areaExtent.Height);

            foreach (var diagram in diagrams)
            {
                var pageContent = new PageContent();
                var fixedPage = new FixedPage();

                //pageContent.Child = fixedPage;
                ((IAddChild)pageContent).AddChild(fixedPage);

                fixedDocument.Pages.Add(pageContent);

                fixedPage.Width = areaExtent.Width;
                fixedPage.Height = areaExtent.Height;

                var element = CreateDiagramElement(diagram, 
                    areaExtent, 
                    origin, 
                    area, 
                    fixedStrokeThickness,
                    fixedDocument.Resources);

                // transform and scale for print
                element.LayoutTransform = new ScaleTransform(scale, scale);

                // set element position
                FixedPage.SetLeft(element, areaOrigin.Width);
                FixedPage.SetTop(element, areaOrigin.Height);

                // add element to page
                fixedPage.Children.Add(element);

                // update fixed page layout
                //fixedPage.Measure(areaExtent);
                //fixedPage.Arrange(area);
            }

            return fixedDocument;
        }

        public FixedDocumentSequence CreateFixedDocumentSequence(IEnumerable<IEnumerable<string>> projects, 
            Size areaExtent, 
            Size areaOrigin,
            bool fixedStrokeThickness)
        {
            var fixedDocumentSeq = new FixedDocumentSequence() { Name = "diagrams" };

            foreach (var diagrams in projects)
            {
                var fixedDocument = CreateFixedDocument(diagrams, 
                    areaExtent, 
                    areaOrigin, 
                    fixedStrokeThickness);

                var documentRef = new DocumentReference();
                documentRef.BeginInit();
                documentRef.SetDocument(fixedDocument);
                documentRef.EndInit();

                (fixedDocumentSeq as IAddChild).AddChild(documentRef);
            }

            return fixedDocumentSeq;
        }

        #endregion

        #region Print

        private void SetPrintDialogOptions(PrintDialog dlg)
        {
            if (dlg == null)
                throw new ArgumentNullException();

            dlg.PrintQueue = LocalPrintServer.GetDefaultPrintQueue();

            dlg.PrintTicket = dlg.PrintQueue.DefaultPrintTicket;
            dlg.PrintTicket.PageOrientation = PageOrientation.Landscape;
            dlg.PrintTicket.OutputQuality = OutputQuality.High;
            dlg.PrintTicket.TrueTypeFontMode = TrueTypeFontMode.DownloadAsNativeTrueTypeFont;
        }

        private bool ShowPrintDialog(PrintDialog dlg)
        {
            if (dlg == null)
                throw new ArgumentNullException();

            // configure printer
            SetPrintDialogOptions(dlg);

            // show print dialog
            if (dlg.ShowDialog() == true)
                return true;
            else
                return false;
        }

        public void Print(IEnumerable<string> diagrams, string name)
        {
            var dlg = new PrintDialog();

            ShowPrintDialog(dlg);

            // print capabilities
            var caps = dlg.PrintQueue.GetPrintCapabilities(dlg.PrintTicket);
            var areaExtent = new Size(caps.PageImageableArea.ExtentWidth, caps.PageImageableArea.ExtentHeight);
            var areaOrigin = new Size(caps.PageImageableArea.OriginWidth, caps.PageImageableArea.OriginHeight);

            // create document
            var s = System.Diagnostics.Stopwatch.StartNew();

            var fixedDocument = CreateFixedDocument(diagrams, 
                areaExtent, 
                areaOrigin,
                false);

            s.Stop();
            System.Diagnostics.Debug.Print("CreateFixedDocument in {0}ms", s.Elapsed.TotalMilliseconds);

            // print document
            dlg.PrintDocument(fixedDocument.DocumentPaginator, name);
        }

        public void PrintSequence(IEnumerable<IEnumerable<string>> projects, string name)
        {
            if (projects == null)
                throw new ArgumentNullException();

            var dlg = new PrintDialog();

            ShowPrintDialog(dlg);

            // print capabilities
            var caps = dlg.PrintQueue.GetPrintCapabilities(dlg.PrintTicket);
            var areaExtent = new Size(caps.PageImageableArea.ExtentWidth, caps.PageImageableArea.ExtentHeight);
            var areaOrigin = new Size(caps.PageImageableArea.OriginWidth, caps.PageImageableArea.OriginHeight);

            // create document
            var s = System.Diagnostics.Stopwatch.StartNew();

            var fixedDocumentSeq = CreateFixedDocumentSequence(projects, 
                areaExtent, 
                areaOrigin,
                false);

            s.Stop();
            System.Diagnostics.Debug.Print("CreateFixedDocumentSequence in {0}ms", s.Elapsed.TotalMilliseconds);

            // print document
            dlg.PrintDocument(fixedDocumentSeq.DocumentPaginator, name);
        }

        public void Print()
        {
            Editor.ModelUpdateSelectedDiagram();

            var diagrams = Editor.ModelGenerateSolution(null, false).Item2;

            Print(diagrams, "solution");
        }

        public void PrintHistory()
        {
            Editor.ModelUpdateSelectedDiagram();

            var diagrams = Editor.ModelGenerateSolution(null, true).Item2;

            Print(diagrams, "history");
        }

        #endregion

        #region Show

        public void ShowDiagram()
        {
            var model = Editor.ModelUpdateSelectedDiagram();

            var diagrams = new List<string>();
            diagrams.Add(model);

            ShowDiagramsWindow(diagrams, "Diagram");
        }

        public void ShowDiagramSelectedElements()
        {
            var model = Editor.ModelGenerateFromSelected();

            var diagrams = new List<string>();
            diagrams.Add(model);

            ShowDiagramsWindow(diagrams, "Diagram (Selected Elements)");
        }

        public void ShowProjectDiagrams()
        {
            Editor.ModelUpdateSelectedDiagram();

            var diagrams = Editor.ModelGetCurrentProjectDiagrams();

            if (diagrams != null)
            {
                ShowDiagramsWindow(diagrams, "Project Diagrams");
            }
        }

        public void ShowSolutionDiagrams()
        {
            Editor.ModelUpdateSelectedDiagram();

            var diagrams = Editor.ModelGenerateSolution(null, false).Item2;

            ShowDiagramsWindow(diagrams, "Solution Diagrams");
        }

        public void ShowDiagramHistory()
        {
            Editor.ModelUpdateSelectedDiagram();

            var diagrams = Editor.ModelGenerateSolution(null, false).Item2;

            ShowDiagramsWindow(diagrams, "Diagram History");
        }

        public void ShowDiagramsWindow(IEnumerable<string> diagrams, string title)
        {
            var areaExtent = new Size(PageWidth, PageHeight);
            var areaOrigin = new Size(0, 0);

            var fixedDocument = CreateFixedDocument(diagrams,
                areaExtent,
                areaOrigin,
                true);

            var window = new Window()
            {
                Title = title,
                Width = PageWidth + 80,
                Height = PageHeight + 120,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Maximized
            };

            var viewer = new DocumentViewer();

            viewer.Document = fixedDocument;

            window.Content = viewer;

            window.Show();
        }

        #endregion

        #region Dxf Inspect

        private void ShowHtmlWindow(string html, string title)
        {
            var window = new HtmlWindow();

            window.Title = title;

            window.Html.NavigateToString(html);

            window.Show();
        }

        public void InspectDxf()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Dxf (*.dxf)|*.dxf|All Files (*.*)|*.*",
                Title = "Inspect Dxf"
            };

            var res = dlg.ShowDialog();
            if (res == true)
            {
                try
                {
                    var html = ParseDxf(dlg.FileName);

                    ShowHtmlWindow(html, "Dxf Inspect");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        private const string CodeEntityType = "0";
        private const string CodeName = "2";

        private const string HeaderSection = "SECTION";

        public class DxfTag
        {
            public string Code { get; set; }
            public string Data { get; set; }
        }

        public string ParseDxf(string fileName)
        {
            var sb = new StringBuilder();
            var tags = new List<DxfTag>();
            DxfTag tag = null;
            bool previousName = false;
            //bool haveCodeEntityType = false;
            bool haveSection = false;

            sb.AppendLine("<html><head>");

            sb.AppendFormat("<title>{0}</title>{1}", System.IO.Path.GetFileName(fileName), Environment.NewLine);
            sb.AppendFormat("<meta charset=\"utf-8\"/>");

            sb.AppendLine("<style>");
            sb.AppendLine("body { background-color:rgb(221,221,221); }");
            sb.AppendLine("dl,dt,dd { font-family: Arial; font-size:10pt; width:100%; }");
            sb.AppendLine("dl { font-weight:normal; margin:0.0cm 0.0cm 0.0cm 0.0cm; background-color:rgb(221,221,221); }");
            
            sb.AppendLine("dt { font-weight:bold; }");
            sb.AppendLine("dt.header { margin:0.0cm 0.0cm 0.0cm 0.0cm; background-color:rgb(255,30,102); }");
            sb.AppendLine("dt.section { margin:0.0cm 0.0cm 0.0cm 0.0cm; background-color:rgb(255,242,102); }");
            sb.AppendLine("dt.other { margin:0.0cm 0.0cm 0.0cm 0.0cm; background-color:rgb(191,191,191); }");
            
            sb.AppendLine("dd { font-weight:normal; margin:0.0cm 0.0cm 0.0cm 0.0cm; background-color:rgb(221,221,221); }");

            sb.AppendLine("code.lineHeader { width:2.0cm; text-align:Left; color:rgb(0,0,0); }");
            sb.AppendLine("code.codeHeader { width:1.2cm; text-align:right; color:rgb(0,0,0); }");
            sb.AppendLine("code.dataHeader { margin:0.0cm 0.0cm 0.0cm 0.5cm; text-align:left; color:rgb(0,0,0); }");

            sb.AppendLine("code.line { width:2.0cm; text-align:Left; color:rgb(84,84,84); }");
            sb.AppendLine("code.code { width:1.2cm; text-align:right; color:rgb(116,116,116); }");
            sb.AppendLine("code.data { margin:0.0cm 0.0cm 0.0cm 0.5cm; text-align:left; color:rgb(0,0,0); }");


            sb.AppendLine("div.container { clear:both; width:auto; display:inline-block; zoom: 1;*display: inline; height:0.0cm; vertical-align:top; overflow:auto; }");
            sb.AppendLine("div.header { margin:0.2cm; }");
            sb.AppendLine("div.content { margin:0.2cm; width:10.0cm; float:left; }");
            sb.AppendLine("div.footer { margin:0.2cm;clear:both;text-align:center; }");

            sb.AppendLine("h1.header { margin-bottom:0; }");

            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");

            sb.AppendLine("<div class=\"container\">");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendFormat("<h1 class=\"header\">{0}</h1></div>{1}", 
                System.IO.Path.GetFileName(fileName), 
                Environment.NewLine);

            sb.AppendLine("<dl>");

            using (var reader = new System.IO.StreamReader(fileName))
            {
                string data = reader.ReadToEnd();

                //var lines = data.Split(Environment.NewLine.ToCharArray(),
                //    StringSplitOptions.RemoveEmptyEntries);

                var lines = data.Split("\n".ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries);

                string[] entity = new string[2] { null, null };
                int lineNumber = 0;

                // header
                //sb.AppendFormat("<dt class=\"header\"><code class=\"lineHeader\">LINE</code><code class=\"codeHeader\">CODE</code><code class=\"dataHeader\">DATA</code></dt>{0}",
                //   Environment.NewLine);

                foreach (var line in lines)
                {
                    var str = line.Trim();

                    // CodeEntityType data
                    if (tag != null)
                    {
                        tag.Data = str;
                        tags.Add(tag);

                        bool isHeaderSection = str == HeaderSection;
                        string entityClass = isHeaderSection == true ? "section" : "other";

                        if (haveSection == true && isHeaderSection == true)
                        {
                            sb.AppendFormat("</div>");
                            haveSection = false;
                        }

                        if (haveSection == false &&  isHeaderSection == true)
                        {
                            haveSection = true;
                            sb.AppendFormat("<div class=\"content\">");
                            sb.AppendFormat("<dt class=\"header\"><code class=\"lineHeader\">LINE</code><code class=\"codeHeader\">CODE</code><code class=\"dataHeader\">DATA</code></dt>{0}", Environment.NewLine);
                        }

                        sb.AppendFormat("<dt class=\"{3}\"><code class=\"line\">{4}</code><code class=\"code\">{0}:</code><code class=\"data\">{1}</code></dt>{2}",
                            tag.Code,
                            tag.Data,
                            Environment.NewLine,
                            entityClass,
                            lineNumber);

                        //haveCodeEntityType = true;

                        tag = null;
                    }
                    else
                    {
                        if (str == CodeEntityType && entity[0] == null)
                        {
                            tag = new DxfTag();
                            tag.Code = str;
                        }
                        else
                        {
                            if (entity[0] == null)
                            {
                                entity[0] = str;
                                entity[1] = null;
                            }
                            else
                            {
                                entity[1] = str;

                                sb.AppendFormat("<dd><code class=\"line\">{3}</code><code class=\"code\">{0}:</code><code class=\"data\">{1}</code></dd>{2}",
                                    entity[0],
                                    entity[1],
                                    Environment.NewLine,lineNumber);

                                // entity Name
                                previousName = entity[0] == CodeName;

                                entity[0] = null;
                                entity[1] = null;

                                //haveCodeEntityType = false;
                            }
                        }
                    }

                    lineNumber++;
                }
            }

            if (haveSection == true)
            {
                sb.AppendFormat("</div>");
            }

            sb.AppendLine(@"</dl>");

            sb.AppendLine("<div class=\"footer\">Copyright (C) Wiesław Šoltés 2013. All Rights Reserved</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body></html>");

            return sb.ToString();
        } 

        #endregion
    }

    #endregion
}
