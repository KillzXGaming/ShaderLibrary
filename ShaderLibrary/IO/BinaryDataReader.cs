using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.IO
{
    public class BinaryDataReader : BinaryReader
    {
        public BinaryHeader Header;

        public long Position => this.BaseStream.Position;

        private bool IsBigEndian = false;

        public BinaryDataReader(Stream input, bool is_big_endian = false) : base(input)
        {
            IsBigEndian = is_big_endian;
        }

        public override uint ReadUInt32()
        {
            var bytes = base.ReadBytes(4);
            if (IsBigEndian) Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes);
        }

        public override int ReadInt32()
        {
            var bytes = base.ReadBytes(4);
            if (IsBigEndian) Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes);
        }

        public override short ReadInt16()
        {
            var bytes = base.ReadBytes(2);
            if (IsBigEndian) Array.Reverse(bytes);

            return BitConverter.ToInt16(bytes);
        }

        public override ushort ReadUInt16()
        {
            var bytes = base.ReadBytes(2);
            if (IsBigEndian) Array.Reverse(bytes);

            return BitConverter.ToUInt16(bytes);
        }

        public long ReadOffset()
        {
            if (IsBigEndian) //Wii u
            {
                var pos = this.BaseStream.Position;
                return pos + this.ReadUInt32();
            }
            return ReadInt64();
        }

        public T ReadCustom<T>(Func<T> value, ulong offset)
        {
            if (offset == 0) return default(T);

            using (this.BaseStream.TemporarySeek((long)offset, SeekOrigin.Begin)) {
                return value.Invoke();
            }
        }


        public sbyte[] ReadSbytes(int count)
        {
            sbyte[] values = new sbyte[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadSByte();
            return values;
        }

        public bool[] ReadBooleans(int count)
        {
            bool[] values = new bool[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadBoolean();
            return values;
        }

        public float[] ReadSingles(int count)
        {
            float[] values = new float[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadSingle();
            return values;
        }

        public ushort[] ReadUInt16s(int count)
        {
            ushort[] values = new ushort[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadUInt16();
            return values;
        }

        public int[] ReadInt32s(int count)
        {
            int[] values = new int[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadInt32();
            return values;
        }

        public uint[] ReadUInt32s(int count)
        {
            uint[] values = new uint[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadUInt32();
            return values;
        }

        public long[] ReadInt64s(int count)
        {
            long[] values = new long[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadInt64();
            return values;
        }

        public ulong[] ReadUInt64s(int count)
        {
            ulong[] values = new ulong[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadUInt64();
            return values;
        }

        public string LoadString()
        {
            return LoadString((uint)this.ReadOffset());
        }

        public string LoadString(ulong offset)
        {
            using (this.BaseStream.TemporarySeek((long)offset, SeekOrigin.Begin)) {
                return ReadZeroTerminatedString();
            }
        }

        public ResDict<T> LoadDictionary<T>() where T : IResData, new()
        {
            uint valuesOffset = (uint)this.ReadUInt64();
            uint dictOffset = (uint)this.ReadUInt64();

            return this.LoadDictionary<T>(dictOffset, valuesOffset);
        }

        public ResDict<T> LoadDictionary<T>(ulong offset) where T : IResData, new()
        {
            if (offset == 0) return new ResDict<T>();

            using (this.BaseStream.TemporarySeek((long)offset, SeekOrigin.Begin))
            {
                return this.ReadDictionary<T>();
            }
        }

        public ResDict<T> LoadDictionary<T>(ulong offset, ulong valueOffset) where T : IResData, new()
        {
            if (offset == 0)
                return new ResDict<T>();

            using (this.BaseStream.TemporarySeek((long)offset, SeekOrigin.Begin))
            {
                var dict = this.ReadDictionary<T>();
                var list = this.ReadArray<T>(valueOffset, dict.Keys.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    string key = dict.GetKey(i);
                    dict[key] = list[i];
                }
                return dict;
            }
        }

        private ResDict<T> ReadDictionary<T>() where T : IResData, new()
        {
            return this.Read<ResDict<T>>();
        }

        public List<T> ReadArray<T>(ulong offset, int count) where T : IResData
        {
            using (this.BaseStream.TemporarySeek((long)offset, SeekOrigin.Begin))
            {
                return this.ReadArray<T>(count);
            }
        }
        public List<T> ReadArray<T>(int count) where T : IResData
        {
            List<T> list = new();
            for (int i = 0; i < count; i++)
                list.Add(this.Read<T>());
            return list;
        }

        public T Read<T>(ulong offset) where T : IResData, new()
        {
            T instance = (T)Activator.CreateInstance(typeof(T));

            if (offset == 0)
                return default(T);

            this.SeekBegin((long)offset);
            instance.Read(this);
            return instance;
        }

        public T Read<T>() where T : IResData
        {
            T instance = (T)Activator.CreateInstance(typeof(T));
            instance.Read(this);
            return instance;
        }

        public void SeekBegin(long offset)
        {
            this.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public void SeekBegin(ulong offset)
        {
            this.BaseStream.Seek((long)offset, SeekOrigin.Begin);
        }


        public string ReadZeroTerminatedString(int maxLength = int.MaxValue)
        {
            long start = this.Position;
            int size = 0;

            // Read until we hit the end of the stream (-1) or a zero
            while (this.ReadByte() - 1 > 0 && size < maxLength)
            {
                size++;
            }

            this.BaseStream.Position = start;
            string text = Encoding.UTF8.GetString(this.ReadBytes(size), 0, size);
            this.BaseStream.Position++; // Skip the null byte
            return text;
        }
    }
}
