using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderLibrary.Test
{
    /// <summary>
    /// Tool for compiling using a game that uses the glslc library with an nso patch
    /// </summary>
    public class GlslcCompilerTool
    {
        public static void Run(BnshFile.ShaderCode vertexCode, BnshFile.ShaderCode pixelCode)
        {
            //using headless lib
            if (!Directory.Exists("ryujinx"))
                return;

            //get shaders
            string frag_code = Path.Combine("ryujinx", "portable", "sdcard", "fragment.bin.code");
            string frag_control = Path.Combine("ryujinx", "portable", "sdcard", "fragment.bin.control");

            //Remove previous
            if (File.Exists(frag_code)) File.Delete(frag_code);
            if (File.Exists(frag_control)) File.Delete(frag_control);

            //run exec and compile code
            string exec = Path.Combine("ryujinx", "Ryujinx.Headless.SDL2.exe");
            string game = Path.Combine("ryujinx", "game.nsp");
            Exec(exec, game);


            if (File.Exists(frag_code)) 
                pixelCode.ByteCode = File.ReadAllBytes(frag_code);
            if (File.Exists(frag_control)) 
                pixelCode.ControlCode = File.ReadAllBytes(frag_control);
        }

        private static void Exec(string exec, string game)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/K " + $"{exec} {game}");
            info.CreateNoWindow = true;
            info.UseShellExecute = true;
            info.CreateNoWindow = true;

            Process cmd = Process.Start(info);
            cmd.WaitForExit();
        }
    }
}
