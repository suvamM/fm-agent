using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormalMethodsAgent
{
    public class DiskIOSkill
    {
        [KernelFunction]
        [Description("Return the contents of the file from the provided path.")]
        public string ReadFileContents(
        [Description("The path to the file to read.")] string filePath)
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";

            return File.ReadAllText(filePath);
        }

        [KernelFunction]
        [Description("Writes the provided content to the specified file path.")]
        public string WriteFileContents(
            [Description("The path to the file to write.")] string filePath,
            [Description("The content to write to the file.")] string content)
        {
            try
            {
                File.WriteAllText(filePath, content);
                return $"Successfully wrote to {filePath}";
            }
            catch (Exception ex)
            {
                return $"Error writing to file {filePath}: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Returns a list containing the paths of every C# file in the provided project path.")]
        private List<string> ListCSharpFiles(
            [Description("The project whose C# files needs to be enumerated")] string projectPath)
        {
            // Naive implementation: find methods with [Test] or similar attributes that use async/await or Task
            return Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories).ToList();
        }
    }
}
