using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Events
{
    internal class SquareEventArgs : EventArgs
    {
        public Square Square { get; }

        public SquareEventArgs(Square square)
        {
            Square = square;
        }

        public delegate bool SquareEventHandler(object sender, SquareEventArgs e);
    }
}
