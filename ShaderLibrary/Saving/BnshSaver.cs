﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Reflection.PortableExecutable;
using ShaderLibrary.Common;
using System.Diagnostics;

namespace ShaderLibrary
{
    public class BnshSaver
    {
        private const string _bnshsignature = "BNSH";
        private const string _grscSignature = "grsc";

        public RelocationTable RelocationTable = new RelocationTable();
        public StringTable StringTable = new StringTable();


        private long _fileSizePos;

        class RelocatableOffset
        {
            public long Position; //

            public long Offset;
        }

        public void Save(BnshFile bnsh, BinaryWriter writer)
        {
            //RLT section list as order of data written
            //0 = main file
            //1 = obj data pointers (one per variation
            //2 = (0)
            //3 = (0)
            //4 = byte code data pointers
            //5 = string table

            writer.WriteSignature(_bnshsignature);
            writer.Write(0);
            writer.Write(bnsh.BinHeader.VersionMicro);
            writer.Write(bnsh.BinHeader.VersionMinor);
            writer.Write(bnsh.BinHeader.VersionMajor);
            writer.Write(bnsh.BinHeader.ByteOrder);
            writer.Write((byte)bnsh.BinHeader.Alignment);
            writer.Write((byte)bnsh.BinHeader.TargetAddressSize);
            SaveFileNameString(writer, bnsh.Name);
            writer.Write((ushort)bnsh.BinHeader.Flag);

            writer.Write((ushort)0x60); //block offset
            RelocationTable.SaveHeaderOffset(writer);
            _fileSizePos = writer.BaseStream.Position;
            writer.Write(0u);

            writer.Write(new byte[64]);

            long grscPos = writer.BaseStream.Position;

            //GRSC
            writer.WriteSignature(_grscSignature);
            writer.Write(bnsh.Header.BlockSize); //gets updated later
            writer.Write((ulong)bnsh.Header.BlockOffset); //gets updated later
            writer.Write(bnsh.Header.Version);
            writer.Write(bnsh.Header.CodeTarget);
            writer.Write(bnsh.Header.CompilerVersion);
            writer.Write(bnsh.Variations.Count);

            RelocationTable.SaveEntry(writer, 2, 1, 0, 0, "grsc header");

            var shaderVariationArrayOffset = writer.SaveOffset();
            var memoryPoolOffset = writer.SaveOffset();
            writer.Write(bnsh.Header.Unknown2);

            writer.Write(new byte[40]); //reserved

            long shaderVarPos = writer.BaseStream.Position;
            WriteOffset(writer, shaderVariationArrayOffset);

            RelocationTable.SaveEntry(writer,
                (uint)writer.BaseStream.Position + 16, 2, (uint)bnsh.Variations.Count, 6, 0, "variation headers");

            //Shader variations
            long variationPos = writer.BaseStream.Position;
            foreach (var var in bnsh.Variations)
            {
                writer.Write(0L); //
                writer.Write(0L); //
                writer.Write(0L); //shader program offset
                writer.Write(grscPos);
                writer.Write(new byte[32]); //reserved
            }

            long[] byteCodeOffsetsVertex = new long[bnsh.Variations.Count];
            long[] byteCodeOffsetsPixel = new long[bnsh.Variations.Count];
            long[] byteCodeOffsetsGeom = new long[bnsh.Variations.Count];
            long[] byteCodeOffsetsCompute = new long[bnsh.Variations.Count];

            long[] objOffsets = new long[bnsh.Variations.Count];

            Dictionary<byte[], long> data_blocks = new Dictionary<byte[], long>(new ByteArrayComparer());

            int num_dict = 0;

            //Shader programs
            for (int i = 0; i < bnsh.Variations.Count; i++)
            {
                WriteOffset(writer, shaderVarPos + i * 64 + 16);

                var prog = bnsh.Variations[i].BinaryProgram;

                writer.Write(prog.header.CodeType);
                writer.Write((byte)prog.header.Format);
                writer.Write(new byte[2]); //reserved
                writer.Write(prog.header.BinaryFormat);

                long[] stageOffsets = new long[6];

                RelocationTable.SaveEntry(writer, 6, 1, 0, 0, "stage offsets");

                for (int j = 0; j < 6; j++)
                    stageOffsets[j] = writer.SaveOffset();

                writer.Write(new byte[40]); //reserved

                writer.Write(prog.MemoryData.Length);
                writer.Write(0);

                RelocationTable.SaveEntry(writer, 1, 1, 0, 1, "obj data");

                objOffsets[i] = writer.SaveOffset();

                RelocationTable.SaveEntry(writer, 2, 1, 0, 0, "program offsets");

                writer.Write((ulong)(variationPos + i * 64)); //variation offset
                var reflectionOffset = writer.SaveOffset();
                writer.Write(new byte[32]); //reserved

                long[] controlCodeOffsets = new long[6];

                //shader codes
                void WriteShaderHeaders(BnshFile.ShaderCode code, int idx, ref long byteCodeOffset)
                {
                    if (code == null || code.ByteCode == null) return;

                    WriteOffset(writer, stageOffsets[idx]);

                    writer.Write(0L); //0
                    controlCodeOffsets[idx] = writer.SaveOffset();  //Control code offset
                    byteCodeOffset = writer.SaveOffset();  //Byte code offset

                    writer.Write((uint)code.ByteCode.Length);
                    writer.Write((uint)code.ControlCode.Length);
                    writer.Write(code.Reserved); //reserved
                }

                long spos = writer.BaseStream.Position;

                WriteShaderHeaders(prog.VertexShader, 0, ref byteCodeOffsetsVertex[i]);
                WriteShaderHeaders(prog.GeometryShader, 3, ref byteCodeOffsetsGeom[i]);
                WriteShaderHeaders(prog.FragmentShader, 4, ref byteCodeOffsetsPixel[i]);
                WriteShaderHeaders(prog.ComputeShader, 5, ref byteCodeOffsetsCompute[i]);

                var num_headers = (uint)(writer.BaseStream.Position - spos) / 64;

                RelocationTable.SaveEntry(writer, (uint)spos + 8, 1, num_headers, 7, 0, "control code offsets");
                RelocationTable.SaveEntry(writer, (uint)spos + 16, 1, num_headers, 7, 4, "byte code offsets");

                long[] stageReflectOffsets = new long[6];

                //reflection
                if (prog.VertexShaderReflection != null || 
                    prog.FragmentShaderReflection != null ||
                    prog.GeometryShaderReflection != null ||
                    prog.ComputeShaderReflection != null)
                {
                    WriteOffset(writer, reflectionOffset);

                    RelocationTable.SaveEntry(writer, 6, 1, 0, 0, "reflection stage offsets");
                    for (int j = 0; j < 6; j++)
                        stageReflectOffsets[j] = writer.SaveOffset();

                    writer.Write(new byte[16]); //reserved
                }

                void SaveControlCode(BnshFile.ShaderCode code, int idx)
                {
                    if (code == null || code.ControlCode == null)
                        return;

                    byte[] data = code.ControlCode;

                    if (data_blocks.ContainsKey(data))
                    {
                        using (writer.BaseStream.TemporarySeek(controlCodeOffsets[idx], SeekOrigin.Begin))
                        {
                            writer.Write(data_blocks[data]);
                        }
                    }
                    else
                    {
                        writer.AlignBytes(8);
                        data_blocks.Add(data, writer.BaseStream.Position);

                        WriteOffset(writer, controlCodeOffsets[idx]);
                        writer.Write(data);
                    }
                }

                //binaries
                SaveControlCode(prog.VertexShader, 0);
             //   SaveControlCode(prog.TessellationControlShader, 1);
             //   SaveControlCode(prog.TessellationEvaluationShader, 2);
                SaveControlCode(prog.GeometryShader, 3);
                SaveControlCode(prog.FragmentShader, 4);
                SaveControlCode(prog.ComputeShader, 5);

                //reflection data
                BnshFile.ShaderReflectionData[] reflectionDatas = new BnshFile.ShaderReflectionData[6];
                reflectionDatas[0] = prog.VertexShaderReflection;
                reflectionDatas[1] = prog.HullShaderReflection;
                reflectionDatas[2] = prog.DomainShaderReflection;
                reflectionDatas[3] = prog.GeometryShaderReflection;
                reflectionDatas[4] = prog.FragmentShaderReflection;
                reflectionDatas[5] = prog.ComputeShaderReflection;

                ReflectionPointers[] reflectionPointers = new ReflectionPointers[6];

                for (int j = 0; j < 6; j++)
                {
                    if (reflectionDatas[j] == null)
                        continue;

                    writer.AlignBytes(8);

                    WriteOffset(writer, stageReflectOffsets[j]);

                    reflectionPointers[j] = new ReflectionPointers();

                    RelocationTable.SaveEntry(writer, 5, 1, 0, 0, "reflection stage offsets");

                    reflectionDatas[j].header.InputDictionaryOffset = (ulong)writer.SaveOffset();
                    reflectionDatas[j].header.OutputDictionaryOffset = (ulong)writer.SaveOffset();
                    reflectionDatas[j].header.SamplerDictionaryOffset = (ulong)writer.SaveOffset();
                    reflectionDatas[j].header.ConstantBufferDictionaryOffset = (ulong)writer.SaveOffset();
                    reflectionDatas[j].header.UnorderedAccessBufferDictionaryOffset = (ulong)writer.SaveOffset();

                    writer.Write(reflectionDatas[j].header.OutputIdx);
                    writer.Write(reflectionDatas[j].header.SamplerIdx);
                    writer.Write(reflectionDatas[j].header.ConstBufferIdx);
                    writer.Write(reflectionDatas[j].header.SlotCount);

                    RelocationTable.SaveEntry(writer, 1, 1, 0, 0, "Reflection slots");

                    reflectionDatas[j].header.SlotOffset = (int)writer.BaseStream.Position;
                    writer.Write(reflectionDatas[j].header.SlotOffset);
                    writer.Write(reflectionDatas[j].header.ComputeWorkGroupX);
                    writer.Write(reflectionDatas[j].header.ComputeWorkGroupY);
                    writer.Write(reflectionDatas[j].header.ComputeWorkGroupZ);
                    writer.Write(reflectionDatas[j].header.Unknown1);
                    writer.Write(reflectionDatas[j].header.Unknown2);
                    writer.Write(reflectionDatas[j].header.Unknown3);
                }

                foreach (var data in reflectionDatas)
                {
                    if (data == null)
                        continue;

                    if (data.Inputs.Count > 0)
                    {
                        writer.AlignBytes(8);
                        WriteOffset(writer, (long)data.header.InputDictionaryOffset);
                        WriteDictionary(writer, data.Inputs);
                        num_dict++;
                    }
                    if (data.Outputs.Count > 0)
                    {
                        writer.AlignBytes(8);
                        WriteOffset(writer, (long)data.header.OutputDictionaryOffset);
                        WriteDictionary(writer, data.Outputs);
                        num_dict++;
                    }
                    if (data.Samplers.Count > 0)
                    {
                        writer.AlignBytes(8);
                        WriteOffset(writer, (long)data.header.SamplerDictionaryOffset);
                        WriteDictionary(writer, data.Samplers);
                        num_dict++;
                    }
                    if (data.ConstantBuffers.Count > 0)
                    {
                        writer.AlignBytes(8);
                        WriteOffset(writer, (long)data.header.ConstantBufferDictionaryOffset);
                        WriteDictionary(writer, data.ConstantBuffers);
                        num_dict++;
                    }
                    if (data.UnorderedAccessBuffers.Count > 0)
                    {
                        writer.AlignBytes(8);
                        WriteOffset(writer, (long)data.header.UnorderedAccessBufferDictionaryOffset);
                        WriteDictionary(writer, data.UnorderedAccessBuffers);
                        num_dict++;

                    }
                    if (data.Slots.Length > 0)
                    {
                        writer.AlignBytes(8);

                        //Uint32 offset
                        var pos = writer.BaseStream.Position;
                        using (writer.BaseStream.TemporarySeek(data.header.SlotOffset, System.IO.SeekOrigin.Begin))
                        {
                            writer.Write((uint)pos);
                        }

                        writer.Write(data.Slots);
                    }
                }
            }

            writer.AlignBytes(8);

            //memory pool
            WriteOffset(writer, memoryPoolOffset);

            writer.Write(97); //mem pool property
            long binary_buffer_size = writer.BaseStream.Position;
            writer.Write(0u);

            RelocationTable.SaveEntry(writer, 1, 1, 0, 4, "Pool Buffer Offset");
            long binary_buffer_offset = writer.SaveOffset();
            writer.Write(new byte[0x10]);

            RelocationTable.SaveEntry(writer, 1, 1, 0, 0, "Pool Offset");
            long memory_offset = writer.SaveOffset();
            writer.Write(new byte[40]);

            WriteOffset(writer, memory_offset);
            writer.Write(new byte[320]); //pool

            //relocation table section 0 start/end
            RelocationTable.SetRelocationSection(0, 0, (uint)writer.BaseStream.Position);

            long obj_start = writer.BaseStream.Position;

            for (int i = 0; i < bnsh.Variations.Count; i++)
            {
                var prog = bnsh.Variations[i].BinaryProgram;

                writer.AlignBytes(4);
                WriteOffset(writer, objOffsets[i]);
                writer.Write(prog.MemoryData);
            }

            long obj_size = writer.BaseStream.Position - obj_start;

            //relocation table section 1 start/end
            RelocationTable.SetRelocationSection(1, (uint)writer.BaseStream.Position, (uint)obj_size);
            //relocation table section 2 start/end
            RelocationTable.SetRelocationSection(2, (uint)writer.BaseStream.Position, 0u);
            //relocation table section 3 start/end
            RelocationTable.SetRelocationSection(3, (uint)writer.BaseStream.Position, 0u);

            //raw binary byte code
            writer.AlignBytes(4096);

            long data_start = writer.BaseStream.Position;

            WriteOffset(writer, binary_buffer_offset);

            Dictionary<byte[], long> code_blocks = new Dictionary<byte[], long>(new ByteArrayComparer());

            for (int i = 0; i < bnsh.Variations.Count; i++)
            {
                var prog = bnsh.Variations[i].BinaryProgram;

                void SaveShaderCode(BnshFile.ShaderCode code, long offset_save)
                {
                    if (code == null || code.ByteCode == null)
                        return;

                    byte[] data = code.ByteCode;

                    if (code_blocks.ContainsKey(data))
                    {
                        using (writer.BaseStream.TemporarySeek(offset_save, SeekOrigin.Begin))
                        {
                            writer.Write(code_blocks[data]);
                        }
                    }
                    else
                    {
                        writer.AlignBytes(8);
                        code_blocks.Add(data, writer.BaseStream.Position);

                        WriteOffset(writer, offset_save);
                        writer.Write(data);
                    }
                }

                SaveShaderCode(prog.VertexShader, byteCodeOffsetsVertex[i]);
                SaveShaderCode(prog.GeometryShader, byteCodeOffsetsGeom[i]);
                SaveShaderCode(prog.FragmentShader, byteCodeOffsetsPixel[i]);
                SaveShaderCode(prog.ComputeShader, byteCodeOffsetsCompute[i]);
            }

            writer.AlignBytes(8192);

            long data_end = writer.BaseStream.Position;
            long data_size = data_end - data_start;

            using (writer.BaseStream.TemporarySeek(binary_buffer_size, SeekOrigin.Begin)) {
                writer.Write((uint)data_size);
            }

            //relocation table section 4 start/end
            RelocationTable.SetRelocationSection(4, (uint)data_start, (uint)data_size);

            writer.AlignBytes(8);

            var size = writer.BaseStream.Position - grscPos;

            //block sizes
            using (writer.BaseStream.TemporarySeek(grscPos + 4, SeekOrigin.Begin))
            {
                writer.Write((uint)size);
                writer.Write((uint)size); //offset to next
            }

            StringTable.Write(writer);
            RelocationTable.Write(writer);

            //file size
            using (writer.BaseStream.TemporarySeek(this._fileSizePos, SeekOrigin.Begin))
            {
                writer.Write((uint)writer.BaseStream.Length);
            }
        }

        private void WriteDictionary(BinaryWriter saver, ResDict<ResString> resDict)
        {
            resDict.GenerateTree();
            var nodes = resDict.GetNodes();

            saver.WriteSignature("_DIC");
            saver.Write(nodes.Count - 1);

            int curNode = 0;
            foreach (var node in nodes)
            {
                saver.Write(node.Reference);
                saver.Write(node.IdxLeft);
                saver.Write(node.IdxRight);

                if (curNode == 0) //root (empty)
                {
                    //Relocate from the first entry
                    RelocationTable.SaveEntry(saver, 1, (uint)nodes.Count, 1, 5, "Dict");
                    SaveString(saver, "");
                }
                else
                {
                    SaveString(saver, node.Key);
                }
                curNode++;
            }
        }

        private void WriteOffset(BinaryWriter saver, long offset)
        {
            long pos = saver.BaseStream.Position;
            using (saver.BaseStream.TemporarySeek(offset, System.IO.SeekOrigin.Begin))
            {
                saver.Write((uint)pos);
            }
        }

        private void SaveFileNameString(BinaryWriter writer, string name)
        {
            var pos = writer.BaseStream.Position;

            writer.Write(0u); //uint size

            StringTable.AddEntry(pos, name);
        }

        private void SaveString(BinaryWriter writer, string str)
        {
            var ofs = writer.SaveOffset();

            if (str == null)
                return;

            StringTable.AddEntry(ofs, str);
        }
    }

    class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }
            return left.SequenceEqual(right);
        }
        public int GetHashCode(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return key.Sum(b => b);
        }
    }

    class ReflectionPointers
    {
        internal long _ofsShaderInputDictionary;
        internal long _ofsShaderOutputDictionary;
        internal long _ofsShaderSamplerDictionary;
        internal long _ofsShaderConstantBufferDictionary;
        internal long _ofsUnorderedAccessBufferDictionary;

        internal long _ofsAttributeSlots;
    }
}