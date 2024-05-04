using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShaderLibrary.Helpers
{
    public class ShaderOptionSearcher
    {
        public static int GetProgramIndex(ShaderModel shader, Dictionary<string, string> options)
        {
            //Generate keys of the shader options
            int[] key_lookup = WriteOptionKeys(shader, options);

            //Lookup the key in the key list
            int num_keys_per_program = shader.StaticKeyLength + shader.DynamicKeyLength;
            for (int i = 0; i < shader.Programs.Count; i++)
            {
                if (IsMatch(shader, i, key_lookup))
                    return i;
            }

            //Temp fall back to previous search method
            //Should not need to run unless the shader options given are incomplete (ie missing render info options)
            for (int i = 0; i < shader.Programs.Count; i++)
            {
                var idx = num_keys_per_program * i;

                if (IsValidProgram(shader, i, options))
                    return i;
            }
            return -1;
        }

        static bool IsMatch(ShaderModel shader, int programIdx, int[] keys)
        {
            int num_keys_per_program = shader.StaticKeyLength + shader.DynamicKeyLength;
            var idx = num_keys_per_program * programIdx;

            var program_keys = shader.KeyTable.Skip(idx).Take(keys.Length);
            return keys.SequenceEqual(program_keys);
        }

        static int[] WriteOptionKeys(ShaderModel shader, Dictionary<string, string> options)
        {
            //Setup default keys
            int[] key_lookup = WriteDefaultKey(shader);

            //Setup static and dynamic keys
            for (int j = 0; j < shader.StaticOptions.Count; j++)
            {
                var option = shader.StaticOptions[j];
                if (!options.ContainsKey(option.Name))
                    continue;

                //Set the static option choice
                int choiceIndex = option.Choices.GetIndex(options[option.Name]);
                if (choiceIndex == -1)
                    throw new Exception(string.Format("Invalid choice given {1} for option {0}!", option.Name, options[option.Name]));

                option.SetKey(ref key_lookup[option.Bit32Index], choiceIndex);
            }

            for (int j = 0; j < shader.DynamicOptions.Count; j++)
            {
                var option = shader.DynamicOptions[j];
                if (!options.ContainsKey(option.Name))
                    continue;

                //Set the dynamic option choice
                int choiceIndex = option.Choices.GetIndex(options[option.Name]);
                if (choiceIndex == -1)
                    throw new Exception(string.Format("Invalid choice given {1} for option {0}!", option.Name, options[option.Name]));

                int ind = option.Bit32Index - option.KeyOffset;
                option.SetKey(ref key_lookup[shader.StaticKeyLength + ind], choiceIndex);
            }
            return key_lookup;
        }

        static int[] WriteDefaultKey(ShaderModel shader)
        {
            int num_keys = shader.StaticKeyLength + shader.DynamicKeyLength;

            int[] keys = new int[num_keys];

            for (int j = 0; j < shader.StaticOptions.Count; j++)
            {
                var option = shader.StaticOptions[j];
                //Set the default static option choice
                option.SetKey(ref keys[option.Bit32Index], option.DefaultChoiceIdx);
            }

            for (int j = 0; j < shader.DynamicOptions.Count; j++)
            {
                var option = shader.DynamicOptions[j];
                int ind = option.Bit32Index - option.KeyOffset;

                //Set the default dynamic option choice
                option.SetKey(ref keys[shader.StaticKeyLength + ind], option.DefaultChoiceIdx);
            }

            return keys;
        }

        public static bool IsValidProgram(ShaderModel shader, int programIndex, Dictionary<string, string> options)
        {
            //The amount of keys used per program
            int numKeysPerProgram = shader.StaticKeyLength + shader.DynamicKeyLength;

            //Static key (total * program index)
            int baseIndex = numKeysPerProgram * programIndex;

            for (int j = 0; j < shader.StaticOptions.Count; j++)
            {
                var option = shader.StaticOptions[j];
                //The options must be the same between bfres and bfsha
                if (!options.ContainsKey(option.Name))
                    continue;

                //Get key in table
                int choiceIndex = option.GetChoiceIndex(shader.KeyTable[baseIndex + option.Bit32Index]);
                if (choiceIndex > option.Choices.Count)
                    throw new Exception($"Invalid choice index in key table! Option {option.Name} choice {options[option.Name]}");

                //If the choice is not in the program, then skip the current program
                var choice = option.Choices.GetKey(choiceIndex);
                if (options[option.Name] != choice)
                    return false;
            }

            for (int j = 0; j < shader.DynamicOptions.Count; j++)
            {
                var option = shader.DynamicOptions[j];
                if (!options.ContainsKey(option.Name))
                    continue;

                int ind = option.Bit32Index - option.KeyOffset;
                int choiceIndex = option.GetChoiceIndex(shader.KeyTable[baseIndex + shader.StaticKeyLength + ind]);
                if (choiceIndex > option.Choices.Count)
                    throw new Exception($"Invalid choice index in key table!");

                var choice = option.Choices.GetKey(choiceIndex);
                if (options[option.Name] != choice)
                    return false;
            }
            return true;
        }

        //Checks if the shader option list is missing any shader option choices required for a full key search
        public static void CheckMissingShaderOptions(ShaderModel shader, Dictionary<string, string> options)
        {
            int num_keys_per_program = shader.StaticKeyLength + shader.DynamicKeyLength;
            for (int i = 0; i < shader.Programs.Count; i++)
            {
                var idx = num_keys_per_program * i;

                if (IsValidProgram(shader, i, options))
                    CheckChoices(shader, i, options);
            }
        }

        static void CheckChoices(ShaderModel shader, int programIndex, Dictionary<string, string> options)
        {
            int numKeysPerProgram = shader.StaticKeyLength + shader.DynamicKeyLength;

            var maxBit = shader.StaticOptions.Values.Max(x => x.Bit32Index);
            int baseIndex = numKeysPerProgram * programIndex;
            for (int j = 0; j < shader.StaticOptions.Count; j++)
            {
                var option = shader.StaticOptions[j];
                int choiceIndex = option.GetChoiceIndex(shader.KeyTable[baseIndex + option.Bit32Index]);
                if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                string choice = option.Choices.GetKey(choiceIndex);

                //A shader option choice not set in the lookup and not a default choice
                //This must be set for a valid lookup
                if (!options.ContainsKey(option.Name) && choice != option.DefaultChoice)
                    Console.WriteLine($"Unexpected choice value {option.Name} should be {choice}, not default {option.DefaultChoice}");
            }

            for (int j = 0; j < shader.DynamicOptions.Count; j++)
            {
                var option = shader.DynamicOptions[j];
                int ind = option.Bit32Index - option.KeyOffset;
                int choiceIndex = option.GetChoiceIndex(shader.KeyTable[baseIndex + shader.StaticKeyLength + ind]);
                if (choiceIndex > option.Choices.Count || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                string choice = option.Choices.GetKey(choiceIndex);
            }
        }
    }
}
