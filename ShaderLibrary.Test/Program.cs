using ShaderLibrary;

BfshaFile bfsha = new BfshaFile("alRenderMaterial.bfsha");

Dictionary<string, string> options = new Dictionary<string, string>();

//add bfres options from a material here
options.Add("enable_ao", "1");

//ensure to add skin count parameter (varies by game)
options.Add("cSkinWeightNum", "0");

var programIdx = bfsha.ShaderModels[0].GetProgramIndex(options);
//no shader exists with the option combinations, skip
if (programIdx == -1)
    return;

var variation = bfsha.ShaderModels[0].GetVariation(programIdx);

var pixel_bytecode = variation.BinaryProgram.FragmentShader.ByteCode;
var pixel_control = variation.BinaryProgram.FragmentShader.ControlCode;

var vertex_control = variation.BinaryProgram.VertexShader.ControlCode;
var vertex_bytecode = variation.BinaryProgram.VertexShader.ByteCode;

bfsha.Save("alRenderMaterialRB.bfsha");