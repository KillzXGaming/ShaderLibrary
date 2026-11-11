using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.Sharc
{
    public interface ISharcFile
    {
        IEnumerable<ISharcProgram> GetPrograms();
        uint GetVersion();
        public bool IsVersion2 => false;
        void Save(Stream stream);
    }

    public interface ISharcProgram
    {
        string Name { get; set; }
        IEnumerable<SharcFile.VariationMacro> GetMacros();
        int GetVariationIndex(Dictionary<string, string> options);
        int GetBinaryIndex(int variation);
        IEnumerable<Dictionary<string, string>> GetAllVariationCombinations();
        string ToXml();
    }
}
