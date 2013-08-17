﻿// Copyright (C) Wiesław Šoltés 2013. 
// All Rights Reserved

#region References

using CanvasDiagramEditor.Core;
using CanvasDiagramEditor.Controls;
using CanvasDiagramEditor.Editor;
using CanvasDiagramEditor.Util;
using CanvasDiagramEditor.Dxf.Core;
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
    #region MainWindow

    public partial class MainWindow : Window
    {
        #region Fields

        private DiagramEditor Editor { get; set; }

        private string LogicDictionaryUri = "Views/LogicDictionary.xaml";

        private PointEx InsertPointInput = new PointEx(30, 30.0);
        private PointEx InsertPointOutput = new PointEx(930.0, 30.0);
        private PointEx InsertPointGate = new PointEx(325.0, 30.0);

        private const double PageWidth = 1260;
        private const double PageHeight = 891;

        private LineGuidesAdorner GuidesAdorner = null;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            InitializeEditor();

            this.DiagramControl.Editor = this.Editor;
            this.DiagramControl.ZoomSlider = this.ZoomSlider;

            this.Loaded += MainWindow_Loaded;
            this.MouseMove += MainWindow_MouseMove;
        }

        void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (GuidesAdorner != null)
            {
                var canvas = this.DiagramControl.DiagramCanvas;
                var point = e.GetPosition(canvas);

                double x = Editor.SnapOffsetX(point.X, true);
                double y = Editor.SnapOffsetY(point.Y, true);

                GuidesAdorner.X = x;
                GuidesAdorner.Y = y;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.DiagramControl.PanScrollViewer.Focus();

            SetCurrentTable();

            InitializeTagEditor();
        }

        private void SetCurrentTable()
        {
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
            Editor = new DiagramEditor();
            Editor.Context = new Context();

            Editor.Context.CurrentTree = this.SolutionTree;
            Editor.Context.CurrentCanvas = this.DiagramControl.DiagramCanvas;

            var counter = new IdCounter();
            counter.ProjectCount = 1;
            counter.DiagramCount = 1;
            this.DiagramControl.DiagramCanvas.SetCounter(counter);

            var properties = new DiagramProperties();
            this.DiagramControl.DiagramCanvas.SetProperties(properties);

            Editor.Context.IsControlPressed = () => Keyboard.Modifiers == ModifierKeys.Control;
            Editor.Context.UpdateProperties = () => UpdateProperties(Editor.Context.CurrentCanvas.GetProperties());

            Editor.Context.Clipboard = new WindowsClipboard();

            // diagram creator
            var creator = GetDiagramCreator();

            Editor.Context.DiagramCreator = creator;

            // set checkbox states
            EnableHistory.IsChecked = Editor.Context.EnableHistory;
            EnableInsertLast.IsChecked = Editor.Context.EnableInsertLast;
            EnableSnap.IsChecked = Editor.Context.EnableSnap;
            SnapOnRelease.IsChecked = Editor.Context.SnapOnRelease;

            // tree actions
            Editor.Context.CreateTreeSolutionItem = () => CreateTreeSolutionItem();
            Editor.Context.CreateTreeProjectItem = () => CreateTreeProjectItem();
            Editor.Context.CreateTreeDiagramItem = () => CreateTreeDiagramItem();

            // update canvas grid
            Editor.Context.UpdateProperties();
            Model.SetGrid(Editor.Context.CurrentCanvas, 
                Editor.Context.DiagramCreator,
                false);
        }

        private IDiagramCreator GetDiagramCreator()
        {
            var creator = new WpfDiagramCreator();

            creator.SetThumbEvents = (thumb) => SetThumbEvents(thumb);
            creator.SetPosition = (element, left, top, snap) => Editor.SetPosition(element, left, top, snap);
            
            creator.GetTags = () => Editor.Context.Tags;
            creator.GetCounter = () => Editor.Context.CurrentCanvas.GetCounter();

            creator.SetCanvas(this.DiagramControl.DiagramCanvas);
            creator.ParserPath = this.DiagramControl.PathGrid;

            return creator;
        }

        private void SetThumbEvents(ElementThumb thumb)
        {
            thumb.DragDelta += (sender, e) =>
            {
                var canvas = Editor.Context.CurrentCanvas;
                var element = sender as IThumb;

                double dX = e.HorizontalChange;
                double dY = e.VerticalChange;

                Editor.Drag(canvas, element, dX, dY);
            };

            thumb.DragStarted += (sender, e) =>
            {
                var canvas = Editor.Context.CurrentCanvas;
                var element = sender as IThumb;

                Editor.DragStart(canvas, element);
            };

            thumb.DragCompleted += (sender, e) =>
            {
                var canvas = Editor.Context.CurrentCanvas;
                var element = sender as IThumb;

                Editor.DragEnd(canvas, element);
            };
        }

        private void UpdateProperties(DiagramProperties prop)
        {
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
        }

        private ITreeItem CreateTreeDiagramItem()
        {
            var diagram = new SolutionTreeViewItem();

            diagram.Header = ModelConstants.TagHeaderDiagram;
            diagram.ContextMenu = this.Resources["DiagramContextMenuKey"] as ContextMenu;
            diagram.MouseRightButtonDown += TreeViewItem_MouseRightButtonDown;

            return diagram as ITreeItem;
        }

        private ITreeItem CreateTreeProjectItem()
        {
            var project = new SolutionTreeViewItem();

            project.Header = ModelConstants.TagHeaderProject;
            project.ContextMenu = this.Resources["ProjectContextMenuKey"] as ContextMenu;
            project.MouseRightButtonDown += TreeViewItem_MouseRightButtonDown;
            project.IsExpanded = true;

            return project as ITreeItem;
        }

        private ITreeItem CreateTreeSolutionItem()
        {
            var solution = new SolutionTreeViewItem();

            solution.Header = ModelConstants.TagHeaderSolution;
            solution.ContextMenu = this.Resources["SolutionContextMenuKey"] as ContextMenu;
            solution.MouseRightButtonDown += TreeViewItem_MouseRightButtonDown;
            solution.IsExpanded = true;

            return solution as ITreeItem;
        }

        #endregion

        #region CheckBox Events

        private void EnableHistory_Click(object sender, RoutedEventArgs e)
        {
            Editor.Context.EnableHistory = EnableHistory.IsChecked == true ? true : false;

            if (Editor.Context.EnableHistory == false)
            {
                var canvas = Editor.Context.CurrentCanvas;

                History.Clear(canvas);
            }
        }

        private void EnableSnap_Click(object sender, RoutedEventArgs e)
        {
            Editor.Context.EnableSnap = EnableSnap.IsChecked == true ? true : false;
        }

        private void SnapOnRelease_Click(object sender, RoutedEventArgs e)
        {
            Editor.Context.SnapOnRelease = SnapOnRelease.IsChecked == true ? true : false;
        }

        private void EnableInsertLast_Click(object sender, RoutedEventArgs e)
        {
            Editor.Context.EnableInsertLast = EnableInsertLast.IsChecked == true ? true : false;
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
            var diagram = Editor.ModelGenerateFromSelected(Editor.Context.CurrentCanvas);

            this.TextModel.Text = diagram;
        }

        private void InsertModel_Click(object sender, RoutedEventArgs e)
        {
            var diagram = this.TextModel.Text;
            double offsetX = double.Parse(TextOffsetX.Text);
            double offsetY = double.Parse(TextOffsetY.Text);

            Editor.ModelInsert(diagram, offsetX, offsetY, true);
        }

        private void UpdateGrid_Click(object sender, RoutedEventArgs e)
        {
            Editor.Context.UpdateProperties();

            Model.SetGrid(Editor.Context.CurrentCanvas,
                Editor.Context.DiagramCreator,
                true);
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

            var canvas = Editor.Context.CurrentCanvas;
            var creator = Editor.Context.DiagramCreator;

            var oldItem = e.OldValue as SolutionTreeViewItem;
            var newItem = e.NewValue as SolutionTreeViewItem;

            bool isDiagram = Editor.TreeSwitchItems(canvas, creator, oldItem, newItem);
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
            InitializeTagEditor();
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            Editor.OpenSolution();
            InitializeTagEditor();
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
            InitializeTagEditor();
        }

        private void FileSaveTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.TagsSave();
        }

        private void FileImportTags_Click(object sender, RoutedEventArgs e)
        {
            Editor.TagsImport();
            InitializeTagEditor();
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
            var dxf = new DxfInspect();
            dxf.Inspect();
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
            Editor.EditPaste(new PointEx(0.0, 0.0), true);
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

        private void EditConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
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

        private void ViewToggleGuides_Click(object sender, RoutedEventArgs e)
        {
            ToggleGuides();
        }

        #endregion

        #region Help Menu Events

        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Canvas Diagram Editor\n\n" +
                "Copyright (C) Wiesław Šoltés 2013.\n" +
                "All Rights Reserved",
                "About Canvas Diagram Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
            var canvas = Editor.Context.CurrentCanvas;
            bool isControl = Keyboard.Modifiers == ModifierKeys.Control;
            //bool isControlShift = (Keyboard.Modifiers & ModifierKeys.Shift) > 0 && (Keyboard.Modifiers & ModifierKeys.Control) > 0;

            switch (e.Key)
            {
                // '<' -> select previous solution tree item
                case Key.OemComma:
                    {
                        Editor.TreeSelectPreviousItem(isControl);
                    }
                    break;

                // '>' -> select next solution tree item
                case Key.OemPeriod:
                    {
                        Editor.TreeSelectNextItem(isControl);
                    }
                    break;

                // '[' -> select previous element
                case Key.OemOpenBrackets:
                    {
                        // use control Key to select many element
                        Editor.SelectPrevious(!isControl);
                    }
                    break;

                // ']' -> select next element
                case Key.OemCloseBrackets:
                    {
                        // use control Key to select many element
                        Editor.SelectNext(!isControl);
                    }
                    break;

                // '|' -> select connected elements
                case Key.OemPipe:
                    {
                        Editor.SelectConnected();
                    }
                    break;

                // Ctrl+J:
                // -> add new project to selected solution
                // -> add new diagram to selected project
                // -> add new diagram after selected diagram and select new diagram
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

                // Ctrl+M
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

                // Ctrl+O -> open solution
                // O -> insert Output
                case Key.O:
                    {
                        if (isControl == true)
                        {
                            Editor.OpenSolution();
                            InitializeTagEditor();
                            e.Handled = true;
                            break;
                        }
                        else
                        {
                            var point = GetInsertionPoint();
                            InsertOutput(canvas, point);
                            e.Handled = true;
                            break;
                        }
                    }

                // Ctrl+S -> save solution
                // S -> invert wire start
                case Key.S:
                    {
                        if (isControl == true)
                        {
                            Editor.SaveSolution();
                            e.Handled = true;
                            break;
                        }
                        else
                        {
                            Editor.WireToggleStart();
                        }
                    }
                    break;

                // Ctrl+N -> new solution
                case Key.N:
                    {
                        if (isControl == true)
                        {
                            Editor.TreeCreateNewSolution();
                            InitializeTagEditor();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // Ctrl+T -> open tags
                case Key.T:
                    {
                        if (isControl == true)
                        {
                            Editor.TagsOpen();
                            InitializeTagEditor();
                            e.Handled = true;
                            break;
                        }
                    }
                    break;

                // Ctrl+I -> import
                // I -> insert Input
                case Key.I:
                    {
                        if (isControl == true)
                        {
                            Editor.ModelImport();
                            e.Handled = true;
                        }
                        else
                        {
                            var point = GetInsertionPoint();
                            InsertInput(canvas, point);

                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+E -> export to dxf
                // E -> invert wire end
                case Key.E:
                    {
                        if (isControl == true)
                        {
                            var table = TableGrid.GetData(this) as DiagramTable;
                            Editor.DxfExport(ShortenStart.IsChecked.Value, ShortenEnd.IsChecked.Value, table);
                            e.Handled = true;
                        }
                        else
                        {
                            Editor.WireToggleEnd();
                        }
                    }
                    break;

                // Ctrl+P -> print
                case Key.P:
                    {
                        if (isControl == true)
                        {
                            Print();
                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+R -> reset tags
                // R -> insert OrGate
                case Key.R:
                    {
                        if (isControl == true)
                        {
                            Editor.ModelResetThumbTags();
                            e.Handled = true;
                        }
                        else
                        {
                            var point = GetInsertionPoint();
                            InsertOrGate(canvas, point);
                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+Z -> undo
                case Key.Z:
                    {
                        if (isControl == true)
                        {
                            Editor.HistoryUndo();
                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+Y -> redo
                case Key.Y:
                    {
                        if (isControl == true)
                        {
                            Editor.HistoryRedo();
                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+X -> cut
                case Key.X:
                    {
                        if (isControl == true)
                        {
                            Editor.EditCut();
                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+C -> copy
                // C -> connect
                case Key.C:
                    {
                        if (isControl == true)
                        {
                            Editor.EditCopy();
                            e.Handled = true;
                        }
                        else
                        {
                            Connect();
                        }
                    }
                    break;

                // Ctrl+V -> paste
                case Key.V:
                    {
                        // paste from clipboard
                        if (isControl == true)
                        {
                            //var point = GetInsertionPoint();
                            //if (point == null)
                            //    point = new PointEx(0.0, 0.0);
                            
                            var point = new PointEx(0.0, 0.0);

                            Editor.EditPaste(point, true);
                            e.Handled = true;
                        }
                    }
                    break;

                // Ctrl+A -> select all
                // A -> insert AndGate
                case Key.A:
                    {
                        if (isControl == true)
                        {
                            Editor.SelectAll();
                            e.Handled = true;
                        }
                        else
                        {
                            var point = GetInsertionPoint();
                            InsertAndGate(canvas, point);
                            e.Handled = true;
                        }
                    }
                    break;

                // Del -> delete
                case Key.Delete:
                    {
                        if (GuidesAdorner == null)
                        {
                            // delete selected elements
                            Editor.EditDelete();
                        }
                        else
                        {
                            var elements = Model.GetSelected(canvas);

                            // delete selected elements
                            if (elements.Count() > 0)
                            {
                                Editor.EditDelete(canvas, elements);
                            }
                            // delete single element using guides
                            else
                            {
                                Editor.Delete(canvas, GetInsertionPoint());
                            }
                        }

                        e.Handled = true;
                    }
                    break;

                // Up Arrow -> move selected elements/line guides up
                case Key.Up:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            if (GuidesAdorner == null)
                            {
                                Editor.MoveUp(canvas);
                            }
                            else
                            {
                                var prop = Editor.Context.CurrentCanvas.GetProperties();
                                double y = GuidesAdorner.Y;

                                y -= prop.SnapY;
                                if (y >= (prop.SnapOffsetY + prop.SnapY))
                                    GuidesAdorner.Y = y;
                            }
                            
                            e.Handled = true;
                        }
                    }
                    break;

                // Down Arrow -> move selected elements/line guides down
                case Key.Down:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            if (GuidesAdorner == null)
                            {
                                Editor.MoveDown(canvas);
                            }
                            else
                            {
                                var prop = Editor.Context.CurrentCanvas.GetProperties();
                                double y = GuidesAdorner.Y;

                                y += prop.SnapY;
                                if (y <= canvas.GetHeight() - prop.SnapY - prop.SnapOffsetY)
                                    GuidesAdorner.Y = y;
                            }

                            e.Handled = true;
                        }
                    }
                    break;

                // Left Arrow -> move selected elements/line guides left
                case Key.Left:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            if (GuidesAdorner == null)
                            {
                                Editor.MoveLeft(canvas);
                            }
                            else
                            {
                                var prop = Editor.Context.CurrentCanvas.GetProperties();
                                double x = GuidesAdorner.X;

                                x -= prop.SnapX;
                                if (x >= (prop.SnapOffsetX + prop.SnapX))
                                    GuidesAdorner.X = x;
                            }

                            e.Handled = true;
                        }
                    }
                    break;

                // Right Arrow -> move selected elements/line guides right
                case Key.Right:
                    {
                        if (e.OriginalSource is ScrollViewer)
                        {
                            if (GuidesAdorner == null)
                            {
                                Editor.MoveRight(canvas);
                            }
                            else
                            {
                                var prop = Editor.Context.CurrentCanvas.GetProperties();
                                double x = GuidesAdorner.X;

                                x += prop.SnapX;
                                if (x <= canvas.GetWidth() - prop.SnapX - prop.SnapOffsetX)
                                    GuidesAdorner.X = x;
                            }

                            e.Handled = true;
                        }
                    }
                    break;

                // F5 -> tag editor
                case Key.F5:
                    {
                        InitializeTagEditor();
                        e.Handled = true;
                    }
                    break;

                // F6 -> table editor
                case Key.F6:
                    {
                        ShowTableEditor();
                        e.Handled = true;
                    }
                    break;

                // Ctrl+H -> show diagram history
                case Key.H:
                    {
                        if (isControl == true)
                        {
                            ShowDiagramHistory();
                            e.Handled = true;
                        }
                    }
                    break;

                // F7 -> show project diagrams
                case Key.F7:
                    {
                        ShowProjectDiagrams();
                        e.Handled = true;
                    }
                    break;

                // F8 -> show solution diagrams
                case Key.F8:
                    {
                        ShowSolutionDiagrams();
                        e.Handled = true;
                    }
                    break;

                // G -> show/hide guides
                case Key.G:
                    {
                        ToggleGuides();
                    }
                    break;

                // Esc -> deselect all/cancel connection/hide guides
                case Key.Escape:
                    {
                        // deselect all
                        Editor.SelectNone();

                        // cancel connection
                        Editor.MouseEventRightDown(canvas);

                        // hide guides
                        if (GuidesAdorner != null)
                            HideGuides();
                    }
                    break;
            }
        }


        #endregion

        #region Guides

        private void ToggleGuides()
        {
            var point = GetInsertionPoint();

            if (GuidesAdorner == null)
            {
                var prop = Editor.Context.CurrentCanvas.GetProperties();

                if (point == null)
                {
                    ShowGuides(prop.SnapX + prop.SnapOffsetX,
                        prop.SnapY + prop.SnapOffsetY);
                }
                else
                {
                    ShowGuides(Editor.SnapOffsetX(point.X, true),
                        Editor.SnapOffsetY(point.Y, true));
                }
            }
            else
            {
                HideGuides();
            }
        }

        private void ShowGuides(double x, double y)
        {
            var canvas = DiagramControl.DiagramCanvas;
            var adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
            GuidesAdorner = new LineGuidesAdorner(canvas);

            RenderOptions.SetEdgeMode(GuidesAdorner, EdgeMode.Aliased);
            GuidesAdorner.SnapsToDevicePixels = false;
            GuidesAdorner.IsHitTestVisible = false;

            double zoom = ZoomSlider.Value;
            double zoom_fx = DiagramControl.CalculateZoom(zoom);

            GuidesAdorner.StrokeThickness = 1.0 / zoom_fx;
            GuidesAdorner.CanvasWidth = canvas.Width;
            GuidesAdorner.CanvasHeight = canvas.Height;
            GuidesAdorner.X = x;
            GuidesAdorner.Y = y;

            adornerLayer.Add(GuidesAdorner);

            GuidesAdorner.Cursor = Cursors.None;
            canvas.Cursor = Cursors.None;
        }

        private void HideGuides()
        {
            var canvas = DiagramControl.DiagramCanvas;
            var adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
            adornerLayer.Remove(GuidesAdorner);
            GuidesAdorner = null;

            canvas.Cursor = Cursors.Arrow;
        }

        #endregion

        #region Connect

        private void Connect()
        {
            var canvas = DiagramControl.DiagramCanvas;

            var point = GetInsertionPoint();
            var elements = this.HitTest(canvas, point, 6.0);
            var pin = elements.Where(x => x is PinThumb).FirstOrDefault();

            bool result = Editor.MouseEventPreviewLeftDown(canvas, point, pin as IThumb);
            if (result == false)
            {
                Editor.MouseEventLeftDown(canvas, point);
            }
        }

        public List<DependencyObject> HitTest(Visual visual, IPoint point, double radius)
        {
            var elements = new List<DependencyObject>();

            var elippse = new EllipseGeometry()
            {
                RadiusX = radius,
                RadiusY = radius,
                Center = new Point(point.X, point.Y),
            };

            var hitTestParams = new GeometryHitTestParameters(elippse);
            var resultCallback = new HitTestResultCallback(result => HitTestResultBehavior.Continue);

            var filterCallback = new HitTestFilterCallback(
                element =>
                {
                    elements.Add(element);
                    return HitTestFilterBehavior.Continue;
                });

            VisualTreeHelper.HitTest(visual, filterCallback, resultCallback, hitTestParams);

            return elements;
        }

        private PointEx GetInsertionPoint()
        {
            PointEx insertionPoint = null;

            if (GuidesAdorner != null)
            {
                double x = GuidesAdorner.X;
                double y = GuidesAdorner.Y;

                insertionPoint = new PointEx(x, y);
            }
            else
            {
                var relativeTo = DiagramControl.DiagramCanvas;
                var point = Mouse.GetPosition(relativeTo);
                double x = point.X;
                double y = point.Y;
                double width = relativeTo.Width;
                double height = relativeTo.Height;

                if (x >= 0.0 && x <= width &&
                    y >= 0.0 && y <= height)
                {
                    insertionPoint = new PointEx(x, y);
                }
            }

            return insertionPoint;
        }

        #endregion

        #region Insert

        private void InsertInput(ICanvas canvas, PointEx point)
        {
            Editor.HistoryAdd(canvas, true);
            
            var element = Editor.InsertInput(canvas, 
                point != null ? point : InsertPointInput);

            if (GuidesAdorner == null)
                Editor.SelectOneElement(element, true);
        }

        private void InsertOutput(ICanvas canvas, PointEx point)
        {
            Editor.HistoryAdd(canvas, true);

            var element = Editor.InsertOutput(canvas, 
                point != null ? point : InsertPointOutput);

            if (GuidesAdorner == null)
                Editor.SelectOneElement(element, true);
        }

        private void InsertOrGate(ICanvas canvas, PointEx point)
        {
            Editor.HistoryAdd(canvas, true);

            var element = Editor.InsertOrGate(canvas, 
                point != null ? point : InsertPointGate);

            if (GuidesAdorner == null)
                Editor.SelectOneElement(element, true);
        }

        private void InsertAndGate(ICanvas canvas, PointEx point)
        {
            Editor.HistoryAdd(canvas, true);

            var element = Editor.InsertAndGate(canvas, 
                point != null ? point : InsertPointGate);

            if (GuidesAdorner == null)
                Editor.SelectOneElement(element, true);
        }

        #endregion

        #region Tag Editor

        public List<IElement> GetSeletedIO()
        {
            var selected = GetSelectedInputOutputElements();

            if (selected.Count == 0)
            {
                var all = GetAllInputOutputElements();
                return all;
            }
            else
            {
                return selected;
            }
        }

        private void InitializeTagEditor()
        {
            var control = this.TagEditorControl;

            if (Editor.Context.Tags == null)
            {
                Editor.Context.Tags = new List<object>();
            }

            control.Selected = GetSeletedIO();
            control.Tags = Editor.Context.Tags;
            control.Initialize();

            DiagramControl.SelectionChanged = () =>
            {
                control.Selected = GetSeletedIO();
                control.UpdateSelected();
            };
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

        #region Table Logo

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
                    SetTableLogo(logoId, fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        private void SetTableLogo(int logoId, string fileName)
        {
            var table = GetCurrentTable();
            if (table != null)
            {
                BitmapImage src = CreateBitmapImage(fileName);

                if (logoId == 1)
                {
                    table.Logo1 = src;

                    UpdateCurrentTable(table);
                }
                else if (logoId == 2)
                {
                    table.Logo2 = src;

                    UpdateCurrentTable(table);
                }
            }
        }

        private void UpdateCurrentTable(DiagramTable table)
        {
            TableGrid.SetData(this, null);
            TableGrid.SetData(this, table);
        }

        private DiagramTable GetCurrentTable()
        {
            var table = TableGrid.GetData(this) as DiagramTable;

            return table;
        }

        private BitmapImage CreateBitmapImage(string fileName)
        {
            BitmapImage src = new BitmapImage();

            src.BeginInit();
            src.UriSource = new Uri(fileName, UriKind.RelativeOrAbsolute);
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.EndInit();

            return src;
        }

        #endregion

        #region Table Editor

        public void ShowTableEditor()
        {
            // SetLogo(1);
            // SetLogo(2);
        }

        #endregion

        #region Zoom Events

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded == false)
                return;

            Editor.Context.ZoomLogBase = LogBaseSlider.Value;
            Editor.Context.ZoomExpFactor = ExpFactorSlider.Value;

            double zoom = ZoomSlider.Value;

            zoom = Math.Round(zoom, 1);

            if (e.OldValue != e.NewValue)
            {
                double zoom_fx = this.DiagramControl.Zoom(zoom);

                if (GuidesAdorner != null)
                {
                    GuidesAdorner.StrokeThickness = 1.0 / zoom_fx;
                }
            }
        }

        #endregion

        #region Fixed Document

        private void SetPrintStrokeSthickness(ResourceDictionary resources)
        {
            if (resources == null)
                return;

            resources[ResourceConstants.KeyLogicStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.LogicThicknessMm);
            resources[ResourceConstants.KeyWireStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.WireThicknessMm);
            resources[ResourceConstants.KeyElementStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.ElementThicknessMm);
            resources[ResourceConstants.KeyIOStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.IOThicknessMm);
            resources[ResourceConstants.KeyPageStrokeThickness] = DipUtil.MmToDip(DxfDiagramCreator.PageThicknessMm);
        }

        private void SetPrintColors(ResourceDictionary resources)
        {
            if (resources == null)
                return;

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
                Width = Editor.Context.CurrentCanvas.GetWidth(),
                Height = Editor.Context.CurrentCanvas.GetHeight()
            };

            Model.Parse(diagram,
                canvas, Editor.Context.DiagramCreator, 
                0, 0, 
                false, false, false, true);

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
            var model = Editor.ModelGenerateFromSelected(Editor.Context.CurrentCanvas);

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
    }

    #endregion
}
