using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace BFS_With_Maze_Demo
{
    /// <summary>
    /// 某一个格子本身的状态
    /// </summary>
    public enum Status { EXIT, ENTER, CLEAR, BARREL, };


    public class WindowModel
    {
        private CancellationTokenSource CTS = new CancellationTokenSource();
        public Dispatcher Dispatcher;
        public ButtonModel[,] buttonModels;
        public Queue<(int r, int c)> ShortestPath { get; private set; } = new Queue<(int r, int c)>();
        (int r, int c) cur;
        private bool isStepping = false;
        public bool Stepping
        {
            get => isStepping;
            set
            {
                if (!isStepping && value)
                {
                    Task.Factory.StartNew(() => StepIn(ShortestPath,CTS), CTS.Token);
                    isStepping = value;
                }
                else if (isStepping && !value)
                {
                    CTS.Cancel();
                    CTS = new CancellationTokenSource();
                    isStepping = value;
                }
            }
        }

        private int R, C;

        public WindowModel(int r = 1, int c = 1) => Resize(r, c);

        /// <summary>
        /// 重新生成相应大小的网格元素
        /// </summary>
        /// <param name="r">所需生成的行数</param>
        /// <param name="c">列数</param>
        public void Resize(int r, int c)
        {
            if (r <= 0 || c <= 0)
                throw new ArgumentOutOfRangeException("r || c");
            R = r; C = c;
            buttonModels = new ButtonModel[r, c];
            for (int i = 0; i < r; ++i)
                for (int j = 0; j < c; ++j)
                {
                    buttonModels[i, j] = new ButtonModel();
                    buttonModels[i, j].OnStatusChanged += (s, e) =>
                    {
                        if (Stepping == true)
                        {
                            Stepping = false;
                            RefreshPath();
                            Stepping = true;
                        }
                        else RefreshPath();
                    };
                }
            buttonModels[0, 0] = Status.ENTER;
            buttonModels[0, 0].IsDriven = true;
            buttonModels[r - 1, c - 1] = Status.EXIT;
            ShortestPath.Enqueue(cur = (0, 0));
            RefreshPath();
        }

        /// <summary>
        /// 使用 BFS 算法查询最短路，如果没有最短路则返回一个单个节点的路径
        /// </summary>
        public void RefreshPath()
        {
            Queue<(int r, int c)> q = new Queue<(int r, int c)>();
            bool[,] vis = new bool[R, C];
            var prev = new (int r, int c)[R, C];
            for (var i = 0; i < R; ++i)
                for (var j = 0; j < C; ++j)
                {
                    vis[i, j] = false;
                    prev[i, j] = (-1, -1);
                }
            q.Enqueue(cur);
            vis[cur.r,cur.c] = true;

            while (q.Count != 0)
            {
                int[] dr = { -1, 0, 1, 0 };
                int[] dc = { 0, 1, 0, -1 };
                var (r, c) = q.Dequeue();
                if ((r, c) == (R - 1, C - 1)) break;
                foreach (var dir in Enumerable.Range(0, 4))
                {
                    var (nr, nc) = (r + dr[dir], c + dc[dir]);
                    if (0 <= nr && nr < R && 0 <= nc && nc < C && buttonModels[nr, nc].Status != Status.BARREL && !vis[nr, nc])
                    {
                        vis[nr, nc] = true;
                        prev[nr, nc] = (r, c);
                        q.Enqueue((nr, nc));
                    }
                }
            }

            ShortestPath = new Queue<(int r, int c)>();
            if (!vis[R - 1, C - 1])
            {
                ShortestPath.Enqueue(cur);
                return;
            }

            var tmp = new Stack<(int, int)>();
            for (var (r, c) = (R - 1, C - 1); (r, c) != (-1, -1); (r, c) = prev[r, c])
                tmp.Push((r, c));
            while (tmp.Count > 0) ShortestPath.Enqueue(tmp.Pop());
            return;
        }

        private Mutex mutex = new Mutex();
        Action backward = null;
        public void StepIn(Queue<(int r, int c)> ShortestPath, CancellationTokenSource cts)
        {
            mutex.WaitOne();
            while (ShortestPath.Count > 0)
            {
                if (cts.IsCancellationRequested) break;
                backward?.Invoke();
                var (r, c) = cur = ShortestPath.Dequeue();
                Dispatcher.Invoke(() => buttonModels[r, c].IsDriven = true);
                backward = () => Dispatcher.Invoke(() => buttonModels[r, c].IsDriven = false);
                Task.Delay(1000).Wait();
            }
            mutex.ReleaseMutex();
        }
    }

    public class ButtonModel
    {
        public delegate void ModeChanged(object sender,EventArgs e);
        public event ModeChanged OnStatusChanged, OnDrivenChanged;
        
        private Status status = Status.CLEAR;
        public Status Status {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    OnStatusChanged?.Invoke(this,null);
                }
            }
        }
        private bool driven = false;
        public bool IsDriven {
            get => driven;
            set
            {
                if (driven != value)
                {
                    driven = value;
                    OnDrivenChanged?.Invoke(this,null);
                }
            }
        }

        public Brush Brush
        {
            get
            {
                switch (Status)
                {
                    case Status.BARREL:
                        return Brushes.Black;
                    case Status.CLEAR:
                        return Brushes.White;
                    case Status.ENTER:
                        return Brushes.Blue;
                    case Status.EXIT:
                        return Brushes.Green;
                    default:
                        return null;
                }
            }
        }
        public static implicit operator ButtonModel(Status e) => new ButtonModel { status=e };
    }
}
