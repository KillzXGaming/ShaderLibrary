using BfshaLibrary.WiiU;
using ShaderLibrary.IO;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.WiiU
{
    public class GX2DisplayListHelper
    {
        static readonly Dictionary<int, uint> _vertex_scalars = new(){
            {0,0x28868},
            {1,0x28a84},
            {2,0x286c4},
            {14,0x2881c},
            {15,0x288e0},
            {49,0x28b20},
            {50,0x28c58},
            {51,0x28a20}};

        static readonly Dictionary<int, uint> _pixel_scalars = new(){
            {0,0x28850},
            {1,0x28854},
            {2,0x286cc},
            {3,0x286d0},
            {37,0x2823c},
            {38,0x287a0},
            {39,0x2880c},
            {40,0x286d8}};

        public enum GX2_PATCH_TYPE
        {
            FETCH_SHADER = 1,
            VERTEX_SHADER = 2,
            GEOMETRY_COPY_SHADER = 3,
            GEOMETRY_SHADER = 4,
            PIXEL_SHADER = 5,
            COMPUTE_SHADER = 6
        }

        const int SET_CONTEXT_REG = 0x69;
        const int SET_LOOP_CONST = 0x6c;

        public static T CreateShader<T>(GX2ShaderDisplayList shader) where T : BfshaGX2Header, new()
        {
            BfshaGX2Header result = null;

            if (shader.DisplayList == null) return (T)result;

            var type = (GX2_PATCH_TYPE)shader.patchType;
            var regs = new Dictionary<uint, uint>();
            var loops = new Dictionary<uint, uint>();

            using var reader = new BinaryDataReader(
                new MemoryStream(shader.DisplayList), true);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int packetStart = checked((int)reader.BaseStream.Position);
                uint header = reader.ReadUInt32();

                int count = checked((int)((header >> 16) & 0x3fff) + 1);
                uint opcode = (header >> 8) & 0xff;

                if ((header >> 30) != 3 || (header & 0xff) != 0 ||
                    (opcode != SET_CONTEXT_REG && opcode != SET_LOOP_CONST))
                {
                    throw new InvalidDataException(
                        $"Unsupported PM4 header {header:X8} at 0x{packetStart:X}");
                }

                // Ensure packet can be read
                int packetSize = checked((count + 1) * 4);
                if (packetSize > reader.BaseStream.Length - packetStart)
                    throw new InvalidDataException("Invalid PM4 packet");

                uint index = reader.ReadUInt32();
                uint address = checked(
                    (opcode == SET_CONTEXT_REG ? 0x28000u : 0x3e200u) +
                    4u * index);

                for (int i = 0; i < count - 1; i++)
                {
                    uint at = address + 4u * (uint)i;
                    uint value = reader.ReadUInt32(); // either dst index for set register, or a loop value
                    if (opcode == SET_LOOP_CONST)
                        loops[at] = value;
                    else
                        regs[at] = value;
                }
            }

            var raw_registers = RebuildNativeRegisters(type, regs);
            switch (type)
            {
                case GX2_PATCH_TYPE.VERTEX_SHADER:
                    result = new BfshaGX2VertexHeader()
                    {
                        Regs = raw_registers,
                        Data = shader.Code,
                        Mode = shader.modeType,
                        UnusedHeader = new uint[22],
                        DisplayList = shader.DisplayList,
                        Loops = loops.Select(x => new GSHFile.GX2LoopVar()
                        {
                            Offset = x.Key,
                            Value = x.Value
                        }).ToList(),
                    };
                    break;
                case GX2_PATCH_TYPE.PIXEL_SHADER:
                    result = new BfshaGX2PixelHeader()
                    {
                        Regs = raw_registers,
                        Data = shader.Code,
                        Mode = shader.modeType,
                        UnusedHeader = new uint[14],
                        DisplayList = shader.DisplayList,
                        Loops = loops.Select(x => new GSHFile.GX2LoopVar()
                        {
                            Offset = x.Key,
                            Value = x.Value
                        }).ToList(),
                    }; 
                    break;
                case GX2_PATCH_TYPE.GEOMETRY_SHADER:
                case GX2_PATCH_TYPE.GEOMETRY_COPY_SHADER:
                    result = new BfshaGX2GeometryHeader()
                    {
                        Regs = raw_registers,
                        Data = shader.Code,
                        Mode = shader.modeType,
                        UnusedHeader = new uint[14],
                        DisplayList = shader.DisplayList,
                        Loops = loops.Select(x => new GSHFile.GX2LoopVar()
                        {
                            Offset = x.Key,
                            Value = x.Value
                        }).ToList(),
                    };
                    break;
            }
            return (T)result;
        }

        static byte[] RebuildNativeRegisters(GX2_PATCH_TYPE patchType, IReadOnlyDictionary<uint, uint> regs)
        {
            // vertex patch or pixel patch
            uint[] result = new uint[0];
            // Check which address values to apply for vertex/pixel registers
            var values = _vertex_scalars;
            switch (patchType)
            {
                case GX2_PATCH_TYPE.VERTEX_SHADER:
                    result = new uint[52];
                    values = _vertex_scalars;
                    break;
                case GX2_PATCH_TYPE.PIXEL_SHADER:
                    result = new uint[41];
                    values = _pixel_scalars;
                    break;
                // TODO.
                case GX2_PATCH_TYPE.GEOMETRY_SHADER:
                    result = new uint[19];
                    break;
                case GX2_PATCH_TYPE.GEOMETRY_COPY_SHADER:
                    result = new uint[19];
                    break;
            }

            foreach (var pair in values)
                if (regs.TryGetValue(pair.Value, out uint value)) 
                    result[pair.Key] = value;

            void CopyRun(uint address, int limit, int first, int countIndex)
            {
                for (int i = 0; i < limit; i++)
                    if (regs.TryGetValue(address + 4 * (uint)i, out uint value)) {
                        result[first + i] = value; 
                        result[countIndex] = (uint)i + 1;
                    }
            }

            if (patchType ==  GX2_PATCH_TYPE.VERTEX_SHADER)
            {
                CopyRun(0x28614, 10, 4, 3); 
                CopyRun(0x28380, 32, 17, 16);
            }
            else
                CopyRun(0x28644, 32, 5, 4);

            byte[] bytes = new byte[result.Length * 4];
            for (int i = 0; i < result.Length; i++) 
                BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(i * 4, 4), result[i]);
            return bytes;
        }
    }
}
