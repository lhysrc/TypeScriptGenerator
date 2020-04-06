using System;
using System.Reflection;
using TypeScriptGenerator;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            ModelsGenerator.create(
                new []{Assembly.LoadFrom("../TypeScriptModelsGenerator.dll")},
                "../ts.g",
                opt=>{ opt.TypeMatcher = x => true; }
            );

            Console.WriteLine("Hello World!");
        }
    }
}
