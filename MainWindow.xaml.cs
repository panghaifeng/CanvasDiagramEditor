﻿
namespace CanvasDiagramEditor
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
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

    #endregion

    #region MainWindow

    public partial class MainWindow : Window
    {
        #region Fields

        private Line _line = null;
        private FrameworkElement _root = null;

        private int wireCounter = 0;
        private int inputCounter = 0;
        private int outputCounter = 0;
        private int andGateCounter = 0;
        private int orGateCounter = 0;

        private Point rightClick;

        private string lastInsert = "Input";

        private bool enableSnap = true;
        private double snap = 15;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            EnableSnap.IsChecked = enableSnap;
        }

        #endregion

        #region Snap

        public double Snap(double original, double snap, double offset)
        {
            return Snap(original - offset, snap) + offset;
        }

        public double Snap(double original, double snap)
        {
            return original + ((Math.Round(original / snap) - original / snap) * snap);
        }

        public double Snap(double original)
        {
            return enableSnap == true ? Snap(original, snap) : original;
        }

        #endregion

        #region Move

        private void MoveRoot(FrameworkElement element, double dX, double dY)
        {
            double left = Canvas.GetLeft(element) + dX;
            double top = Canvas.GetTop(element) + dY;

            Canvas.SetLeft(element, Snap(left));
            Canvas.SetTop(element, Snap(top));

            MoveLines(element, dX, dY);
        }

        private void MoveLines(FrameworkElement element, double dX, double dY)
        {
            if (element.Tag != null)
            {
                var tuples = element.Tag as List<Tuple<Line, FrameworkElement, FrameworkElement>>;

                foreach (var tuple in tuples)
                {
                    var line = tuple.Item1;
                    var start = tuple.Item2;
                    var end = tuple.Item3;

                    if (start != null)
                    {
                        line.X1 = Snap(line.X1 + dX);
                        line.Y1 = Snap(line.Y1 + dY);
                    }
                    else if (end != null)
                    {
                        line.X2 = Snap(line.X2 + dX);
                        line.Y2 = Snap(line.Y2 + dY);
                    }
                }
            }
        }

        #endregion

        #region Create

        private Line CreateWire(double x1, double y1, double x2, double y2, int id)
        {
            var line = new Line()
            {
                Style = Application.Current.Resources["LineStyleKey"] as Style,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Uid = "Wire|" + id.ToString()
            };

            return line;
        }

        private Thumb CreateInput(double x, double y, int id)
        {
            var thumb = new Thumb()
            {
                Template = Application.Current.Resources["InputControlTemplateKey"] as ControlTemplate,
                Style = Application.Current.Resources["RootThumbStyleKey"] as Style,
                Uid = "Input|" + id.ToString()
            };

            thumb.DragDelta += this.RootElement_DragDelta;

            Canvas.SetLeft(thumb, Snap(x));
            Canvas.SetTop(thumb, Snap(y));

            return thumb;
        }

        private Thumb CreateOutput(double x, double y, int id)
        {
            var thumb = new Thumb()
            {
                Template = Application.Current.Resources["OutputControlTemplateKey"] as ControlTemplate,
                Style = Application.Current.Resources["RootThumbStyleKey"] as Style,
                Uid = "Output|" + id.ToString()
            };

            thumb.DragDelta += this.RootElement_DragDelta;

            Canvas.SetLeft(thumb, Snap(x));
            Canvas.SetTop(thumb, Snap(y));

            return thumb;
        }

        private Thumb CreateAndGate(double x, double y, int id)
        {
            var thumb = new Thumb()
            {
                Template = Application.Current.Resources["AndGateControlTemplateKey"] as ControlTemplate,
                Style = Application.Current.Resources["RootThumbStyleKey"] as Style,
                Uid = "AndGate|" + id.ToString()
            };

            thumb.DragDelta += this.RootElement_DragDelta;

            Canvas.SetLeft(thumb, Snap(x));
            Canvas.SetTop(thumb, Snap(y));

            return thumb;
        }

        private Thumb CreateOrGate(double x, double y, int id)
        {
            var thumb = new Thumb()
            {
                Template = Application.Current.Resources["OrGateControlTemplateKey"] as ControlTemplate,
                Style = Application.Current.Resources["RootThumbStyleKey"] as Style,
                Uid = "OrGate|" + id.ToString()
            };

            thumb.DragDelta += this.RootElement_DragDelta;

            Canvas.SetLeft(thumb, Snap(x));
            Canvas.SetTop(thumb, Snap(y));

            return thumb;
        }

        private void ConnectPins(Canvas canvas, FrameworkElement pin)
        {
            var root = ((pin.Parent as FrameworkElement).Parent as FrameworkElement).TemplatedParent as FrameworkElement;

            _root = root;

            System.Diagnostics.Debug.Print("Canvas_PreviewMouseLeftButtonDown, source: {0}, {1}", pin.GetType(), pin.Name);

            double rx = Canvas.GetLeft(_root);
            double ry = Canvas.GetTop(_root);
            double px = Canvas.GetLeft(pin);
            double py = Canvas.GetTop(pin);
            double x = rx + px;
            double y = ry + py;

            System.Diagnostics.Debug.Print("x: {0}, y: {0}", x, y);

            if (_root.Tag == null)
            {
                _root.Tag = new List<Tuple<Line, FrameworkElement, FrameworkElement>>();
            }

            var tuples = _root.Tag as List<Tuple<Line, FrameworkElement, FrameworkElement>>;

            if (_line == null)
            {
                var line = CreateWire(x, y, x, y, wireCounter);
                wireCounter += 1;

                _line = line;

                var tuple = new Tuple<Line, FrameworkElement, FrameworkElement>(_line, _root, null);
                tuples.Add(tuple);

                canvas.Children.Add(_line);
            }
            else
            {
                _line.X2 = x;
                _line.Y2 = y;

                var tuple = new Tuple<Line, FrameworkElement, FrameworkElement>(_line, null, _root);
                tuples.Add(tuple);

                _line = null;
                _root = null;
            }
        }

        #endregion

        #region Insert

        private void InsertInput(Point point)
        {
            var canvas = this.DiagramCanvas;

            var thumb = CreateInput(point.X, point.Y, inputCounter);
            inputCounter += 1;

            canvas.Children.Add(thumb);
        }

        private void InsertOutput(Point point)
        {
            var canvas = this.DiagramCanvas;

            var thumb = CreateOutput(point.X, point.Y, outputCounter);
            outputCounter += 1;

            canvas.Children.Add(thumb);
        }

        private void InsertAndGate(Point point)
        {
            var canvas = this.DiagramCanvas;

            var thumb = CreateAndGate(point.X, point.Y, andGateCounter);
            andGateCounter += 1;

            canvas.Children.Add(thumb);
        }

        private void InsertOrGate(Point point)
        {
            var canvas = this.DiagramCanvas;

            var thumb = CreateOrGate(point.X, point.Y, orGateCounter);
            orGateCounter += 1;

            canvas.Children.Add(thumb);
        }

        private void InsertLast(Point point)
        {
            switch (lastInsert)
            {
                case "Input":
                    InsertInput(point);
                    break;
                case "Output":
                    InsertOutput(point);
                    break;
                case "AndGate":
                    InsertAndGate(point);
                    break;
                case "OrGate":
                    InsertOrGate(point);
                    break;
            }
        }

        #endregion

        #region Diagram Model

        private void ClearDiagramModel()
        {
            var canvas = this.DiagramCanvas;

            canvas.Children.Clear();

            wireCounter = 0;
            inputCounter = 0;
            outputCounter = 0;
            andGateCounter = 0;
            orGateCounter = 0;
        }

        private string GenerateDiagramModel()
        {
            var canvas = this.DiagramCanvas;
            var sb = new StringBuilder();

            string header = "[Diagram]";

            System.Diagnostics.Debug.Print(header);
            sb.AppendLine(header);

            foreach (var child in canvas.Children)
            {
                var element = child as FrameworkElement;

                double x = Canvas.GetLeft(element);
                double y = Canvas.GetTop(element);

                if (element.Uid.StartsWith("Wire"))
                {
                    var line = element as Line;

                    string str = string.Format("+;{0};{1};{2};{3};{4}", element.Uid, line.X1, line.Y1, line.X2, line.Y2);
                    sb.AppendLine(str);

                    System.Diagnostics.Debug.Print(str);
                }
                else
                {
                    string str = string.Format("+;{0};{1};{2}", element.Uid, x, y);
                    sb.AppendLine(str);

                    System.Diagnostics.Debug.Print(str);
                }

                if (element.Tag != null)
                {
                    var tuples = element.Tag as List<Tuple<Line, FrameworkElement, FrameworkElement>>;

                    foreach (var tuple in tuples)
                    {
                        var line = tuple.Item1;
                        var start = tuple.Item2;
                        var end = tuple.Item3;

                        if (start != null)
                        {
                            // Start
                            string str = string.Format("-;{0};Start", line.Uid);
                            sb.AppendLine(str);

                            System.Diagnostics.Debug.Print(str);
                        }
                        else if (end != null)
                        {
                            // End
                            string str = string.Format("-;{0};End", line.Uid);
                            sb.AppendLine(str);

                            System.Diagnostics.Debug.Print(str);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private void ParseDiagramModel(string diagram)
        {
            var canvas = this.DiagramCanvas;
            var lines = diagram.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var dict = new Dictionary<string, Tuple<FrameworkElement, List<Tuple<string, string>>>>();
            Tuple<FrameworkElement, List<Tuple<string,string>>> tuple = null;

            string name = null;

            ClearDiagramModel();

            // create roor elements
            foreach (var line in lines)
            {
                var args = line.Split(';');
                int length = args.Length;

                if (length >= 2)
                {
                    name = args[1];

                    if (string.Compare(args[0], "+", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        if (name.StartsWith("Input", StringComparison.InvariantCultureIgnoreCase) && length == 4)
                        {
                            double x = double.Parse(args[2]);
                            double y = double.Parse(args[3]);

                            int id = int.Parse(name.Split('|')[1]);

                            inputCounter = Math.Max(inputCounter, id + 1);

                            var element = CreateInput(x, y, id);
                            canvas.Children.Add(element);

                            tuple = new Tuple<FrameworkElement, List<Tuple<string, string>>>(element, new List<Tuple<string, string>>());

                            dict.Add(args[1], tuple);
                        }
                        else if (name.StartsWith("Output", StringComparison.InvariantCultureIgnoreCase) && length == 4)
                        {
                            double x = double.Parse(args[2]);
                            double y = double.Parse(args[3]);

                            int id = int.Parse(name.Split('|')[1]);

                            outputCounter = Math.Max(outputCounter, id + 1);

                            var element = CreateOutput(x, y, id);
                            canvas.Children.Add(element);

                            tuple = new Tuple<FrameworkElement, List<Tuple<string, string>>>(element, new List<Tuple<string, string>>());

                            dict.Add(args[1], tuple);
                        }
                        else if (name.StartsWith("AndGate", StringComparison.InvariantCultureIgnoreCase) && length == 4)
                        {
                            double x = double.Parse(args[2]);  
                            double y = double.Parse(args[3]);

                            int id = int.Parse(name.Split('|')[1]);

                            andGateCounter = Math.Max(andGateCounter, id + 1);

                            var element = CreateAndGate(x, y, id);
                            canvas.Children.Add(element);

                            tuple = new Tuple<FrameworkElement, List<Tuple<string, string>>>(element, new List<Tuple<string, string>>());

                            dict.Add(args[1], tuple);
                        }
                        else if (name.StartsWith("OrGate", StringComparison.InvariantCultureIgnoreCase) && length == 4)
                        {
                            double x = double.Parse(args[2]);
                            double y = double.Parse(args[3]);

                            int id = int.Parse(name.Split('|')[1]);

                            orGateCounter = Math.Max(orGateCounter, id + 1);

                            var element = CreateOrGate(x, y, id);
                            canvas.Children.Add(element);

                            tuple = new Tuple<FrameworkElement, List<Tuple<string, string>>>(element, new List<Tuple<string, string>>());

                            dict.Add(args[1], tuple);
                        }
                        else if (name.StartsWith("Wire", StringComparison.InvariantCultureIgnoreCase) && length == 6)
                        {
                            double x1 = double.Parse(args[2]);  
                            double y1 = double.Parse(args[3]);
                            double x2 = double.Parse(args[4]);
                            double y2 = double.Parse(args[5]);

                            int id = int.Parse(name.Split('|')[1]);

                            wireCounter = Math.Max(wireCounter, id + 1);

                            var element = CreateWire(x1, y1, x2, y2, id);
                            canvas.Children.Add(element);

                            tuple = new Tuple<FrameworkElement, List<Tuple<string, string>>>(element, new List<Tuple<string, string>>());

                            dict.Add(args[1], tuple);
                        }
                    }
                    else if (string.Compare(args[0], "-", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        if (tuple != null)
                        {
                            var wires = tuple.Item2;

                            wires.Add(new Tuple<string, string>(name, args[2]));
                        }
                    }
                }
            }

            // update wire connections
            foreach (var item in dict)
            {
                var element = item.Value.Item1;
                var wires = item.Value.Item2;

                if (element.Tag == null)
                {
                    element.Tag = new List<Tuple<Line, FrameworkElement, FrameworkElement>>();
                }

                if (wires.Count > 0)
                {
                    var tuples = element.Tag as List<Tuple<Line, FrameworkElement, FrameworkElement>>;

                    foreach (var wire in wires)
                    {
                        string _name = wire.Item1;
                        string _type = wire.Item2;

                        if (string.Compare(_type, "Start", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            var line = dict[_name].Item1 as Line;

                            var _tuple = new Tuple<Line, FrameworkElement, FrameworkElement>(line, element, null);
                            tuples.Add(_tuple);
                        }
                        else if (string.Compare(_type, "End", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            var line = dict[_name].Item1 as Line;

                            var _tuple = new Tuple<Line, FrameworkElement, FrameworkElement>(line, null, element);
                            tuples.Add(_tuple);
                        }
                    }
                }
            }
        }

        #endregion

        #region Open/Save

        private void Save(string fileName)
        {
            using (var writer = new System.IO.StreamWriter(fileName))
            {
                string diagram = GenerateDiagramModel();

                writer.Write(diagram);
            }
        }

        private void Open(string fileName)
        {
            using (var reader = new System.IO.StreamReader(fileName))
            {
                string diagram = reader.ReadToEnd();

                ParseDiagramModel(diagram);
            }
        }

        #endregion

        #region Thumb Events

        private void RootElement_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            double dX = e.HorizontalChange;
            double dY = e.VerticalChange;

            MoveRoot(thumb, dX, dY);
        }

        #endregion

        #region Canvas Events

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = DiagramCanvas;
            var point = e.GetPosition(canvas);

            InsertLast(point);
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = DiagramCanvas;
            var pin = (e.OriginalSource as FrameworkElement).TemplatedParent as FrameworkElement;

            if (pin != null)
            {
                ConnectPins(canvas, pin);
                e.Handled = true;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var canvas = DiagramCanvas;

            if (_root != null && _line != null)
            {
                var point = e.GetPosition(canvas);

                double x = point.X;
                double y = point.Y;

                _line.X2 = x;
                _line.Y2 = y;
            }
        }

        private void Canvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            rightClick = e.GetPosition(DiagramCanvas);
            e.Handled = false;
        }

        #endregion

        #region Button Events

        private void OpenModel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Diagram (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Open Diagram"
            };

            var res = dlg.ShowDialog();
            if (res == true)
            {
                this.Open(dlg.FileName);
            }
        }

        private void SaveModel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "Diagram (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Save Diagram",
                FileName = "diagram"
            };

            var res = dlg.ShowDialog();
            if (res == true)
            {
                this.Save(dlg.FileName);
            }
        }

        private void ClearModel_Click(object sender, RoutedEventArgs e)
        {
            ClearDiagramModel();
        }

        private void GenerateModel_Click(object sender, RoutedEventArgs e)
        {
            var text = GenerateDiagramModel();

            model.Text = text;
        }

        private void ParseModel_Click(object sender, RoutedEventArgs e)
        {
            var text = model.Text;

            ParseDiagramModel(text);
        }

        #endregion

        #region Context Menu Events

        private void InsertInput_Click(object sender, RoutedEventArgs e)
        {
            InsertInput(this.rightClick);

            lastInsert = "Input";
        }

        private void InsertOutput_Click(object sender, RoutedEventArgs e)
        {
            InsertOutput(this.rightClick);

            lastInsert = "Output";
        }

        private void InsertAndGate_Click(object sender, RoutedEventArgs e)
        {
            InsertAndGate(this.rightClick);

            lastInsert = "AndGate";
        }

        private void InsertOrGate_Click(object sender, RoutedEventArgs e)
        {
            InsertOrGate(this.rightClick);

            lastInsert = "OrGate";
        } 

        #endregion

        #region CheckBox Events

        private void EnableSnap_Click(object sender, RoutedEventArgs e)
        {
            enableSnap = EnableSnap.IsChecked == true ? true : false;
        } 

        #endregion
    }

    #endregion
}
