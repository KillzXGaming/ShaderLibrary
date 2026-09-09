using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.Sharc
{
    public interface ISharcFile
    {
        public uint GetVersion();
        public bool IsVersion2 => false;
    }
}
