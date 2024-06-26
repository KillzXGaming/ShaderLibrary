﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.CompileTool
{
    public class ControlShader
    {
        private uint Magic = 2557874740;
        private uint Count = 1;
        private uint val_0x8 = 9;
        private uint val_0xC = 288;
        private uint val_0x10 = 11;
        private uint val_0x14 = 2000; //pointer to byte at the end
        private uint val_0x18 = 0;
        private uint val_0x1C = 0;
        private uint val_0x20 = 2001; //size?

        private uint bytecode_len;
        private uint constants_len;
        private uint constants_start;
        private uint constants_end;

        private uint[] Unknowns = new uint[50];

        private byte val_0x2000;

        public ControlShader()
        {
            Unknowns[0] = 48;
            Unknowns[1] = 32; //todo this one varies
            Unknowns[28] = 2011069788;
            Unknowns[29] = 1721049022;
            Unknowns[30] = 3;
            Unknowns[32] = 1;
            Unknowns[34] = 2228756;
            Unknowns[48] = 2001;
        }

        public ControlShader(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data))) {
                Read(reader);
            }
        }

        public ControlShader(string filePath)
        {
            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                Read(reader);
            }
        }

        private void Read(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            Count = reader.ReadUInt32();
            val_0x8 = reader.ReadUInt32();
            val_0xC = reader.ReadUInt32();
            val_0x10 = reader.ReadUInt32();
            val_0x14 = reader.ReadUInt32();
            val_0x18 = reader.ReadUInt32();
            val_0x1C = reader.ReadUInt32();
            val_0x20 = reader.ReadUInt32();

            reader.BaseStream.Seek(1776, SeekOrigin.Begin);
            reader.ReadUInt64(); //points to end of file
            bytecode_len = reader.ReadUInt32();
            constants_len = reader.ReadUInt32();
            //start/end in byte code shader
            constants_start = reader.ReadUInt32();
            constants_end = reader.ReadUInt32();

            if (constants_len != 0)
            {
                //Console.WriteLine($"total {constants_end} aligned {GetAlignPos((int)bytecode_len + 48 + (int)constants_len, 256)} og {constants_end - bytecode_len - 48 - constants_len}");
            }

            for (int i = 0; i < 50; i++)
                Unknowns[i] = reader.ReadUInt32();

            val_0x2000 = reader.ReadByte();
        }



        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                Save(fs);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new BinaryWriter(stream)) {
                Write(writer);
            }
        }

        private void Write(BinaryWriter writer)
        {
            writer.Write(this.Magic);
            writer.Write(this.Count);
            writer.Write(val_0x8);
            writer.Write(val_0xC);
            writer.Write(val_0x10);
            writer.Write(val_0x14);
            writer.Write(val_0x18);
            writer.Write(val_0x1C);
            writer.Write(val_0x20);

            writer.BaseStream.Seek(1776, SeekOrigin.Begin);
            writer.Write((ulong)2001);
            writer.Write(bytecode_len);
            writer.Write(constants_len);
            writer.Write(constants_start);
            writer.Write(constants_end);

            for (int i = 0; i < 50; i++)
                writer.Write(Unknowns[i]);

            writer.Write(val_0x2000);
        }

        public float[] GetConstantsAsFloats(byte[] shader_code)
        {
            if (shader_code.Length < this.constants_start + this.constants_len)
                return new float[0];

            using (var reader = new BinaryReader(new MemoryStream(shader_code)))
            {
                reader.BaseStream.Seek(this.constants_start, SeekOrigin.Begin);

                float[] values = new float[this.constants_len / 4];
                for (int i = 0; i < values.Length; i++)
                    values[i] = reader.ReadSingle();
                return values;
            }
        }

        public byte[] GetConstants(byte[] shader_code)
        {
            if (shader_code.Length < this.constants_start + this.constants_len)
                return new byte[0];

            using (var reader = new BinaryReader(new MemoryStream(shader_code)))
            {
                reader.BaseStream.Seek(this.constants_start, SeekOrigin.Begin);
                return reader.ReadBytes((int)this.constants_len); 
            }
        }

        public void SetConstants(byte[] shader_code, byte[] constants, out byte[] new_shader_code)
        {
            this.constants_len = (uint)constants.Length; //constants size
            this.bytecode_len = GetBytecodeLength(shader_code);  //shader code without the header

            //save to output shader bytecode
            var mem = new MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                //shader code
                writer.Write(shader_code);

                AlignBytes(writer, 256);

                //constants (don't write atm, buggy alignment issues)
                this.constants_start = (uint)writer.BaseStream.Position;

                if (constants.Length > 0)
                {
                    writer.Write(constants);
                    AlignBytes(writer, 256);
                }

                this.constants_end = (uint)writer.BaseStream.Position;
            }

            //save output
            new_shader_code = mem.ToArray();
        }

        private uint GetBytecodeLength(byte[] code)
        {
            using (var reader = new BinaryReader(new MemoryStream(code)))
            {
                reader.ReadBytes(48); //start
                reader.ReadBytes(0x50); //nvn header
                //byte code here
                int bytecode_size = 0;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    ulong cmd = reader.ReadUInt64();
                    if (cmd == 0)
                        break;

                    bytecode_size += 8;
                }
                return (uint)bytecode_size + 0x50;
            }
        }

        static void AlignBytes(BinaryWriter wr, int align, byte pad_val = 0)
        {
            var startPos = wr.BaseStream.Position;
            long position = wr.Seek((int)(-wr.BaseStream.Position % align + align) % align, SeekOrigin.Current);

            wr.Seek((int)startPos, System.IO.SeekOrigin.Begin);
            while (wr.BaseStream.Position != position)
            {
                wr.Write((byte)pad_val);
            }
        }

        static int GetAlignPos(int startPos, int align)
        {
            return (-startPos % align + align) % align;
        }
    }
}
