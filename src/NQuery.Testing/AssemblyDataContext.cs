using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using MdDumper.Visualization;
using NQuery.Symbols;

namespace NQuery
{
    public static class AssemblyDataContext
    {
        public static readonly DataContext Instance = Create();
        private static PEReader PeReader;
        private static MetadataReader Reader;

        private static DataContext Create()
        {
            PeReader = new PEReader(File.OpenRead(@"some.dll"));
            Reader = PeReader.GetMetadataReader();
            var visualizer = new MetadataVisualizer(Reader);

            return DataContext.Default.AddTables(
                new SchemaTableSymbol(TableDefinition.Create("Assemblies", visualizer.GetAssemblies())),
                new SchemaTableSymbol(TableDefinition.Create("AssemblyRefs", visualizer.GetAssemblyRefs())),
                new SchemaTableSymbol(TableDefinition.Create("Blobs", visualizer.GetBlobs())),
                new SchemaTableSymbol(TableDefinition.Create("Constants", visualizer.GetConstants())),
                new SchemaTableSymbol(TableDefinition.Create("CustomAttributes", visualizer.GetCustomAttributes())),
                new SchemaTableSymbol(TableDefinition.Create("DeclSecurity", visualizer.GetDeclSecurity())),
                new SchemaTableSymbol(TableDefinition.Create("EncLogs", visualizer.GetEnCLogs())),
                new SchemaTableSymbol(TableDefinition.Create("Events", visualizer.GetEvents())),
                new SchemaTableSymbol(TableDefinition.Create("Fields", visualizer.GetFields())),
                new SchemaTableSymbol(TableDefinition.Create("Files", visualizer.GetFiles())),
                new SchemaTableSymbol(TableDefinition.Create("GenericParamConstraints", visualizer.GetGenericParamConstraints())),
                new SchemaTableSymbol(TableDefinition.Create("GenericParams", visualizer.GetGenericParams())),
                new SchemaTableSymbol(TableDefinition.Create("Guids", visualizer.GetGuids())),
                new SchemaTableSymbol(TableDefinition.Create("ManifestResources", visualizer.GetManifestResources())),
                new SchemaTableSymbol(TableDefinition.Create("MemberRefs", visualizer.GetMemberRefs())),
                new SchemaTableSymbol(TableDefinition.Create("MethodImpls", visualizer.GetMethodImpls())),
                new SchemaTableSymbol(TableDefinition.Create("Methods", visualizer.GetMethods())),
                new SchemaTableSymbol(TableDefinition.Create("MethodSpecs", visualizer.GetMethodSpecs())),
                new SchemaTableSymbol(TableDefinition.Create("Modules", visualizer.GetModule())),
                new SchemaTableSymbol(TableDefinition.Create("ModuleRefs", visualizer.GetModuleRefs())),
                new SchemaTableSymbol(TableDefinition.Create("Params", visualizer.GetParams())),
                new SchemaTableSymbol(TableDefinition.Create("Properties", visualizer.GetProperties())),
                new SchemaTableSymbol(TableDefinition.Create("StandAloneSigs", visualizer.GetStandAloneSigs())),
                new SchemaTableSymbol(TableDefinition.Create("Strings", visualizer.GetStrings())),
                new SchemaTableSymbol(TableDefinition.Create("TypeDefs", visualizer.GetTypeDefs())),
                new SchemaTableSymbol(TableDefinition.Create("TypeRefs", visualizer.GetTypeRef())),
                new SchemaTableSymbol(TableDefinition.Create("TypeSpecs", visualizer.GetTypeSpecs())),
                new SchemaTableSymbol(TableDefinition.Create("UserStrings", visualizer.GetUserStrings()))
            );
        }
    }
}