using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
//sing System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RazorGDIControlWPF;
using System.Windows.Threading;
using System.Threading;
using System.Drawing;
using System.IO;
using Microsoft.Win32;

namespace VAG
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region MyRegion
        private Color BACKGROUND_COLOR = Color.White;

        private int NODE_RADIUS = 30;
        private Color NODE_COLOR = Color.Black;
        private Color ACTIVE_NODE_COLOR = Color.LawnGreen;
        private Color DISABLE_NODE_COLOR = Color.Red;
        private Color EDGE_COLOR = Color.Black;
        private Color EDGE_ACTIVE_COLOR = Color.LawnGreen;
        private Color DISABLE_EDGE_COLOR = Color.Red;

        private bool NODE_FILL = false;
        private int NODE_FILL_OFFSET = 6;


        private bool DRAW_NODE_NUMBER = true;
        private bool NODE_NUM_OR_WORD = true;
        private Color NODE_TEXT_COLOR = Color.Black;
        private int NODE_TEXT_SIZE = 10;
        private float EDGE_THICKNESS = 2f;
        private int EDGE_ARROW_SIZE = 7;



        private bool DRAW_EDGE_COMMENT = true;
        private Color EDGE_COMMENT_COLOR = Color.Black;

        //--private Color BACKGROUND_COLOR = Color.White;
        //--private Color NODE_COLOR = Color.Black;
        //--private Color ACTIVE_NODE_COLOR = Color.LawnGreen;
        //--private Color DISABLE_NODE_COLOR = Color.Red;
        //--private Color EDGE_COLOR = Color.Black;
        //--private Color EDGE_ACTIVE_COLOR = Color.LawnGreen;
        //--private Color DISABLE_EDGE_COLOR = Color.Red;
        //private bool DRAW_WEIGHT_EDGE = true;
        //--private float THICKNESS_EDGE = 3.0f;//1.5f;
        //private Color TEXT_COLOR = Color.Black;
        //--private int[][] Matrix;
        //--private System.Drawing.Point[] Points;
        //--private bool NODE_FILL = false;
        //--private int NODE_NOT_FILL_OFFSET = 3;
        //private Color NODE_TEXT_COLOR = Color.Black;
        //private Color EDGE_TEXT_COLOR = Color.Black;

        ////public static Type Numeric = false;
        //--private bool SHOW_NUMBER_NODE = true;
        //--private bool DRAW_NODE_NUMBER_OR_WORD = true;

        private System.Drawing.Point[] Points;
        private int[][] Matrix;
        #endregion

        private System.Timers.Timer fpstimer;
        private DispatcherTimer rendertimer;
        private Thread renderthread;
        private int fps;
        private Pen pen = new Pen(Color.Red);

        public MainWindow()
        {
            InitializeComponent();
        }

        private delegate void fpsdelegate();
        private void showfps()
        {
            //this.Title = "FPS: " + fps; fps = 0;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //fpstimer = new System.Timers.Timer(1000);
            //fpstimer.Elapsed += (sender1, args) =>
            //{
            //    Dispatcher.BeginInvoke(DispatcherPriority.Render, new fpsdelegate(showfps));
            //};
            //fpstimer.Start();

            rendertimer = new DispatcherTimer();
            rendertimer.Interval = TimeSpan.FromMilliseconds(15); /* ~60Hz LCD on my PC */
            rendertimer.Tick += (o, args) => Render();
            //rendertimer.Start();

            Reset();
        }

        private void Render()
        {
            // do lock to avoid resize/repaint race in control
            // where are BMP and GFX recreates
            // better practice is Monitor.TryEnter() pattern, but here we do it simpler
            lock (Canvas.RazorLock)
            {
                //Canvas.RazorGFX.DrawString("habrahabr.ru", System.Drawing.SystemFonts.DefaultFont, System.Drawing.Brushes.Azure, 10, 10);
                //Canvas.RazorPaint();

            }
            Canvas.RazorPaint();
            fps++;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //renderthread.Abort();
            rendertimer.Stop();
            //fpstimer.Stop();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ////readGraph("1.txt");
            ////drawGraph();
            ////Canvas.RazorPaint();

            Algorithm.Dijkstra dj = new Algorithm.Dijkstra();
            dj.ActiveNode += ActiveNode;
            dj.DisableNode += InactiveNode;
            dj.ActiveEdge += ActiveEdge;
            dj.InactiveEdge += InactiveEdge;
            dj.WriteLog += WriteLog;
            //dj.Calculate(Matrix, 100);

            //rendertimer.Start();
            Task t = new Task(() =>
            {
                dj.Calculate(Matrix);
            });
            t.Start();

        }

        private void WriteLog(string message)
        {
            txtLog.Dispatcher.BeginInvoke((Action)(() =>
           {
               txtLog.Text += message + Environment.NewLine;
               txtLog.ScrollToEnd();
           }));
        }

        OpenFileDialog ofd;
        private void btnLoadGraph_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
            Reset();
            ofd = new OpenFileDialog();
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ofd.Filter = "Graph File (*.graph)|*.graph";
            ofd.ShowDialog();
            if(ofd.FileName!="")
            {
                readGraph(ofd.FileName);
                drawGraph();
            }
            btnStart.IsEnabled = true;
        }

        private void btnSetSettings_Click(object sender, RoutedEventArgs e)
        {
            SetSettings();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            if (t != null)
                t.Abort();
        }

        private void cmbAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool flag = false;
            switch (((sender as ComboBox).SelectedItem as ComboBoxItem).Content)
            {
                case "Кратчайший путь(Дейкстра)":
                    {
                        radioDrawNumNode.IsChecked = true;
                        chckShowWeightEdge.IsChecked = true;
                        txtRadiusNode.Text = "25";
                        txtThicknessEdge.Text = "2";
                        flag = true;
                        break;
                    }
                case "Эйлеров цикл(Флюри)":
                    {
                        chckShowWeightEdge.IsChecked = false;
                        radioDrawWordNode.IsChecked = true;
                        txtRadiusNode.Text = "30";
                        txtThicknessEdge.Text = "3";
                        flag = true;
                        break;
                    }
                case "Насыщение сети(Форд-Фалкерсон)":
                    {
                        radioDrawNumNode.IsChecked = true;
                        chckShowWeightEdge.IsChecked = true;
                        txtRadiusNode.Text = "25";
                        txtThicknessEdge.Text = "2";
                        flag = true;
                        break;
                    }
                default:
                    WriteLog("Читаю фаил...");
                    WriteLog("Ошибка.");
                    break;
            }
            if(flag)
            {
                WriteLog("Читаю фаил...");
                WriteLog("Граф определен.");
                WriteLog("Алгоритм выставлен");
            }
            SetSettings();
        }
        Thread t;
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            readGraph(ofd.FileName);
            WriteLog("----------------------Сброс--------------------");
            switch (cmbAlgorithm.Text)
            {
                case "Кратчайший путь(Дейкстра)":
                    {
                        Algorithm.Dijkstra dj = new Algorithm.Dijkstra();
                        dj.ActiveNode += ActiveNode;
                        dj.DisableNode += DisableNode;
                        dj.ActiveEdge += ActiveEdge;
                        dj.InactiveEdge += InactiveEdge;
                        dj.WriteLog += WriteLog;

                        t = new Thread(delegate ()
                        {
                            dj.Calculate(Matrix);
                        });

                        t.Start();
                        break;
                    }
                case "Эйлеров цикл(Флюри)":
                    {
                        Algorithm.Fleury fl = new Algorithm.Fleury();
                        fl.ActiveNode += ActiveNode;
                        fl.InactiveNode += InactiveNode;
                        fl.ActiveEdge += ActiveEdge;
                        fl.DisableEdge += DisableEdge;
                        fl.WriteLog += WriteLog;

                        t = new Thread(delegate ()
                        {
                            fl.Calculate(Matrix);
                        });

                        t.Start();
                        break;
                    }
                case "Насыщение сети(Форд-Фалкерсон)":
                    {
                        Algorithm.Ford_Fulkerson fl = new Algorithm.Ford_Fulkerson();
                        fl.ActiveNode += ActiveNode;
                        fl.InactiveNode += InactiveNode;
                        fl.ActiveEdge += ActiveEdge;
                        fl.InactiveEdge += InactiveEdge;
                        fl.RedrawGraph += RedrawGrah;
                        fl.DisableEdge += DisableEdge;
                        fl.WriteLog += WriteLog;

                        t = new Thread(delegate ()
                        {
                            fl.Calculate(deepCopy<int[][]>(Matrix));
                        });

                        t.Start();
                        break;
                    }
                default:
                    MessageBox.Show("Выберите Алгоритм из списка!");
                    break;
            }
        }

        private void sliderSpeedAnimation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.SpeedAnimation = (int)e.NewValue;
        }
    }

    public partial class MainWindow
    {
        private void readGraph(string pathToFile)
        {
            string[] strArray = File.ReadAllText(pathToFile).Split('-');
            switch (strArray[0].TrimEnd())
            {
                case "Dijkstra":
                    cmbAlgorithm.SelectedIndex = 0;
                    break;
                case "Fleury":
                    cmbAlgorithm.SelectedIndex = 1;
                    break;
                case "Ford_Fulkerson":
                    cmbAlgorithm.SelectedIndex = 2;
                    break;
                default:
                    MessageBox.Show("Не известный мне граф");
                    break;
            }
            Points = new System.Drawing.Point[strArray[1].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None).Length];
            Matrix = new int[Points.Length][];
            int index = -1;
            string[] textArray3 = new string[] { "\r\n" };
            foreach (string item in strArray[1].Trim().Split(textArray3, StringSplitOptions.None))
            {
                int[] numArray = item.Split(' ').Select<string, int>(new Func<string, int>(int.Parse)).ToArray<int>();
                Points[++index] = new System.Drawing.Point(numArray[0], numArray[1]);
            }
            index = -1;
            foreach (string item in strArray[2].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                if (item == "")
                    continue;
                Matrix[++index] = item.TrimEnd().Replace("inf", 0x7fffffff.ToString()).Split(' ').Select<string, int>(new Func<string, int>(int.Parse)).ToArray<int>();
            }
        }

        void drawNode(System.Drawing.Point p, string NumNode, Color color)
        {
            if (NODE_FILL)
                Canvas.RazorGFX.FillEllipse(new SolidBrush(color), new System.Drawing.Rectangle(new System.Drawing.Point(p.X - (NODE_RADIUS / 2), p.Y - (NODE_RADIUS / 2)), new System.Drawing.Size(NODE_RADIUS, NODE_RADIUS)));
            else
            {
                //Canvas.RazorGFX.DrawEllipse(new Pen(NODE_COLOR,NODE_UNFILL_THICKNESS), new System.Drawing.Rectangle(new System.Drawing.Point(p.X - (NODE_RADIUS / 2), p.Y - (NODE_RADIUS / 2)), new System.Drawing.Size(NODE_RADIUS, NODE_RADIUS)));
                Canvas.RazorGFX.FillEllipse(new SolidBrush(color), new System.Drawing.Rectangle(new System.Drawing.Point(p.X - (NODE_RADIUS / 2), p.Y - (NODE_RADIUS / 2)), new System.Drawing.Size(NODE_RADIUS, NODE_RADIUS)));
                Canvas.RazorGFX.FillEllipse(new SolidBrush(BACKGROUND_COLOR),
                    new System.Drawing.Rectangle(new System.Drawing.Point(p.X - ((NODE_RADIUS - NODE_FILL_OFFSET) / 2), p.Y - ((NODE_RADIUS - NODE_FILL_OFFSET) / 2)),
                    new System.Drawing.Size(NODE_RADIUS - NODE_FILL_OFFSET, NODE_RADIUS - NODE_FILL_OFFSET)));
            }
            if (DRAW_NODE_NUMBER)
                Canvas.RazorGFX.DrawString(NumNode,
                    new Font(new Font("Arial", NODE_TEXT_SIZE), System.Drawing.FontStyle.Bold),
                    new SolidBrush(color),
                    new PointF(p.X - ((NODE_RADIUS - (NODE_TEXT_SIZE+(NODE_TEXT_SIZE/2))) / 2), p.Y - ((NODE_RADIUS - (NODE_TEXT_SIZE+(NODE_TEXT_SIZE / 2))) / 2)));
                    //new PointF(p.X - ((NODE_RADIUS - (NODE_TEXT_SIZE)) / 2), p.Y - ((NODE_RADIUS - (NODE_TEXT_SIZE)) / 2)));
        }
        void drawEdge(int Node1, int Node2, string comment, Color color)
        {
            System.Drawing.Point p1 =Points[Node1], p2= Points[Node2];
            Pen pen = new Pen(color, EDGE_THICKNESS);
            FixEdge(ref p1, ref p2);
            if ((Matrix[Node1][Node2] != Matrix[Node2][Node1]) && Matrix[Node2][Node1] == int.MaxValue)
            {
                pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(EDGE_ARROW_SIZE, EDGE_ARROW_SIZE, true);
            }
            Canvas.RazorGFX.DrawLine(pen, p1, p2);
            if (DRAW_EDGE_COMMENT)
            {
                int x = (p1.X + p2.X) / 2;
                int y = (p1.Y + p2.Y) / 2;
                Canvas.RazorGFX.DrawString(comment,
                    new Font(new Font("Arial", NODE_TEXT_SIZE), System.Drawing.FontStyle.Bold),
                    new SolidBrush(EDGE_COMMENT_COLOR),
                    new PointF(x, y));
            }
        }
        void FixEdge(ref System.Drawing.Point p1, ref System.Drawing.Point p2)
        {
            double Hypotenuse = Math.Sqrt((Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
            double sideA = Math.Abs(p2.X - p1.X);
            double sideB = Math.Sqrt(Math.Pow(Hypotenuse, 2) - Math.Pow(sideA, 2));

            double sinBeta = sideB / Hypotenuse;
            double cosBeta = sideA / Hypotenuse;

            if (p1.X < p2.X && p1.Y > p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X + (int)((NODE_RADIUS/2) * cosBeta)), Math.Abs(p1.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X - (int)((NODE_RADIUS/2) * cosBeta)), Math.Abs(p2.Y + (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X < p2.X && p1.Y < p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X + (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y + (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X > p2.X && p1.Y > p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X + (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y + (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X > p2.X && p1.Y < p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y + (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X + (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X == p2.X && p1.Y > p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y + (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X == p2.X && p1.Y < p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y + (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X > p2.X && p1.Y == p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X + (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
            }
            else if (p1.X < p2.X && p1.Y == p2.Y)
            {
                p1 = new System.Drawing.Point(Math.Abs(p1.X + (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p1.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
                p2 = new System.Drawing.Point(Math.Abs(p2.X - (int)((NODE_RADIUS / 2) * cosBeta)), Math.Abs(p2.Y - (int)((NODE_RADIUS / 2) * sinBeta)));
            }
        }

        void drawGraph()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                if (NODE_NUM_OR_WORD)
                    drawNode(Points[i], (i+1).ToString(),NODE_COLOR);
                else
                    drawNode(Points[i], ((char)(i + 97)).ToString(), NODE_COLOR);
                for (int j = 0; j < Matrix[i].Length; j++)
                {
                    if (Matrix[i][j] != 0x7fffffff)
                    {
                        drawEdge(i, j, Matrix[i][j].ToString(),EDGE_COLOR);
                    }
                }
            }
            Canvas.RazorPaint();
        }

        void ActiveNode(int Node)
        {
            drawNode(Points[Node], (Node+1).ToString(), ACTIVE_NODE_COLOR);
            UpdateCanvas();
        }
        void InactiveNode(int Node)
        {
            drawNode(Points[Node], (Node + 1).ToString(), NODE_COLOR);
            UpdateCanvas();
        }
        void DisableNode(int Node)
        {
            drawNode(Points[Node], (Node + 1).ToString(), DISABLE_NODE_COLOR);
            UpdateCanvas();
        }


        void ActiveEdge(int Node1, int Node2)
        {
            drawEdge(Node1, Node2, "", EDGE_ACTIVE_COLOR);
            UpdateCanvas();
        }
        void ActiveEdge(int Node1, int Node2,Color color)
        {
            drawEdge(Node1, Node2, "", color);
            UpdateCanvas();
        }
        void InactiveEdge(int Node1, int Node2)
        {
            drawEdge(Node1, Node2, "",EDGE_COLOR);
            UpdateCanvas();
        }
        void DisableEdge(int Node1, int Node2)
        {
            drawEdge(Node1, Node2, "", DISABLE_EDGE_COLOR);
            UpdateCanvas();
        }

        void RedrawGrah()
        {
            Canvas.RazorGFX.Clear(BACKGROUND_COLOR);
            Canvas.RazorPaint();
            drawGraph();
        }

        void UpdateCanvas()
        {
            Canvas.RazorPaint();
        }


        void Reset()
        {
            Canvas.RazorGFX.Clear(BACKGROUND_COLOR);
            Canvas.RazorPaint();
            txtLog.Clear();
        }
        private void SetSettings() 
        {
            //NODE_COLOR = clNode.BackColor;
            //EDGE_COLOR = clEdge.BackColor;
            NODE_NUM_OR_WORD = radioDrawNumNode.IsChecked.Value;
            DRAW_NODE_NUMBER = !radioNotDrawingNode.IsChecked.Value;
            DRAW_EDGE_COMMENT = chckShowWeightEdge.IsChecked.Value;
            int.TryParse(txtRadiusNode.Text, out NODE_RADIUS);
            float.TryParse(txtThicknessEdge.Text, out EDGE_THICKNESS);
        }
        private static T deepCopy<T>(T other)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
    }

    public static class Global
    {
        private static int speedAnimation = 1;
        public static int SpeedAnimation
        {
            get => speedAnimation;
            set => speedAnimation = value;
        }
    }
}
namespace Algorithm
{
    interface IGraph
    {
        void Calculate(int[][] Matrix);
    }

    class Dijkstra : IGraph
    {
        class Node
        {
            public int L = int.MaxValue;//lenght path to node
            public int O = -1;//Last node
                              //public bool Active = true;//Status node
        }
        public delegate void NodeDelegate(int Node);
        public event NodeDelegate ActiveNode;
        public event NodeDelegate DisableNode;

        public delegate void EdgeDelegate(int Node1, int Node2);
        public event EdgeDelegate ActiveEdge;
        public event EdgeDelegate InactiveEdge;

        public delegate void LogDelegate(string message);
        public event LogDelegate WriteLog;


        public void Calculate(int[][] Matrix)
        {
            List<Node> node = new List<Node>();
            int INF = int.MaxValue;
            for (int i = 1; i <= Matrix[0].Length; i++) node.Add(new Node());

            for (int i = 0; i < Matrix[0].Length - 1; i++)
            {
                WriteLog($"Поиск наименьших путей с ноды [{i + 1}].");
                ActiveNode(i);
                for (int j = 0; j < Matrix[0].Length; j++)
                {
                    if (Matrix[i][j] != int.MaxValue)
                    {
                        if (i == 12 && j == 11)
                            Console.WriteLine();
                        ActiveEdge(i, j);
                        int lastLenght = node[i].L != INF ? node[i].L + Matrix[i][j] : Matrix[i][j];
                        if (lastLenght < node[j].L)
                        {
                            WriteLog($"Найден более короткий путь.");
                            node[j].O = i;
                            node[j].L = node[i].L != INF ? Matrix[i][j] + node[i].L : Matrix[i][j];
                        }
                        System.Threading.Thread.Sleep(VAG.Global.SpeedAnimation);
                        InactiveEdge(i, j);
                    }
                }
                DisableNode(i);
            }

            //Вывод
            string path = "-" + node.Count;
            //int _lengthPath = 0;
            for (int i = node.Count - 1; ;)
            {
                //_lengthPath += node[i].L;
                path = path.Insert(0, "-" + (node[i].O + 1).ToString());
                ActiveNode(i);
                int lastNode = i;
                i = node[i].O;
                ActiveEdge(i, lastNode);

                if (i == 0)
                {
                    ActiveNode(i);
                    break;
                }
            }
            WriteLog(String.Format("Путь: {0}, Длинна:{1}", path.Remove(0, 1), node[node.Count - 1].L));
        }
    }
    //Eulerean Cyrcle
    class Fleury : IGraph
    {
        public delegate void NodeDelegate(int Node);
        public event NodeDelegate ActiveNode;
        public event NodeDelegate InactiveNode;

        public delegate void EdgeDelegate(int Node1, int Node2);
        public event EdgeDelegate ActiveEdge;
        public event EdgeDelegate DisableEdge;

        public delegate void LogDelegate(string message);
        public event LogDelegate WriteLog;

        private static int countEdge(ref int[][] matrix, int vertex)
        {
            int _temp = 0;
            for (int i = 0; i < matrix.Length; i++)
            {
                if (matrix[vertex][i] == 1)
                    _temp++;
            }
            return _temp;
        }
        private static void removeEdge(ref int[][] matrix, int vertex, int edge, out int newVertex)
        {
            int _temp = 0;
            newVertex = 0;
            for (int i = 0; i < matrix.Length; i++)
            {
                if (matrix[vertex][i] == 1)
                {
                    _temp++;
                    if (_temp == edge)
                    {
                        matrix[vertex][i] = 0;
                        matrix[i][vertex] = 0;
                        newVertex = i;
                        return;
                    }
                }
            }
        }

        public void Calculate(int[][] Matrix)
        {
            Stack<int> vertex = new Stack<int>();
            //vertex.Push(new Random().Next(matrix.GetLength(0)));//Начинаем с рандомного узла
            vertex.Push(0);//Начинаем с 1го узла
            string eulerCircuitVertex = string.Empty;
            int newVertex;
            List<int> p = new List<int>();
            while (vertex.Count != 0)
            {
                int V = countEdge(ref Matrix, vertex.Peek());
                WriteLog($"Из ноды [{(char)(vertex.Peek() + 97)}] доступно ребер - {V}");
                if (V == 0)
                {
                    p.Add(vertex.Peek());
                    //ActiveNode(vertex.Peek());
                    if (p.Count > 1)
                    {
                        ActiveEdge(p[p.Count - 2], p[p.Count - 1]);
                    }
                    eulerCircuitVertex += (char)(vertex.Peek() + 97) + "-";
                    vertex.Pop();
                    System.Threading.Thread.Sleep(VAG.Global.SpeedAnimation);
                }
                else
                {
                    //ActiveNode(vertex.Peek());
                    WriteLog($"Выбираем любое непосещенное ребро.");
                    int randomEdge = new Random().Next(V) + 1;

                    WriteLog($"Удаляем его из списка непосещенных.");
                    removeEdge(ref Matrix, vertex.Peek(), randomEdge, out newVertex);
                    DisableEdge(vertex.Peek(), newVertex);
                    vertex.Push(newVertex);
                    //ActiveNode(vertex.Peek());
                    System.Threading.Thread.Sleep(VAG.Global.SpeedAnimation);
                }
            }

            WriteLog($"Эйлеров цикл: {eulerCircuitVertex.Remove(eulerCircuitVertex.Length - 1)}");
        }
    }
    //Max Flow in a flow network (Задача о максимальном потоке)

    class Ford_Fulkerson:IGraph
    {
        class Node
        {
            private int flowCapacity;
            private int flowBusy;
            private bool flowSealed;

            public int FlowCapacity { get => flowCapacity; set => flowCapacity = value; }
            public int FlowBusy { get => flowBusy; set => flowBusy = value; }
            public bool FlowSealed { get => flowSealed; set => flowSealed = value; }


            public static Node Parse(string s)
            {
                Console.WriteLine();
                return new Node { FlowCapacity = int.Parse(s), FlowBusy = 0, FlowSealed = false };
            }
        }

        class Path
        {
            public int i;
            public int j;
        }

        public delegate void NodeDelegate(int Node);
        public event NodeDelegate ActiveNode;
        public event NodeDelegate InactiveNode;

        public delegate void EdgeDelegate(int Node1, int Node2);
        public event EdgeDelegate ActiveEdge;
        public event EdgeDelegate InactiveEdge;
        public event EdgeDelegate DisableEdge;

        public delegate void Graphics();
        public event Graphics RedrawGraph;

        public delegate void LogDelegate(string message);
        public event LogDelegate WriteLog;

        public void Calculate(int[][] fnMatrix)
        {
            Node[][] FlowMatrix = new Node[fnMatrix.Length][];

            for (int i = 0; i < fnMatrix.Length; i++)
            {
                for (int j = 0; j < fnMatrix[i].Length; j++)
                {
                    if (fnMatrix[i][j] == int.MaxValue)
                        fnMatrix[i][j] = 0;
                }
            }

            for (int i = 0; i < FlowMatrix.Length; i++)
                FlowMatrix[i] = fnMatrix[i].Select<int, Node>(num => new Node() { FlowCapacity = num, FlowBusy = 0, FlowSealed = false }).ToArray<Node>();


            //
            //string tempPath = "1-";
            List<int> path2 = new List<int>();
            int minFlow = int.MaxValue;
            Stack<Path> AA = new Stack<Path>();
            Stack<Path> Full = new Stack<Path>();
            while (true)
            {
                path2.Clear();
                path2.Add(0);
                minFlow = int.MaxValue;
                //RedrawGraph();=================================


                //tempPath = "1-";
                bool flag = false;
                for (int i = 0; i < FlowMatrix.Length; i++)
                {
                    flag = false;
                    for (int j = 0; j < FlowMatrix.Length; j++)
                    {
                        if (((FlowMatrix[i][j].FlowCapacity - FlowMatrix[i][j].FlowBusy) > 0) && !FlowMatrix[i][j].FlowSealed)//Если в потоке есть еще место и он не запечатан,действуем
                        {
                            ActiveNode(i);
                            ActiveEdge(i, j);
                            AA.Push(new Path() { i = i, j = j });
                            flag = true;
                            System.Threading.Thread.Sleep(VAG.Global.SpeedAnimation);
                            if (there_are_moves(ref FlowMatrix[j]) || j == FlowMatrix.Length - 1)//Если за потоком есть путь или это последний поток,идем дальше
                            {
                                //tempPath += (j + 1) + "-";
                                //tempPath += (j) + "-";
                                path2.Add(j);
                                if (minFlow > (FlowMatrix[i][j].FlowCapacity - FlowMatrix[i][j].FlowBusy))
                                {
                                    minFlow = (FlowMatrix[i][j].FlowCapacity - FlowMatrix[i][j].FlowBusy);
                                    WriteLog($"Найден временный минимум в потоке :{minFlow} ");
                                }

                                i = j;
                                j = j == (FlowMatrix.Length - 1) ? j : 0;
                            }
                            else
                            {
                                WriteLog($"За потоком,все пути запечатаны.");
                                WriteLog($"Запечатываем этот поток,и возвращаемся назад");
                                FlowMatrix[i][j].FlowSealed = true;//Запечатываем поток,потому что войдя в него, никуда не выйти.
                                DisableEdge(i, j);
                                Full.Push(new Path() { i = i, j = j });
                                AA.Pop();
                                --j;
                                System.Threading.Thread.Sleep(VAG.Global.SpeedAnimation / 2);

                            }
                            //InactiveEdge(i, j);
                        }
                        else if ((FlowMatrix[i][j].FlowCapacity != 0) && (FlowMatrix[i][j].FlowCapacity == FlowMatrix[i][j].FlowBusy))
                        {
                            WriteLog($"В потоке больше нет места,запечатываем текущий поток.");
                            Full.Push(new Path() { i = i, j = j });
                            DisableEdge(i, j);
                            System.Threading.Thread.Sleep(VAG.Global.SpeedAnimation / 2);
                        }
                    }
                    //InactiveNode(i);

                    if (!flag)
                        break;
                }


                WriteLog("Возвращаемся назад.");
                while (AA.Count != 0)
                {
                    Path _t = AA.Pop();
                    InactiveEdge(_t.i, _t.j);
                    InactiveNode(_t.i);
                }

                if (path2[path2.Count - 1] != FlowMatrix.Length - 1) break;

                for (int i = 0; i < path2.Count - 1; i++)
                {
                    FlowMatrix[path2[i]][path2[i + 1]].FlowBusy += minFlow;
                }
            }

            int inFlow = 0;
            int outFlow = 0;

            for (int i = 0; i < FlowMatrix.Length; i++)
            {
                if (FlowMatrix[0][i].FlowCapacity != 0)
                {
                    inFlow += FlowMatrix[0][i].FlowBusy;
                }
                if (FlowMatrix[i][FlowMatrix.Length - 1].FlowCapacity != 0)
                {
                    outFlow += FlowMatrix[i][FlowMatrix.Length - 1].FlowBusy;
                }
            }

            for (int i = 0; i < FlowMatrix.Length; i++)
            {
                for (int j = 0; j < FlowMatrix.Length; j++)
                {
                    if (FlowMatrix[i][j].FlowCapacity != 0)
                    {
                        WriteLog(String.Format("C({0}:{1}) - {2}/{3}", i, j, FlowMatrix[i][j].FlowCapacity, FlowMatrix[i][j].FlowBusy));
                    }
                }
            }

            //
            while (Full.Count != 0)
            {
                Path _t = Full.Pop();
                ActiveEdge(_t.i, _t.j);
                //InactiveNode(_t.i);
            }
            //
            WriteLog(String.Format("Вход:{0} Выход:{1}", inFlow, outFlow));
            WriteLog("Зеленным помечены заполненные потоки.");
        }
        bool there_are_moves(ref Node[] Node)
        {
            for (int i = 0; i < Node.Length; i++)
            {
                if ((Node[i].FlowCapacity - Node[i].FlowBusy) > 0)
                    return true;
            }
            return false;
        }
    }

}