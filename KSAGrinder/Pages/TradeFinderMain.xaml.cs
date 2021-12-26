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
                else if (MaxDepth >= 4)
                    return "4 이상으로 탐색 깊이를 설정하면\n탐색 시간이 극도로 길어질 수 있습니다!";
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
                    res += $"{total - selected}개 강의는 탐색 및 유효성 검사에서 무시됩니다.";
                else
                    res += $"완벽히 유효한 트레이드만 탐색합니다.";
                return res;
            }
        }

        private readonly ICommand _showDetail = new DelegateCommand<ReadOnlyCollection<ClassMove>>(ShowDetail);
        //private readonly ICommand _showDetail = new DelegateCommand<object>(TestMethod);
        public ICommand ShowDetailCommand => _showDetail;

        private readonly ObservableCollection<ReadOnlyCollection<ClassMove>> _tradeList = new ObservableCollection<ReadOnlyCollection<ClassMove>>();
        public ObservableCollection<ReadOnlyCollection<ClassMove>> TradeList => _tradeList;

        // lecture code => bool
        public readonly Dictionary<string, bool> LecturesToMove = new Dictionary<string, bool>();

        public TradeFinderMain(TradeFinder main, string studentId, Schedule schedule)
        {
            InitializeComponent();
            _main = main;
            StudentId = studentId;
            _schedule = schedule;
            List<Class> originalSchedule = DataManager.GetScheduleFromStudentID(StudentId).ToList();
            foreach (Class targetClass in _schedule)
            {
                int index = originalSchedule.FindIndex(cls => cls.Code == targetClass.Code);
                if (index < 0 || originalSchedule[index].Number == targetClass.Number) continue;
                LecturesToMove.Add(targetClass.Code, true);
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
                    (string lectureCode, bool willMove) = (pair.Key, pair.Value);
                    if (!willMove) continue;
                    tradeCapture.Add(new ClassMove(
                        StudentId,
                        lectureCode,
                        originalSchedule.Find(cls => cls.Code == lectureCode).Number,
                        targetSchedule.Find(cls => cls.Code == lectureCode).Number));
                }
                GenerateClassMoves(new[] { (StudentId, new Schedule(tradeCapture.GetScheduleOf(StudentId))) }, tradeCapture, 0, MaxDepth, ProcessResult);
                if (_cts == null || !_cts.IsCancellationRequested)
                {
                    MessageBox.Show("탐색이 종료되었습니다.");
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
            Components.TradeCapture tradeCapture,
            int depth,
            int maxDepth,
            ProcessResultDelegate processResult,
            int batchSize = 1024,
            int numThreads = 4)
        {
            if (tradeCapture.DoesFormTrade() &&
                targets.All(tuple => tuple.Schedule.Equals(tradeCapture.GetScheduleOf(tuple.StudentId))) &&
                tradeCapture.AreAllSchedulesValid(LecturesToMove.Values.All(b => b) ? null : StudentId))
            {
                bool okay = true;
                foreach ((var studentId, var schedule) in tradeCapture.GetCapturedSchedules())
                {
                    IEnumerable<Class> toCheckValid = schedule;
                    if (studentId == StudentId)
                        toCheckValid = toCheckValid.Where(cls => !LecturesToMove.ContainsKey(cls.Code) || LecturesToMove[cls.Code]);
                    if (!Schedule.CheckValid(toCheckValid))
                    {
                        okay = false;
                        break;
                    }
                }
                if (okay)
                {
                    processResult(tradeCapture);
                    return;
                }
            }

            if (_cts.IsCancellationRequested) return;

            if (depth >= maxDepth) return;

            List<List<(ClassMove, Schedule)>> sequences = new List<List<(ClassMove, Schedule)>>();
            long card = 1;
            foreach ((ClassMove head, ClassMove tail) in tradeCapture.HeadTailTuplesOfNoncycles())
            //foreach (ClassMove tail in tradeCapture.TailsOfNonCycles())
            {
                if (_cts.IsCancellationRequested) return;

                // have to make a new class move from tailMove.NumberTo
                int numberOfClasses = DataManager.NumberOfClasses(tail.LectureCode);
                // Class numbers already targeted (move.ToNumber of some move of tailMove.LectureCode)
                IEnumerable<int> numbersInvolved = from move in tradeCapture where move.LectureCode == tail.LectureCode select move.NumberTo;
                IEnumerable<string> studentsInvolved = tradeCapture.InvolvedStudents();
                List<(ClassMove, Schedule)> currentList = new List<(ClassMove, Schedule)>();

                // For each class numbers of tailMove.LectureCode but not involved
                //foreach (int numberTo in RangeWithPreference(numberOfClasses, head.NumberFrom).Except(numbersInvolved))
                foreach (int numberTo in Enumerable.Range(1, numberOfClasses).Except(numbersInvolved))
                {
                    // For each students in (tailMove.LectureCode, tailMove.NumberTo) but not in targets
                    foreach (string studentId in tradeCapture.GetEnrollListOf(tail.LectureCode, tail.NumberTo).Except(studentsInvolved))
                    {
                        Schedule schedule = new Schedule(tradeCapture.GetScheduleOf(studentId));
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
                Components.TradeCapture localTradeCapture = tradeCapture.Clone();
                int originalNumMoves = localTradeCapture.Count;
                List<IEnumerable<ClassMove>> result = new List<IEnumerable<ClassMove>>();
                foreach (IEnumerable<(ClassMove, Schedule)> optionsToTry in batch)
                {
                    bool good = true;
                    List<(string, Schedule)> targetsToAdd = new List<(string, Schedule)>();
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
            Task[] tasks = new Task[numThreads];
            foreach (IEnumerable<IEnumerable<(ClassMove, Schedule)>> batch in sequences.CartesianProduct().Batch(batchSize))
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
                char eunneun = new KoreanChar(name.Last()).GetJongSung() == '\0' ? '는' : '은';
                sb.AppendLine($"{group.Key} {name}{eunneun}");
                foreach (ClassMove move in group)
                {
                    sb.AppendLine($"\t{DataManager.NameOfLectureFromCode(move.LectureCode)} {move.NumberFrom}분반에서 {move.NumberTo}분반으로");
                }
                instructionsForGroup.Add(sb.ToString().Replace("\0", ""));
            }
            DetailView detailWindow = new DetailView(String.Join(Environment.NewLine, instructionsForGroup));
            detailWindow.ShowDialog();
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
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            _main.Main.Navigate(_classSelection);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
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
