﻿using ShaderLibrary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.IO
{
    public class BinaryDataWriter : BinaryWriter
    {
        public BinaryHeader Header;

        public RelocationTable RelocationTable = new RelocationTable();
        public StringTable StringTable = new StringTable();

        public long Position => this.BaseStream.Position;

        public BinaryDataWriter(Stream input) : base(input)
        {
        }


        public long SaveOffset()
        {
            long pos = this.Position;
            this.Write(0UL);
            return pos;
        }

        public void Write(float[] values)
        {
            for (int i = 0; i < values.Length; i++)
                this.Write(values[i]);
        }

        public void Write(uint[] values)
        {
            for (int i = 0; i < values.Length; i++)
                this.Write(values[i]);
        }

        public void Write(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
                this.Write(values[i]);
        }

        public void Write(bool[] values)
        {
            for (int i = 0; i < values.Length; i++)
                this.Write(values[i]);
        }

        public void WriteSignature(string magic)
        {
            this.Write(Encoding.ASCII.GetBytes(magic));
        }

        internal void WriteDictionary<T>(ResDict<T> dict, long offset_pos) where T : IResData, new()
        {
            if (dict.Count == 0) return;

            this.AlignBytes(8);
            this.WriteOffset(offset_pos);
            WriteDictionary(dict);
        }

        internal void WriteOffset(long offset)
        {
            if (offset == 0)
                return;

            //The offset to point to
            long target = Position;

            //Seek to where to write the offset itself and use relative position
            using (this.BaseStream.TemporarySeek((uint)offset, SeekOrigin.Begin)) {
                Write(((uint)target));
            }
        }

        private void WriteDictionary<T>(ResDict<T> resDict) where T : IResData, new()
        {
            resDict.GenerateTree();
            var nodes = resDict.GetNodes();

            this.WriteSignature("_DIC");
            this.Write(nodes.Count - 1);

            int curNode = 0;
            foreach (var node in nodes)
            {
                this.Write(node.Reference);
                this.Write(node.IdxLeft);
                this.Write(node.IdxRight);

                if (curNode == 0) //root (empty)
                {
                    //Relocate from the first entry
                    RelocationTable.SaveEntry(this, 1, (uint)nodes.Count, 1, 5, "Dict");
                    SaveString("");
                }
                else
                {
                    SaveString(node.Key);
                }
                curNode++;
            }
        }

        public void SaveString(string str)
        {
            var ofs = this.SaveOffset();

            if (str == null)
                return;

            StringTable.AddEntry(ofs, str);
        }

        public void AlignBytes(int align, byte pad_val = 0)
        {
            var startPos = this.BaseStream.Position;
            long position = this.Seek((int)(-this.BaseStream.Position % align + align) % align, SeekOrigin.Current);

            this.Seek((int)startPos, System.IO.SeekOrigin.Begin);
            while (this.BaseStream.Position != position)
            {
                this.Write((byte)pad_val);
            }
        }
    }
}
