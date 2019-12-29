using System;
using System.Windows;
using System.Windows.Controls;

namespace BFS_With_Maze_Demo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WindowModel model;

        public MainWindow()
        {
            InitializeComponent();
            model = new WindowModel()
            {
                Dispatcher = Dispatcher
            };
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.Content.ToString() == "Confirm")
            {
                Reset();
                btn.Content = "Run";
            }
            else if (btn.Content.ToString() == "Run")
            {
                model.Stepping = true;
                btn.Content = "Refresh";
            }
            else if (btn.Content.ToString() == "Refresh")
            {
                model.Stepping = false;
                Reset();
                btn.Content = "Run";
            }
        }

        private void Reset()
        {
            try
            {
                int r = int.Parse(RowText.Text), c = int.Parse(ColumnText.Text);

                model.Resize(r, c);
                GridResize(MainGird, r, c);
                ButtonRefill(MainGird, model, r, c);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("R or C out of range , recommend range is [2,50]", "OutOfRangeException", MessageBoxButton.OK, MessageBoxImage.Error); ;
            }
            catch (FormatException)
            {
                MessageBox.Show("Text format Error , decimal integer please", "FormatError", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ButtonRefill(Grid grid, WindowModel model, int r, int c)
        {
            for (int i = 0; i < r; ++i)
            {
                for (int j = 0; j < c; ++j)
                {
                    Button button = new BlockButton(model.buttonModels[i, j]);
                    grid.Children.Add(button);
                    button.SetValue(Grid.RowProperty, i);
                    button.SetValue(Grid.ColumnProperty, j);
                }
            }
        }

        private static void GridResize(Grid grid,int r,int c)
        {
            if (r < 0 || c < 0) throw new ArgumentOutOfRangeException();
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
            for (int i = 0; i < r; ++i) grid.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < c; ++i) grid.ColumnDefinitions.Add(new ColumnDefinition());
        }
    }

    /// <summary>
    /// 迷宫中每个块的一个自定义控件
    /// </summary>
    public class BlockButton : Button
    {
        private ButtonModel model;
        public BlockButton(ButtonModel model = null)
        {
            this.model = model ?? new ButtonModel();
            this.model.OnStatusChanged += (s, e) => Background = model.Brush;
            this.model.OnDrivenChanged += (s, e) => CircleVisibility = model.IsDriven ? Visibility.Visible : Visibility.Hidden;
            Background = model.Brush; CircleVisibility = model.IsDriven ? Visibility.Visible : Visibility.Hidden;
        }

        protected override void OnClick()
        {
            base.OnClick();
            switch (model.Status)
            {
                case Status.BARREL:
                    model.Status = Status.CLEAR;
                    break;
                case Status.CLEAR:
                    model.Status = Status.BARREL;
                    break;
            }
        }

        public readonly static DependencyProperty CircleVisibilityProperty =
            DependencyProperty.Register("CircleVisibility", typeof(Visibility), typeof(BlockButton));

        public Visibility CircleVisibility
        {
            get => (Visibility) GetValue(CircleVisibilityProperty);
            set { SetValue(CircleVisibilityProperty, value); }
        }
    }
}
