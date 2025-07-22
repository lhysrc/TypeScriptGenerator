using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Runner
{
    public class TypeScriptGeneratorTests
    {
        [Fact]
        public void Generate_Item_TypeScript()
        {
            var assembly = typeof(Item).GetTypeInfo().Assembly;
            using var tempDir = new TempDir();

            TypeScriptGenerator.ModelsGenerator.Generate(
                [assembly],
                tempDir.Path,
                opt =>
                {
                    opt.TypeFilter = t => t == typeof(Item);
                    opt.PropertyConverter = p =>
                        p.GetCustomAttribute<PropertyNameAttribute>()?.Name switch
                        {
                            { } name => name,
                            _ => null
                        };
                });

            var file = Path.Combine(tempDir.Path, "runner", "item.ts");
            Assert.True(File.Exists(file));
            var content = File.ReadAllText(file);
            Assert.Contains("export class Item", content);
            Assert.Contains("id?: string", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("hasBeenRename?: string", content);
        }

        [Fact]
        public void Generate_Status_Enum()
        {
            var assembly = typeof(Status).GetTypeInfo().Assembly;
            using var tempDir = new TempDir();

            TypeScriptGenerator.ModelsGenerator.Generate(
                [assembly],
                tempDir.Path,
                opt =>
                {
                    opt.TypeFilter = t => t == typeof(Status);
                });

            var file = Path.Combine(tempDir.Path, "runner", "status.ts");
            Assert.True(File.Exists(file));
            var content = File.ReadAllText(file);
            Assert.Contains("export enum Status", content);
            Assert.Contains("Done = 1", content);
        }

        [Fact]
        public void Generate_StaticWithNest()
        {
            var assembly = typeof(StaticWithNest).GetTypeInfo().Assembly;
            using var tempDir = new TempDir();

            TypeScriptGenerator.ModelsGenerator.Generate(
                [assembly],
                tempDir.Path,
                opt =>
                {
                    opt.TypeFilter = t => t == typeof(StaticWithNest);
                });

            var file = Path.Combine(tempDir.Path, "runner", "static-with-nest.ts");
            Assert.True(File.Exists(file));
            var content = File.ReadAllText(file);
            Assert.Contains("export module Nest", content);
            Assert.Contains("ConstDouble", content);
        }
    }

    internal sealed class TempDir : IDisposable
    {
        public string Path { get; }
        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path);
        }
        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch
            {
                // ignored
            }
        }
    }
}
