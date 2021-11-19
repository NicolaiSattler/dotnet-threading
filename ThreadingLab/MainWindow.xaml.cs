using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Threading;

namespace ThreadingLab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int Sum; 

        public string[] ControlValues { get; set; }
        public int[] Awnsers { get; set; }
        public CountdownEvent CountDownEvent { get; private set; }
        public Barrier Barrier { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            Awnsers = new int[3];
            ControlValues = new string[3] { TextBox1.Text, TextBox2.Text, TextBox3.Text };
        }

        //TextBoxes are named TextBox1, TextBox2, TextBox3 and ResultTextBox
        //private void CalculateButton_Click(object sender, RoutedEventArgs e)
        //{
        //    //MethodCreateThreads();
        //    //MethodBackgroundWorker();
        //    //MethodThreadPool();
        //    //MethodTaskRunsSingle(); 
        //    //MethodParallelMultiple();
        //    //MethodPLINQ();
        //    //MethodCountDownEvent();
        //    //MethodBarrier();
        //}
        private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"{nameof(CalculateButton_Click)} running on thread: {Thread.CurrentThread.ManagedThreadId}");

            //await MethodAsyncAwait();
            await MethodGetAwaiter();

            Debug.WriteLine($"{nameof(CalculateButton_Click)} running on thread: {Thread.CurrentThread.ManagedThreadId}");
        }
        private void MethodCreateThreads()
        {
            var threads = new Thread[3];
            var uiContext = SynchronizationContext.Current;

            for (int i = 0; i < 3; i++)
            {
                threads[i] = new Thread(CalculateSquare);
                threads[i].Start(i);
            }

            var t4 = new Thread(() =>
            {
                for (int i = 0; i < 3; i++)
                    threads[i].Join();

                uiContext.Post(o =>
                {
                    //change data in ui threat.
                    ResultTextBox.Text = (string)o;
                    CalculateButton.IsEnabled = true;
                }, Awnsers.Sum().ToString());
            });

            t4.Start();

            Awnsers = new int[3];
        }
        private void MethodBackgroundWorker()
        {
            var sum = 0;
            var workers = new BackgroundWorker[3];

            for (int i = 0; i < 3; i++)
            {
                var worker = new BackgroundWorker();
                worker.DoWork += (obj, arg) =>
                {
                    if (int.TryParse(arg.Argument.ToString(), out var intValue))
                    {
                        if (int.TryParse(ControlValues[intValue], out var textboxValue))
                            arg.Result = CalculateSquareFunc(textboxValue);
                    }
                };
                worker.RunWorkerCompleted += (obj, arg) =>
                {
                    sum += (int)arg.Result;
                    ResultTextBox.Text = sum.ToString();
                };
                workers[i] = worker;
            }

            for (int i = 0; i < 3; i++)
                workers[i].RunWorkerAsync(i);
        }
        private void MethodThreadPool()
        {
            for (int i = 0; i < 3; i++)
                ThreadPool.QueueUserWorkItem(CalculateSquare, i);

            ThreadPool.QueueUserWorkItem(SetResultTextBox, null);

            ClearAwnsers();
        }
        private void MethodTaskRunsSingle()
        {
            var context = SynchronizationContext.Current;

            var calculationTask = Task.Run(() => 
            {
                CalculateSquare(0);
            }).ContinueWith((r) => {
                context.Post(o =>
                {
                    ResultTextBox.Text = (string)o;
                    CalculateButton.IsEnabled = true;
                }, Awnsers.Sum().ToString());
            });


            ClearAwnsers();
        }
        private void MethodParallelMultiple()
        {
            var uiContext = SynchronizationContext.Current;
            CalculateButton.IsEnabled = false;

            Task.Run(() =>
            {
                Parallel.Invoke(() => CalculateSquare(0),
                                () => CalculateSquare(1),
                                () => CalculateSquare(2));

                uiContext.Post(o =>
                {
                    ResultTextBox.Text = (string)o;
                    CalculateButton.IsEnabled = true;
                }, Awnsers.Sum().ToString());
                ClearAwnsers();
            });
        }
        private void MethodPLINQ()
        {
            var uiContext = SynchronizationContext.Current;
            CalculateButton.IsEnabled = false;

            Task.Run(() =>
            {
                var sm = new SlowMath();
                var sum = ControlValues.ToList()
                                       .Select(c => sm.Square(Convert.ToInt32(c)))
                                       .AsParallel()
                                       .Sum();
                uiContext.Post(o => 
                { 
                    ResultTextBox.Text = (string)o; 
                    CalculateButton.IsEnabled = true; 
                }, sum);

                ClearAwnsers();
            });
        }
        private void MethodCountDownEvent()
        {
            CalculateButton.IsEnabled = false;
            CountDownEvent= new CountdownEvent(3);
        
            foreach(var item in ControlValues)
                ThreadPool.QueueUserWorkItem(CalculateWithSignal, item);
        }
        private void MethodBarrier()
        {
            CalculateButton.IsEnabled = false;
            var context = SynchronizationContext.Current;
            Barrier = new Barrier(3, (b) => {
                context.Post(d =>
                {
                    ResultTextBox.Text = Sum.ToString();
                    CalculateButton.IsEnabled = true;
                }, null);
            });

            foreach(var item in ControlValues)
                ThreadPool.QueueUserWorkItem(CalculateWithBarrier, item);
        }
        private async Task MethodAsyncAwait()
        {
            try
            {
                CalculateButton.IsEnabled = false;

                var sm = new SlowMath();
                var tasks = ControlValues.ToList().Select(m => sm.SquareAsync(Convert.ToInt32(m)));
                var result = await Task.WhenAll(tasks);

                ResultTextBox.Text = result.Sum().ToString();
            }
            finally
            {
                CalculateButton.IsEnabled = false;
            }
        }
        private async Task MethodGetAwaiter()
        {
            try
            {
                CalculateButton.IsEnabled = false;
                var sum = 0;

                await foreach (var result in GetAwaiter())
                    sum += result;

                Debug.WriteLine($"SquareSync running on thread: {Thread.CurrentThread.ManagedThreadId}");
                ResultTextBox.Text = sum.ToString();
            }
            finally
            {
                CalculateButton.IsEnabled = true;
            }
        }


        private Func<int, int> CalculateSquareFunc = obj => new SlowMath().Square(obj);
        private void ClearAwnsers()
        {
            Awnsers = new int[3];
        }
        private void CalculateSquare(object value)
        {
            if (int.TryParse(value.ToString(), out var index))
            {
                if (int.TryParse(ControlValues[index], out var textboxValue))
                    Awnsers[index] = CalculateSquareFunc(textboxValue);
            }
        }
        private void SetResultTextBox(object state)
        {
            while (Awnsers.Any(c => c == 0)) ;

            ResultTextBox.Dispatcher.Invoke(() => ResultTextBox.Text = Awnsers.Sum().ToString());
        }
        private void CalculateWithSignal(object num)
        {
            var sm = new SlowMath();

            if (int.TryParse(num.ToString(), out var number))
            {
                var result = sm.Square(number);
                Interlocked.Add(ref Sum, result);
                Barrier.SignalAndWait();
           }
        }
        private void CalculateWithBarrier(object num)
        {
            var sm = new SlowMath();

            if (int.TryParse(num.ToString(), out var number))
            {
                var result = sm.Square(number);
                Interlocked.Add(ref Sum, result);

                if (CountDownEvent.Signal())
                {
                    ResultTextBox.Dispatcher.Invoke(() => ResultTextBox.Text = Sum.ToString());
                }
            }
        }
        private async IAsyncEnumerable<int> GetAwaiter()
        {
            var sm = new SlowMath();
            var tasks = new Task<int>[3];
            var index = 0;
            foreach(var item in ControlValues)
            {
                var value = Convert.ToInt32(item);
                tasks[index] = sm.SquareAsync(value);
                index++;
            }

            for (int i = 0; i < 3; i++)
            {
                var result = await tasks[i];
                yield return result;
            }
        }
   }
}
