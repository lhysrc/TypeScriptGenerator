using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using TypeScriptGenerator; // Reference to the F# project

namespace TypeScriptGenerator.MSBuildTask
{
    public class GenerateTypeScriptModels : Task
    {
        [Required]
        public string[] ProjectAssemblies { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public string RootPath { get; set; }

        public string[] TypeFilterNames { get; set; } // e.g., specific interfaces or base classes

        public string[] ExcludedTypeNames { get; set; }

        public string[] PropertyNameOverrides { get; set; } // "OriginalType.OriginalProperty:NewName"

        public string[] TypeNameOverrides { get; set; } // "OriginalFullTypeName:NewTypeName"

        public bool GenerateXmlDocuentation { get; set; } = true; // Default to true

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Starting TypeScript generation...");

            if (ProjectAssemblies == null || ProjectAssemblies.Length == 0)
            {
                Log.LogError("No ProjectAssemblies specified.");
                return false;
            }

            if (string.IsNullOrEmpty(OutputPath))
            {
                Log.LogError("OutputPath is not specified.");
                return false;
            }

            var loadedAssemblies = new List<Assembly>();
            var assemblyLoadContext = new CustomAssemblyLoadContext(Log, RootPath);

            try
            {
                foreach (var assemblyPath in ProjectAssemblies)
                {
                    if (!File.Exists(assemblyPath))
                    {
                        Log.LogError($"Assembly not found: {assemblyPath}");
                        return false;
                    }
                    try
                    {
                        // Load assembly into the custom context
                        var assembly = assemblyLoadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
                        loadedAssemblies.Add(assembly);
                        Log.LogMessage(MessageImportance.Normal, $"Successfully loaded assembly: {assembly.FullName} from {assemblyPath}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Error loading assembly {assemblyPath}: {ex.Message}");
                        if (ex is ReflectionTypeLoadException rtle)
                        {
                            foreach(var loaderEx in rtle.LoaderExceptions)
                            {
                                Log.LogError($"LoaderException: {loaderEx.Message}");
                            }
                        }
                        return false;
                    }
                }

                if (!loadedAssemblies.Any())
                {
                    Log.LogError("No assemblies were successfully loaded.");
                    return false;
                }

                Action<ModelGenerateOptions> optionsAction = opts =>
                {
                    // Configure TypeFilter
                    if (TypeFilterNames != null && TypeFilterNames.Any())
                    {
                        opts.TypeFilter = new Func<Type, bool>(t =>
                        {
                            bool isIncluded = false;
                            // Include if it's an enum or static class (abstract sealed) by default, unless explicitly excluded
                            if (t.IsEnum || (t.IsAbstract && t.IsSealed)) {
                                isIncluded = true;
                            }

                            // Include if it implements any of the specified interfaces/types in TypeFilterNames
                            foreach (var filterName in TypeFilterNames)
                            {
                                if (t.GetInterfaces().Any(i => i.Name == filterName || i.FullName == filterName || i.FullName.StartsWith(filterName + "`"))) // Handle generics simply
                                {
                                    isIncluded = true;
                                    break;
                                }
                                if (t.Name == filterName || t.FullName == filterName || t.FullName.StartsWith(filterName + "`"))
                                {
                                     isIncluded = true;
                                     break;
                                }
                                var baseType = t.BaseType;
                                while(baseType != null && baseType != typeof(object))
                                {
                                    if(baseType.Name == filterName || baseType.FullName == filterName || baseType.FullName.StartsWith(filterName + "`"))
                                    {
                                        isIncluded = true;
                                        break;
                                    }
                                    baseType = baseType.BaseType;
                                }
                                if(isIncluded) break;
                            }
                            
                            // Process Exclusions
                            if (ExcludedTypeNames != null && ExcludedTypeNames.Contains(t.FullName))
                            {
                                return false; // Explicitly excluded
                            }
                            return isIncluded;
                        });
                    }
                    else
                    {
                         // Default behavior if no TypeFilterNames: include enums and static classes, exclude others unless explicitly included by other means (not applicable here yet)
                        opts.TypeFilter = new Func<Type, bool>(t =>
                        {
                             if (ExcludedTypeNames != null && ExcludedTypeNames.Contains(t.FullName))
                            {
                                return false; // Explicitly excluded
                            }
                            return t.IsEnum || (t.IsAbstract && t.IsSealed); // Default to only enums and static classes if no filter
                        });
                    }


                    // Configure PropertyFilter (default from Runner)
                    opts.PropertyFilter = new Func<PropertyInfo, bool>(p => 
                        !p.GetCustomAttributes().Any(a => a.GetType().Name == "JsonIgnoreAttribute"));

                    // Configure PropertyNameOverrides
                    if (PropertyNameOverrides != null && PropertyNameOverrides.Any())
                    {
                        var overrides = PropertyNameOverrides
                            .Select(o => o.Split(':'))
                            .Where(parts => parts.Length == 2)
                            .ToDictionary(parts => parts[0], parts => parts[1]); // Key: "Type.Property", Value: "NewName"

                        opts.PropertyConverter = new Func<PropertyInfo, string>(p =>
                        {
                            var key = $"{p.DeclaringType.FullName}.{p.Name}";
                            if (overrides.TryGetValue(key, out var newName))
                            {
                                return newName;
                            }
                            var keyShort = $"{p.DeclaringType.Name}.{p.Name}"; // Try with short type name
                             if (overrides.TryGetValue(keyShort, out newName))
                            {
                                return newName;
                            }
                            return null; // No override, generator will use default
                        });
                    }

                    // Configure TypeNameOverrides
                    if (TypeNameOverrides != null && TypeNameOverrides.Any())
                    {
                        var overrides = TypeNameOverrides
                            .Select(o => o.Split(':'))
                            .Where(parts => parts.Length == 2)
                            .ToDictionary(parts => parts[0], parts => parts[1]); // Key: "OriginalFullName", Value: "NewName"
                        
                        opts.TypeNameConverter = new Func<Type, string>(t =>
                        {
                            if (overrides.TryGetValue(t.FullName, out var newName))
                            {
                                return newName;
                            }
                            if (overrides.TryGetValue(t.Name, out newName)) // Try with short name
                            {
                                return newName;
                            }
                            return null; // No override, generator will use default
                        });
                    }
                    // GenerateXmlDocuentation is implicitly handled by the F# generator if .xml files are present.
                    // This property could be used in the future to force disable it or pass a specific path.
                };
                
                Log.LogMessage(MessageImportance.Normal, $"Output path set to: {OutputPath}");
                Directory.CreateDirectory(OutputPath); // Ensure output directory exists

                ModelsGenerator.Generate(loadedAssemblies, OutputPath, optionsAction);

                Log.LogMessage(MessageImportance.High, "TypeScript generation completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"An unexpected error occurred during TypeScript generation: {ex.ToString()}");
                return false;
            }
            finally
            {
                assemblyLoadContext.Unload(); // Unload context
            }
        }
    }

    // Custom AssemblyLoadContext to handle dependencies
    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly TaskLoggingHelper _log;
        private readonly string _rootPath;
        private readonly List<string> _probingPaths;

        public CustomAssemblyLoadContext(TaskLoggingHelper log, string rootPath) : base(isCollectible: true)
        {
            _log = log;
            _rootPath = string.IsNullOrEmpty(rootPath) ? null : Path.GetFullPath(rootPath);
            _probingPaths = new List<string>();
            if (_rootPath != null) {
                 _probingPaths.Add(_rootPath);
            }
            // _resolver = new AssemblyDependencyResolver(mainAssemblyPath); // Cannot use with LoadFromAssemblyPath directly for main assembly
            _log.LogMessage(MessageImportance.Normal, $"CustomAssemblyLoadContext initialized. RootPath: {_rootPath ?? "Not set"}");
        }
        
        public new Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            // Add directory of the current assembly to probing paths
            var assemblyDir = Path.GetDirectoryName(assemblyPath);
            if(!_probingPaths.Contains(assemblyDir))
            {
                _probingPaths.Add(assemblyDir);
                _log.LogMessage(MessageImportance.Normal, $"Added probing path: {assemblyDir}");
            }

            // Attempt to load using the base class's LoadFromAssemblyPath first.
            // This correctly loads the assembly itself and triggers Resolving event for its dependencies.
            return base.LoadFromAssemblyPath(assemblyPath);
        }


        protected override Assembly Load(AssemblyName assemblyName)
        {
            _log.LogMessage(MessageImportance.Normal, $"Attempting to resolve: {assemblyName.FullName}");

            // Try to resolve from the root path if specified
            if (_rootPath != null) {
                string assemblyPath = Path.Combine(_rootPath, assemblyName.Name + ".dll");
                 if (File.Exists(assemblyPath))
                {
                    _log.LogMessage(MessageImportance.Normal, $"Found dependency '{assemblyName.Name}' in RootPath: {assemblyPath}");
                    return LoadFromAssemblyPath(assemblyPath);
                }
            }

            // Try to resolve from the directory of already loaded assemblies (simulating _resolver)
            // This is a simplified approach. A more robust solution might involve inspecting .deps.json if available.
            foreach (var path in _probingPaths)
            {
                string assemblyPath = Path.Combine(path, assemblyName.Name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    _log.LogMessage(MessageImportance.Normal, $"Found dependency '{assemblyName.Name}' in probing path: {assemblyPath}");
                    return LoadFromAssemblyPath(assemblyPath); // Use ALC's LoadFromAssemblyPath
                }
            }
            
            _log.LogWarning($"Could not resolve dependency: {assemblyName.FullName}");
            return null; // Let .NET Core try other resolution mechanisms or fail
        }
    }
}
```
