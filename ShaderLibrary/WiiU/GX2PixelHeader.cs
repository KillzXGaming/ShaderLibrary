using System;
using System.Collections.Generic;
using System.IO;

namespace BfshaLibrary.WiiU
{
    public class GX2PixelHeader 
    {
        public byte[] Data { get; set; }
        public uint[] Regs { get; set; }
        public uint Mode { get; set; }
    }
}
