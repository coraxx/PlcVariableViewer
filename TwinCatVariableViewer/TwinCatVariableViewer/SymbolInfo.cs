using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Class for ListView item
    /// </summary>
    public class SymbolInfo : INotifyPropertyChanged
    {
        private string _path;
        public string Path
        {
            get { return _path; }
            set
            {
                if (value == _path) return;
                _path = value;
                OnPropertyChanged();
            }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (value == _type) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        private int _size;
        public int Size
        {
            get { return _size; }
            set
            {
                if (value == _size) return;
                _size = value;
                OnPropertyChanged();
            }
        }

        private long _indexGroup;
        public long IndexGroup
        {
            get { return _indexGroup; }
            set
            {
                if (value == _indexGroup) return;
                _indexGroup = value;
                OnPropertyChanged();
            }
        }

        private long _indexOffset;
        public long IndexOffset
        {
            get { return _indexOffset; }
            set
            {
                if (value == _indexOffset) return;
                _indexOffset = value;
                OnPropertyChanged();
            }
        }

        private bool _isStatic;
        public bool IsStatic
        {
            get { return _isStatic; }
            set
            {
                if (value == _isStatic) return;
                _isStatic = value;
                OnPropertyChanged();
            }
        }

        private string _currentValue;
        public string CurrentValue
        {
            get { return _currentValue; }
            set
            {
                if (value == _currentValue) return;
                _currentValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
