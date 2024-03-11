using ShaderLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.Common
{
    public class StringTable
    {
        private Dictionary<string, StringEntry> _savedStrings = new Dictionary<string, StringEntry>();

        private long _ofsStringTable;

        internal string fileName;

        public void SaveHeaderOffset(BinaryWriter writer)
        {
            _ofsStringTable = writer.BaseStream.Position;
            writer.Write(0); //offset
            writer.Write(0); //size
        }

        public void AddEntry(long ofs, string str)
        {
            if (_savedStrings.ContainsKey(str))
                _savedStrings[str].Positions.Add(ofs);
            else
            {
                _savedStrings.Add(str, new StringEntry()
                {
                    Positions = new List<long>() { ofs },
                });
            }
        }

        public void Write(BinaryDataWriter writer)
        {
            writer.AlignBytes(8);

            // Sort the strings ordinally.
            SortedList<string, StringEntry> sorted = new SortedList<string, StringEntry>();
            foreach (KeyValuePair<string, StringEntry> entry in _savedStrings)
                sorted.Add(entry.Key, entry.Value);

            long start_pos = writer.BaseStream.Position;

            writer.WriteSignature("_STR");
            writer.Write(0);
            writer.Write(0UL);

            foreach (KeyValuePair<string, StringEntry> entry in sorted)
            {
                var pos = writer.BaseStream.Position;

                foreach (var p in entry.Value.Positions)
                {
                    using (writer.BaseStream.TemporarySeek(p, SeekOrigin.Begin))
                    {
                        if (entry.Key == fileName)
                            writer.Write((uint)pos + 2); //filename points after length
                        else 
                            writer.Write((uint)pos);
                    }
                }

                // Align and satisfy offsets.
                writer.Write((short)entry.Key.Length);

                // Write the name.
                writer.Write(Encoding.UTF8.GetBytes(entry.Key));
                writer.Write((byte)0);
                writer.AlignBytes(4);
            }

            writer.AlignBytes(8);

            long end_pos = writer.BaseStream.Position;
            long size = end_pos - start_pos;

            //block sizes
            using (writer.BaseStream.TemporarySeek(start_pos + 4, SeekOrigin.Begin))
            {
                writer.Write((uint)size);
                writer.Write((uint)size);
            }
        }

        class StringEntry
        {
            public List<long> Positions = new List<long>();
        }
    }
}
