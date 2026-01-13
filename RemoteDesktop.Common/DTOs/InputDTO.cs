using System;
using System.Collections.Generic;
using System.Text;

namespace RemoteDesktop.Common.DTOs
{
    [Serializable]
    public class InputDTO
    {
        public int Type { get; set; } // 0: Chuột, 1: Bàn phím
        public int Action { get; set; } // 0: Move, 1: LeftDown, 2: LeftUp, 3: KeyDown, v.v.
        public int X { get; set; }
        public int Y { get; set; }
        public int KeyCode { get; set; }
    }
}
