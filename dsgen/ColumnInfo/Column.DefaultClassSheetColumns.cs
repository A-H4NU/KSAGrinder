using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace dsgen.ColumnInfo;

public readonly partial struct Column
{
    /// <summary>
    /// This class provides hard-coded information of default class sheet columns.
    /// </summary>
    private static class DefaultClassSheetColumns
    {
        private static CultureInfo Culture_koKR = CultureInfo.GetCultureInfo("ko-KR");
        private static CultureInfo Culture_en = CultureInfo.GetCultureInfo("en");

        private static ReadOnlyCollection<Column>? _cachedDefaultColumns = null;
        public static ReadOnlyCollection<Column> DefaultColumns
        {
            get
            {
                if (_cachedDefaultColumns is null)
                {
                    FieldInfo[] fields = typeof(DefaultClassSheetColumns).GetFields(
                        BindingFlags.Public | BindingFlags.Static
                    );
                    ReadOnlyCollectionBuilder<Column> builder = new(fields.Length);
                    foreach (FieldInfo field in fields)
                    {
                        object? obj = field.GetValue(null);
                        if (obj is Column column)
                            builder.Add(column);
                    }
                    _cachedDefaultColumns = builder.ToReadOnlyCollection();
                }
                return _cachedDefaultColumns;
            }
        }

        public static readonly Column Code = new Column()
        {
            IsKey = true,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "교과목코드" },
                { Culture_en, "Course Code" },
            }.AsReadOnly(),
            ColumnName = "Code",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String),
            IsLocalizable = false,
        };

        public static readonly Column Department = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "교과군" },
                { Culture_en, "Curriculum Division" },
            }.AsReadOnly(),
            ColumnName = "Department",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String),
            IsLocalizable = false,
        };
        
        public static readonly Column Name = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "교과목명" },
                { Culture_en, "Course Title" },
            }.AsReadOnly(),
            ColumnName = "Name",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String),
            IsLocalizable = true,
        };

        public static readonly Column Grade = new Column()
        {
            IsKey = true,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "학년" },
                { Culture_en, "Grade" },
            }.AsReadOnly(),
            ColumnName = "Grade",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32),
            IsLocalizable = false,
        };

        public static readonly Column Class = new Column()
        {
            IsKey = true,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "분반" },
                { Culture_en, "Class No" },
            }.AsReadOnly(),
            ColumnName = "Class",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32),
            IsLocalizable = false,
        };

        public static readonly Column Teacher = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "담당교원" },
                { Culture_en, "Teacher" },
            }.AsReadOnly(),
            ColumnName = "Teacher",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String),
            IsLocalizable = false,
        };

        public static readonly Column Time = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "요일" },
                { Culture_en, "Time" },
            }.AsReadOnly(),
            ColumnName = "Time",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof((DayOfWeek day, int hour)[]),
            IsLocalizable = false,
        };

        public static readonly Column Enrollment = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "신청수" },
                { Culture_en, "No. of Students" },
            }.AsReadOnly(),
            ColumnName = "Enrollment",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32),
            IsLocalizable = false,
        };

        public static readonly Column Credit = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "학점" },
                { Culture_en, "Credit" },
            }.AsReadOnly(),
            ColumnName = "Credit",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32),
            IsLocalizable = false,
        };

        public static readonly Column Note = new Column()
        {
            IsKey = false,
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "비고" },
                { Culture_en, "Note" }
            }.AsReadOnly(),
            ColumnName = "Note",
            Types = new Type?[] { typeof(String), null }.AsReadOnly(),
            DataTableType = typeof(String),
            IsLocalizable = false,
        };
    }
}
