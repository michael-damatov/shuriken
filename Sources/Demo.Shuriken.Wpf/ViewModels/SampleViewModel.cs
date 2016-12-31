using Shuriken;

namespace Demo.Shuriken.Wpf.ViewModels
{
    public sealed class SampleViewModel : ObservableObject
    {
        public SampleViewModel(int data)
        {
            Data = data;
        }

        [Observable]
        public int Data { get; set; }

        [Observable]
        public int Data2 => Data * 2;

        [Observable]
        public int Data3 => Data * 3;

        [Observable]
        public int Data4 => Data * 4;

        [Observable]
        public int Data5 => Data * 5;

        [Observable]
        public int Data6 => Data * 6;

        [Observable]
        public int Data7 => Data * 7;

        [Observable]
        public int Data8 => Data * 8;

        [Observable]
        public int Data9 => Data * 9;
    }
}