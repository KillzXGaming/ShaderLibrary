using ShaderLibrary;

BfshaFile bfsha = new BfshaFile("shader.bfsha");

Dictionary<string, string> options = new Dictionary<string, string>();

//add bfres options from a material here
options.Add("enable_shadows", "1");
//ensure to add skin count parameter (varies by game)
options.Add("skin_count", "0");

var shader = bfsha.ShaderModels[0];

var programIdx = shader.GetProgramIndex(options);
//no shader exists with the option combinations, skip
if (programIdx == -1)
    return;

var variation = shader.GetVariation(programIdx);
var program = shader.Programs[programIdx];

var pixel_bytecode = variation.BinaryProgram.FragmentShader.ByteCode;
var pixel_control = variation.BinaryProgram.FragmentShader.ControlCode;

var vertex_control = variation.BinaryProgram.VertexShader.ControlCode;
var vertex_bytecode = variation.BinaryProgram.VertexShader.ByteCode;

var refl = variation.BinaryProgram.FragmentShaderReflection.Samplers;

//we can also check what samplers are used
for (int i = 0; i < shader.Samplers.Count; i++)
{
    var location_info = program.SamplerIndices[i];
    //If index is -1, sampler is not binded in shader
    //else it is binded to the bind id (Ryujinx binds to uniform name by hex name 0x8 + (location id * 2)
    if (location_info.FragmentLocation == -1)
        continue;

    Console.WriteLine(string.Format("Sampler: {0}", shader.Samplers.GetKey(i)));
}

//we can also check what uniform blocks are used
for (int i = 0; i < shader.UniformBlocks.Count; i++)
{
    var location_info = program.UniformBlockIndices[i];
    //If index is -1, block is not binded in shader
    //else it is binded to the bind id (Ryujinx binds to uniform name by hex name 0x3 + location id)
    if (location_info.VertexLocation == -1 && location_info.FragmentLocation == -1)
        continue;

    Console.WriteLine(string.Format("UniformBlock: {0}", shader.UniformBlocks.GetKey(i)));
}

bfsha.Save("shader_new.bfsha");