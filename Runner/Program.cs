using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TypeScriptGenerator;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();
            var configuration = builder.Build();
            var root = configuration["root"];

            var asmNames = new[]
            {
                "QiaoDan.Core.dll",
                "QiaoDan.ViewModels.Abstractions.dll",
                "QiaoDan.Admin.ViewModels.dll",
                "QiaoDan.OA.ViewModels.dll",
                "QiaoDan.HR.ViewModels.dll",
            };

            ModelsGenerator.create(
                asmNames.Select(n => Assembly.LoadFrom(Path.Combine(root, n))),
                "../ts.g",
                opt => 
                { 
                    opt.TypeMatcher = t => t.GetInterface("IViewModel") != null || t.IsEnum || (t.IsAbstract && t.IsSealed); 
                }
            );
        }
    }
}
