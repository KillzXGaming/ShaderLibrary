using ShaderLibrary.IO;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace ShaderLibrary
{
    internal readonly ref struct TemporarySeekHandle
        (Stream stream, long retpos)
    {
        private readonly Stream Stream = stream;
        private readonly long RetPos = retpos;

        public readonly void Dispose()
        {
            Stream.Seek(RetPos, SeekOrigin.Begin);
        }
    }

    internal static class Utils
    {
        public static Span<byte> AsSpan<T>(ref T val) where T : unmanaged
        {
            Span<T> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<T, byte>(valSpan);
        }

        public static TemporarySeekHandle TemporarySeek(this Stream stream, long offset, SeekOrigin origin)
        {
            long ret = stream.Position;
            stream.Seek(offset, origin);
            return new TemporarySeekHandle(stream, ret);
        }
    }
}
