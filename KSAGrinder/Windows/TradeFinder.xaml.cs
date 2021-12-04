using KSAGrinder.Components;
using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace KSAGrinder.Windows
{
    /// <summary>
    /// TradeFinder.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TradeFinder : Window
    {
        public delegate void ProcessResultDelegate(in IEnumerable<ClassMove> result);

        private readonly string _studentId;
        private readonly Schedule _schedule;

        private CancellationTokenSource _cts;
        private Thread _thread;

        public int MaxDepth = 2;

        public int MaxLectureMoves = 1;

        public TradeFinder(string studentId, Schedule schedule)
        {
            InitializeComponent();
            _studentId = studentId;
            _schedule = schedule;
        }

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            void ThreadFunc()
            {
                var tradeCapture = new TradeCapture();
                var originalSchedule = DataManager.GetScheduleFromStudentID(_studentId).ToList();
                foreach (Class targetClass in _schedule)
                {
                    int index = originalSchedule.FindIndex(cls => cls.Code == targetClass.Code);
                    if (index < 0 || originalSchedule[index].Number == targetClass.Number) continue;
                    tradeCapture.Add(new ClassMove(_studentId, targetClass.Code, originalSchedule[index].Number, targetClass.Number));
                }
                GenerateClassMoves(new[] { (_studentId, _schedule) }, tradeCapture, 0, MaxDepth, ProcessResult);
                if (_cts == null || !_cts.IsCancellationRequested)
                    MessageBox.Show("Finished");
            }

            if (_thread != null && _thread.IsAlive)
                MessageBox.Show("이미 작업이 진행 중입니다.");

            _cts = new CancellationTokenSource();
            
            _thread = new Thread(ThreadFunc);
            _thread.IsBackground = true;
            _thread.Start();

            TxtTest.Text = "Start\n";
        }

        private readonly object _lock = new object();
        private void ProcessResult(in IEnumerable<ClassMove> moves)
        {
            if (moves == null) return;
            var sb = new StringBuilder();
            foreach (ClassMove move in moves)
                sb.AppendLine(move.ToString());
            sb.Append('\n');
            lock (_lock)
            {
                Dispatcher.Invoke(() => TxtTest.Text += sb.ToString());
                //MessageBox.Show(sb.ToString());
            }
        }

        private void GenerateClassMoves(
            IEnumerable<(string StudentId, Schedule Schedule)> targets,
            TradeCapture tradeCapture,
            int depth,
            int maxDepth,
            ProcessResultDelegate processResult,
            int batchSize = 1024,
            int numThreads = 4)
        {
            if (tradeCapture.DoesFormTrade() &&
                targets.All(tuple => tuple.Schedule.Equals(tradeCapture.GetScheduleOf(tuple.StudentId))) &&
                tradeCapture.AreAllSchedulesValid())
            {
                processResult(tradeCapture);
                return;
            }

            if (_cts.IsCancellationRequested) return;

            if (depth >= maxDepth) return;

            var sequences = new List<List<(ClassMove, Schedule)>>();
            long card = 1;
            foreach ((ClassMove head, ClassMove tail) in tradeCapture.HeadTailTuplesOfNoncycles())
            //foreach (ClassMove tail in tradeCapture.TailsOfNonCycles())
            {
                if (_cts.IsCancellationRequested) return;

                // have to make a new class move from tailMove.NumberTo
                int numberOfClasses = DataManager.NumberOfClasses(tail.LectureCode);
                // Class numbers already targeted (move.ToNumber of some move of tailMove.LectureCode)
                IEnumerable<int> numbersInvolved = from move in tradeCapture where move.LectureCode == tail.LectureCode select move.NumberTo;
                IEnumerable<string> studentsInvolved = tradeCapture.StudentsInvolved();
                var currentList = new List<(ClassMove, Schedule)>();

                // For each class numbers of tailMove.LectureCode but not involved
                //foreach (int numberTo in RangeWithPreference(numberOfClasses, head.NumberFrom).Except(numbersInvolved))
                foreach (int numberTo in Enumerable.Range(1, numberOfClasses).Except(numbersInvolved))
                {
                    // For each students in (tailMove.LectureCode, tailMove.NumberTo) but not in targets
                    foreach (string studentId in tradeCapture.GetEnrollListOf(tail.LectureCode, tail.NumberTo).Except(studentsInvolved))
                    {
                        var schedule = new Schedule(tradeCapture.GetScheduleOf(studentId));
                        schedule.MoveClass(tail.LectureCode, numberTo);
                        IEnumerable<Schedule> options = schedule.Combination(tradeCapture.InvolvedLecturesOf(studentId).Append(tail.LectureCode), MaxLectureMoves, false);
                        foreach (Schedule option in options)
                        {
                            (ClassMove, Schedule) toAdd = (new ClassMove(studentId, tail.LectureCode, tail.NumberTo, numberTo), option);
                            if (option.IsValid) currentList.Insert(0, toAdd);
                            else currentList.Add(toAdd);
                        }
                    }
                }
                if (currentList.Count == 0) return;
                sequences.Add(currentList);
                card *= currentList.Count;
            }
            //MessageBox.Show(card.ToString());

            if (_cts.IsCancellationRequested) return;

            void ProcessBatch(IEnumerable<IEnumerable<(ClassMove, Schedule)>> batch)
            {
                var localTradeCapture = tradeCapture.Clone();
                int originalNumMoves = localTradeCapture.Count;
                var result = new List<IEnumerable<ClassMove>>();
                foreach (var optionsToTry in batch)
                {
                    bool good = true;
                    var targetsToAdd = new List<(string, Schedule)>();
                    foreach ((ClassMove move, Schedule option) in optionsToTry)
                    {
                        try
                        {
                            localTradeCapture.Add(move);
                        }
                        catch (TradeInvalidException)
                        {
                            Debug.WriteLine("Bad move.");
                            good = false; break;
                        }
                        targetsToAdd.Add((move.StudentId, option));
                    }
                    if (!good) continue;
                    //Debug.Assert(!targetsToAdd.Intersect(targets).Any());
                    //await foreach (IEnumerable<ClassMove> moves in GenerateClassMoves(targets.Concat(targetsToAdd), localTradeCapture, depth + 1, maxDepth))
                    //{
                    //    result.Add(moves);
                    //}
                    int dummy = localTradeCapture.Count;
                    GenerateClassMoves(targets.Concat(targetsToAdd), localTradeCapture, depth + 1, maxDepth, processResult);
                    Debug.Assert(dummy == localTradeCapture.Count);
                    localTradeCapture.Pop(localTradeCapture.Count - originalNumMoves);
                }
            }

            numThreads = (int)Math.Min(Math.Ceiling((double)card / batchSize), numThreads);
            var tasks = new Task[numThreads];
            foreach (var batch in sequences.CartesianProduct().Batch(batchSize))
            {
                // Break so that the left tasks to be executed.
                if (_cts.IsCancellationRequested)
                    break;
                if (tasks.All(task => task != null))
                {
                    int index = Task.WaitAny(tasks);
                    //foreach (var moves in await tasks[index])
                    //    processResult(moves);
                    tasks[index] = new Task(() => ProcessBatch(batch));
                    tasks[index].Start();
                }
                else
                {
                    int index = 0;
                    while (tasks[index] != null) ++index;
                    tasks[index] = new Task(() => ProcessBatch(batch));
                    tasks[index].Start();
                }
            }

            Task.WaitAll(tasks);
            //try
            //{
            //    Task.WaitAll(tasks);
            //}
            //catch (Exception)
            //{
            //    MessageBox.Show("탐색 도중 오류가 발생해 중단했습니다. 모든 경우를 탐색해지 못했을 수 있습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_thread != null && _thread.IsAlive)
            {
                _cts.Cancel();
                _thread.Join(1000);
            }
            _cts?.Dispose();
        }

        /// <summary>
        ///     ex) RangeWithPreference(6,4) = { 4, 1, 2, 3, 5, 6 }
        /// </summary>
        /// <param name="n">A positive integer</param>
        /// <param name="pref">A positive integer from 1 to n</param>
        public static IEnumerable<int> RangeWithPreference(int n, int pref)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (pref <= 0 || pref > n) throw new ArgumentOutOfRangeException(nameof(pref));
            yield return pref;
            for (int i = 1; i <= n; ++i)
            {
                if (i == pref) continue;
                yield return i;
            }
        }
    }
}
