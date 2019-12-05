using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Neo.Compiler
{
    public class Compiler
    {
        public class Assembly
        {
            public readonly byte[] Dll;
            public readonly byte[] Pdb;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dll">Library</param>
            /// <param name="pdb">PDB</param>
            public Assembly(byte[] dll, byte[] pdb)
            {
                Dll = dll;
                Pdb = pdb;
            }

            /// <summary>
            /// Create Assembly
            /// </summary>
            /// <param name="comp">Compilation</param>
            /// <returns>Assembly</returns>
            internal static Assembly Create(Compilation comp)
            {
                using (var streamDll = new MemoryStream())
                using (var streamPdb = new MemoryStream())
                {
                    var result = comp.Emit(streamDll, streamPdb);

                    if (!result.Success)
                    {
                        throw new ArgumentException();
                    }

                    streamDll.Position = 0;
                    streamPdb.Position = 0;

                    return new Assembly(streamDll.ToArray(), streamPdb.ToArray());
                }
            }
        }

        /// <summary>
        /// Build script
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="releaseMode">Release mode (default=true)</param>
        /// <returns>Assembly</returns>
        public static Assembly CompileVBProj(string filename, bool releaseMode = true)
        {
            ExtractFileAndReferences(filename, ".vb", out var files, out var references);
            return CompileVBFile(files.ToArray(), references.ToArray(), releaseMode);
        }

        /// <summary>
        /// Build script
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="releaseMode">Release mode (default=true)</param>
        /// <returns>Assembly</returns>
        public static Assembly CompileCSProj(string filename, bool releaseMode = true)
        {
            ExtractFileAndReferences(filename, ".cs", out var files, out var references);
            return CompileCSFile(files.ToArray(), references.ToArray(), releaseMode);
        }

        /// <summary>
        /// Extract information in project files
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="extension">Extension</param>
        /// <param name="files">Files</param>
        /// <param name="references">References</param>
        private static void ExtractFileAndReferences(string filename, string extension, out List<string> files, out List<string> references)
        {
            var fileInfo = new FileInfo(filename);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(filename);
            }

            // Compile csproj source

            var reader = XmlReader.Create(filename, new XmlReaderSettings() { XmlResolver = null });
            var projDefinition = XDocument.Load(reader);

            // Detect references

            references = projDefinition
               .Element("Project")
               .Elements("ItemGroup")
               .Elements("PackageReference")
               .Select(u => u.Attribute("Include").Value + ".dll")
               .ToList();

            // Detect hints

            var refs = projDefinition
                .Element("Project")
                .Elements("ItemGroup")
                .Elements("Reference")
                .Elements("HintPath")
                .Select(u => u.Value)
                .ToList();

            if (refs.Count > 0)
            {
                references.AddRange(refs);
            }

            // Detect files

            files = projDefinition
               .Element("Project")
               .Elements("ItemGroup")
               .Elements("Compile")
               .Select(u => u.Attribute("Update").Value)
               .ToList();

            files.AddRange(Directory.GetFiles(fileInfo.Directory.FullName, "*" + extension, SearchOption.AllDirectories));
            files = files.Distinct().ToList();
            reader.Dispose();
        }

        /// <summary>
        /// Build script
        /// </summary>
        /// <param name="filenames">File names</param>
        /// <param name="references">References</param>
        /// <param name="releaseMode">Release mode (default=true)</param>
        /// <returns>Assembly</returns>
        public static Assembly CompileVBFile(string[] filenames, string[] references, bool releaseMode = true)
        {
            var tree = filenames.Select(u => VisualBasicSyntaxTree.ParseText(File.ReadAllText(u))).ToArray();
            var op = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: releaseMode ? OptimizationLevel.Release : OptimizationLevel.Debug);
            return Assembly.Create(VisualBasicCompilation.Create("SmartContract", tree, CreateReferences(references), op));
        }

        /// <summary>
        /// Build script
        /// </summary>
        /// <param name="filenames">File names</param>
        /// <param name="references">References</param>
        /// <param name="releaseMode">Release mode (default=true)</param>
        /// <returns>Assembly</returns>
        public static Assembly CompileCSFile(string[] filenames, string[] references, bool releaseMode = true)
        {
            var tree = filenames.Select(u => CSharpSyntaxTree.ParseText(File.ReadAllText(u))).ToArray();
            var op = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: releaseMode ? OptimizationLevel.Release : OptimizationLevel.Debug);
            return Assembly.Create(CSharpCompilation.Create("SmartContract", tree, CreateReferences(references), op));
        }

        /// <summary>
        /// Create references
        /// </summary>
        /// <param name="references">References</param>
        /// <returns>MetadataReferences</returns>
        private static MetadataReference[] CreateReferences(params string[] references)
        {
            var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var refs = new List<MetadataReference>(new MetadataReference[]
            {
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.Numerics.dll")),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DisplayNameAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Neo.SmartContract.Framework.SmartContract).Assembly.Location),
            });
            refs.AddRange(references.Select(u => MetadataReference.CreateFromFile(u)));
            return refs.ToArray();
        }
    }
}
