using System;
using System.Windows;

namespace DrawDemo
{
    public class EdgeObj
    {
        public Point Start { get; set; }

        public Point End { get; set; }

        public Reference GetReference()
        {
            return new Reference();
        }
        public Reference GetEndPointReference(int index)
        {
            if (index == 0)
                return new Reference();
            if (index == 1)
                return new Reference();

            throw new ArgumentOutOfRangeException();
        }
    }
}
