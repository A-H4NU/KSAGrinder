using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ExcelDataReader;

namespace dsgen.Excel
{
    public class ExcelBook : IDictionary<string, ExcelSheet>
    {
        private readonly Dictionary<string, ExcelSheet> _sheets;

        private ExcelBook()
        {
            _sheets = new();
        }

        private ExcelBook(int capacity)
        {
            _sheets = new(capacity);
        }

        public ExcelSheet this[string key]
        {
            get => ((IDictionary<string, ExcelSheet>)_sheets)[key];
            set => ((IDictionary<string, ExcelSheet>)_sheets)[key] = value;
        }

        public ICollection<string> Keys => ((IDictionary<string, ExcelSheet>)_sheets).Keys;

        public ICollection<ExcelSheet> Values => ((IDictionary<string, ExcelSheet>)_sheets).Values;

        ///<inheritdoc/>
        public int Count => ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).Count;

        ///<inheritdoc/>
        public bool IsReadOnly => ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).IsReadOnly;

        /// <summary>
        /// Create an <see cref="ExcelBook"/> instance from the Excel file specified in <paramref name="path"/>.
        /// </summary>
        public static ExcelBook FromFile(string path)
        {
            FileStream? fs = null;
            IExcelDataReader? reader = null;
            try
            {
                fs = File.OpenRead(path);
                reader = ExcelReaderFactory.CreateReader(fs);
                ExcelBook res = new(reader.ResultsCount);
                do
                {
                    res[reader.Name] = ExcelSheet.FromExcelDataReader(reader);
                } while (reader.NextResult());
                return res;
            }
            finally
            {
                reader?.Dispose();
                fs?.Dispose();
            }
        }

        ///<inheritdoc/>
        public void Add(string key, ExcelSheet value)
        {
            ((IDictionary<string, ExcelSheet>)_sheets).Add(key, value);
        }

        ///<inheritdoc/>
        public void Add(KeyValuePair<string, ExcelSheet> item)
        {
            ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).Add(item);
        }

        ///<inheritdoc/>
        public void Clear()
        {
            ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).Clear();
        }

        ///<inheritdoc/>
        public bool Contains(KeyValuePair<string, ExcelSheet> item)
        {
            return ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).Contains(item);
        }

        ///<inheritdoc/>
        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, ExcelSheet>)_sheets).ContainsKey(key);
        }

        ///<inheritdoc/>
        public void CopyTo(KeyValuePair<string, ExcelSheet>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).CopyTo(array, arrayIndex);
        }

        ///<inheritdoc/>
        public IEnumerator<KeyValuePair<string, ExcelSheet>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ExcelSheet>>)_sheets).GetEnumerator();
        }

        ///<inheritdoc/>
        public bool Remove(string key)
        {
            return ((IDictionary<string, ExcelSheet>)_sheets).Remove(key);
        }

        ///<inheritdoc/>
        public bool Remove(KeyValuePair<string, ExcelSheet> item)
        {
            return ((ICollection<KeyValuePair<string, ExcelSheet>>)_sheets).Remove(item);
        }

        ///<inheritdoc/>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out ExcelSheet value)
        {
            return ((IDictionary<string, ExcelSheet>)_sheets).TryGetValue(key, out value);
        }

        ///<inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_sheets).GetEnumerator();
        }
    }
}
