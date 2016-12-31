using System.ComponentModel;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class SampleViewModelRegular : INotifyPropertyChanged
    {
        int data;

        public SampleViewModelRegular(int data)
        {
            this.data = data;
        }

        public int Data
        {
            get
            {
                return data;
            }
            set
            {
                if (data != value)
                {
                    data = value;

                    OnPropertyChanged(new PropertyChangedEventArgs("Data"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data2"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data3"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data4"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data5"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data6"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data7"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data8"));
                    OnPropertyChanged(new PropertyChangedEventArgs("Data9"));
                }
            }
        }

        public int Data2 => data * 2;

        public int Data3 => data * 3;

        public int Data4 => data * 4;

        public int Data5 => data * 5;

        public int Data6 => data * 6;

        public int Data7 => data * 7;

        public int Data8 => data * 8;

        public int Data9 => data * 9;

        void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}