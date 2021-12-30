using KoreanText;

using KSAGrinder.Components;
using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Statics;
using KSAGrinder.Windows;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace KSAGrinder.Pages
{
    /// <summary>
    /// Interaction logic for TradeFinderMain.xaml
    /// </summary>
    public partial class TradeFinderMain : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public delegate void ProcessResultDelegate(Components.TradeCapture result);

        private readonly TradeFinder _main;

        public readonly string StudentId;
        private readonly Schedule _schedule;

        private CancellationTokenSource _cts;
        private Thread _thread;

        private readonly ClassSelection _classSelection;

        public int MaxDepth = 2;

        public int MaxLectureMoves = 1;

        public string WarningMessage
        {
            get
            {
                if (MaxLectureMoves >= 3)
                    return "4 이상으로 탐색 넓이를 설정하면\n탐색 시간이 극도로 길어질 수 있습니다!";
                else if (MaxDepth >= 3)
                    return "3 이상으로 탐색 깊이를 설정하면\n탐색 시간이 극도로 길어질 수 있습니다!";
                return "탐색하는 동안\n1) CPU를 크게 점유해 전력 소모가 증가하고\n"
                    + "2) 메모리(RAM)를 일부 점유하며 따라서\n"
                    + "3) 다른 프로그램의 성능이 저하될 수 있습니다.";
            }
        }

        public string SelectionMessage
        {
            get
            {
                int selected = LecturesToMove.Where(pair => pair.Value).Count();
                int total = LecturesToMove.Count;
                var res = $"총 {selected}/{total}개 강의 선택됨{Environment.NewLine}";
                if (selected < total)
                    res += $"{total - selected}개 강의는 탐색에서 무시됩니다.";
                else
                    res += $"모든 강의를 탐색에 고려합니다.";
                return res;
            }
        }

        private readonly ICommand _showDetail = new DelegateCommand<ReadOnlyCollection<ClassMove>>(ShowDetail);

        public ICommand ShowDetailCommand => _showDetail;

        private readonly ObservableCollection<ReadOnlyCollection<ClassMove>> _tradeList = new ObservableCollection<ReadOnlyCollection<ClassMove>>();
        public ObservableCollection<ReadOnlyCollection<ClassMove>> TradeList => _tradeList;

        // (code, grade) => bool
        public readonly Dictionary<(string, int), bool> LecturesToMove = new Dictionary<(string, int), bool>();

        public TradeFinderMain(TradeFinder main, string studentId, Schedule schedule)
        {
            InitializeComponent();
            _main = main;
            StudentId = studentId;
            _schedule = schedule;
            List<Class> originalSchedule = DataManager.GetScheduleFromStudentID(StudentId).ToList();
            foreach (Class targetClass in _schedule)
            {
                int index = originalSchedule.FindIndex(cls => cls.Code == targetClass.Code && cls.Grade == targetClass.Grade);
                if (index < 0 || originalSchedule[index].Number == targetClass.Number) continue;
                LecturesToMove[(targetClass.Code, targetClass.Grade)] = true;
            }
            _classSelection = new ClassSelection(_main, this);
            UpdateSelectionMessage();
        }

        public void UpdateSelectionMessage() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionMessage)));

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            void ThreadFunc()
            {
                Components.TradeCapture tradeCapture = new Components.TradeCapture();
                List<Class> originalSchedule = DataManager.GetScheduleFromStudentID(StudentId).ToList();
                List<Class> targetSchedule = _schedule.ToList();
                foreach (var pair in LecturesToMove)
                {
                    ((string code, int grade), bool willMove) = (pair.Key, pair.Value);
                    if (!willMove) continue;
                    tradeCapture.Add(new ClassMove(
                        StudentId,
                        code,
                        grade,
                        originalSchedule.Find(cls => cls.Code == code && cls.Grade == grade).Number,
                        targetSchedule.Find(cls => cls.Code == code && cls.Grade == grade).Number));
                }
                GenerateClassMoves(new[] { (StudentId, new Schedule(tradeCapture.GetScheduleOf(StudentId))) }, tradeCapture, 0, MaxDepth, ProcessResult);
                if (_cts == null || !_cts.IsCancellationRequested)
                {
                    MessageBox.Show("탐색이 종료되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    Dispatcher.Invoke(() => SetComponentStatus(working: false));
                }
            }

            if (_thread != null && _thread.IsAlive)
                return;

            SetComponentStatus(working: true);
            _tradeList.Clear();

            _cts = new CancellationTokenSource();

            _thread = new Thread(ThreadFunc)
            {
                IsBackground = true
            };
            _thread.Start();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _thread.Join(5000);

            SetComponentStatus(working: false);
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            _main.Main.Navigate(_classSelection);
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            int GetNStudentsInvolved(IEnumerable<ClassMove> moves)
                => moves.Select(move => move.StudentId).ToHashSet().Count();

            var sorted = TradeList.ToList();
            sorted.Sort((a, b) => GetNStudentsInvolved(a) - GetNStudentsInvolved(b));
            TradeList.Clear();
            foreach (var move in sorted)
                TradeList.Add(move);
            BtnSort.IsEnabled = false;
        }

        private readonly object _processResultLock = new object();
        private void ProcessResult(TradeCapture moves)
        {
            lock (_processResultLock)
            {
                Dispatcher.Invoke(() =>
                {
                    var list = moves.ToList();
                    if (list.Count > 0)
                        _tradeList.Add(new ReadOnlyCollection<ClassMove>(list));
                });
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
                targets.All(tuple => tuple.Schedule.Equals(tradeCapture.GetScheduleOf(tuple.StudentId)))) //&&
                //tradeCapture.AreAllSchedulesValid(LecturesToMove.Values.All(b => b) ? null : StudentId))
            {
                bool okay = true;
                //foreach ((var studentId, var schedule) in tradeCapture.GetCapturedSchedules())
                //{
                //    IEnumerable<Class> toCheckValid = schedule;
                //    if (studentId == StudentId)
                //        toCheckValid = toCheckValid.Where(cls
                //            => !LecturesToMove.ContainsKey((cls.Code, cls.Grade)) || LecturesToMove[(cls.Code, cls.Grade)]);
                //    if (!Schedule.CheckValid(toCheckValid))
                //    {
                //        okay = false;
                //        break;
                //    }
                //}
                if (okay)
                {
                    processResult(tradeCapture);
                    return;
                }
            }

            if (_cts.IsCancellationRequested) return;

            if (depth >= maxDepth) return;

            var sequences = new List<List<(IEnumerable<ClassMove>, Schedule)>>();
            long card = 1;
            foreach ((ClassMove head, ClassMove tail) in tradeCapture.HeadTailTuplesOfNoncycles())
            //foreach (ClassMove tail in tradeCapture.TailsOfNonCycles())
            {
                if (_cts.IsCancellationRequested) return;

                // have to make a new class move from tailMove.NumberTo
                int numberOfClasses = DataManager.GetTheNumberOfClasses(tail.Code, tail.Grade);
                // Class numbers already targeted (move.ToNumber of some move of tailMove.LectureCode)
                IEnumerable<int> numbersInvolved = from move in tradeCapture
                                                   where (move.Code, move.Grade) == (tail.Code, tail.Grade)
                                                   select move.NumberTo;
                IEnumerable<string> studentsInvolved = tradeCapture.InvolvedStudents();
                var currentList = new List<(IEnumerable<ClassMove>, Schedule)>();

                // For each class numbers of tailMove.LectureCode but not involved
                foreach (int numberTo in RangeWithPreference(numberOfClasses, head.NumberFrom).Except(numbersInvolved))
                //foreach (int numberTo in Enumerable.Range(1, numberOfClasses).Except(numbersInvolved))
                {

                    // For each students in (tailMove.LectureCode, tailMove.NumberTo) but not in targets
                    foreach (string studentId in tradeCapture.GetEnrollListOf(tail.Code, tail.Grade, tail.NumberTo).Except(studentsInvolved))
                    {
                        if (_cts.IsCancellationRequested) return;
                        Schedule schedule = new Schedule(tradeCapture.GetScheduleOf(studentId));
                        schedule.MoveClass(tail.Code, tail.Grade, numberTo);
                        var thisMove = new ClassMove(studentId, tail.Code, tail.Grade, tail.NumberTo, numberTo);
                        IEnumerable<Schedule> options = schedule.Combination(
                            tradeCapture.InvolvedLecturesOf(studentId)
                                        .Append((tail.Code, tail.Grade)),
                            MaxLectureMoves,
                            true);
                        foreach (Schedule option in options)
                        {
                            (IEnumerable<ClassMove>, Schedule) toAdd = (Schedule.Difference(studentId, schedule, option).Append(thisMove), option);
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

            void ProcessBatch(IEnumerable<IEnumerable<(IEnumerable<ClassMove>, Schedule)>> batch)
            {
                //int originalNumMoves = localTradeCapture.Count;
                //List<IEnumerable<ClassMove>> result = new List<IEnumerable<ClassMove>>();
                foreach (var optionToTry in batch)
                {
                    var validOptionToTry = MakeValid(optionToTry);
                    if (validOptionToTry == null) continue;

                    TradeCapture localTradeCapture = tradeCapture.Clone();
                    bool good = true;
                    List<(string, Schedule)> targetsToAdd = new List<(string, Schedule)>();
                    foreach ((IEnumerable<ClassMove> moves, Schedule option) in validOptionToTry)
                    {
                        try
                        {
                            foreach (var move in moves)
                                localTradeCapture.Add(move);
                        }
                        catch (TradeInvalidException)
                        {
                            Debug.WriteLine("Bad move.");
                            good = false; break;
                        }
                        targetsToAdd.Add((moves.First().StudentId, option));
                    }
                    if (!good)
                    {
                        continue;
                    }
                    int dummy = localTradeCapture.Count;
                    GenerateClassMoves(targets.Concat(targetsToAdd), localTradeCapture, depth + 1, maxDepth, processResult);
                    Debug.Assert(dummy == localTradeCapture.Count);
                    //localTradeCapture.Pop(localTradeCapture.Count - originalNumMoves);
                }
            }

            IEnumerable<(IEnumerable<ClassMove>, Schedule)> MakeValid(IEnumerable<(IEnumerable<ClassMove> Moves, Schedule Schedule)> option)
            {
                var res = new List<(IEnumerable<ClassMove>, Schedule)>();
                var optionArr = option.ToArray();

                for (int i = 0; i < optionArr.Length; i++)
                {
                    bool overlapping = false;
                    for (int j = 0; j < i; j++)
                    {
                        var std1 = optionArr[i].Moves.First().StudentId;
                        var std2 = optionArr[j].Moves.First().StudentId;
                        if (std1 != std2) continue;
                        var moves1 = optionArr[i].Moves.ToHashSet();
                        var moves2 = optionArr[j].Moves.ToHashSet();
                        if (moves1.SetEquals(moves2))
                        {
                            overlapping = true;
                            break;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    if (!overlapping)
                        res.Add(optionArr[i]);
                }
                return res;
            }

            numThreads = (int)Math.Min(Math.Ceiling((double)card / batchSize), numThreads);
            Task[] tasks = new Task[numThreads];
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

            Task.WaitAll(tasks.Where(t => t != null).ToArray());
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

        private void CmbWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selected = CmbWidth.SelectedItem as ComboBoxItem;
            MaxLectureMoves = Int32.Parse(selected.Content.ToString()) - 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WarningMessage)));
        }

        private void CmbDepth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selected = CmbDepth.SelectedItem as ComboBoxItem;
            MaxDepth = Int32.Parse(selected.Content.ToString());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WarningMessage)));
        }

        private static void ShowDetail(ReadOnlyCollection<ClassMove> tradeCapture)
        {
            List<string> instructionsForGroup = new List<string>();
            foreach (IGrouping<string, ClassMove> group in tradeCapture.GroupBy(move => move.StudentId))
            {
                StringBuilder sb = new StringBuilder();
                string name = DataManager.GetNameFromStudentID(group.Key);
                sb.AppendLine($"{group.Key} {name}");
                foreach (ClassMove move in group)
                {
                    sb.AppendLine($"\t{DataManager.GetNameOfLectureFromCode(move.Code)} {move.NumberFrom}분반에서 {move.NumberTo}분반으로");
                }
                instructionsForGroup.Add(sb.ToString().Replace("\0", ""));
            }
            DetailView detailWindow = new DetailView(String.Join(Environment.NewLine, instructionsForGroup));
            detailWindow.Show();
        }

        private readonly object _setComponentStatusLock = new object();
        private void SetComponentStatus(bool working)
        {
            lock (_setComponentStatusLock)
            {
                CmbDepth.IsEnabled = !working;
                CmbWidth.IsEnabled = !working;
                BtnFind.IsEnabled = !working;
                BtnStop.IsEnabled = working;
                BtnSelect.IsEnabled = !working;
                BtnSort.IsEnabled = !working;
            }
        }

        public void StopWorking()
        {
            if (_thread != null && _thread.IsAlive)
            {
                _cts.Cancel();
                _thread.Join(1000);
            }
            _cts?.Dispose();
        }
    }
}
