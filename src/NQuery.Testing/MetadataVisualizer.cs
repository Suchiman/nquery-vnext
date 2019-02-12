using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MdDumper.Visualization
{
    public sealed class MetadataVisualizer
    {
        private readonly IReadOnlyList<MetadataReader> readers;
        private readonly MetadataAggregator aggregator;

        // enc map for each delta reader
        private readonly ImmutableArray<ImmutableArray<EntityHandle>> encMaps;

        private MetadataReader reader;

        private MetadataVisualizer(IReadOnlyList<MetadataReader> readers)
        {
            this.readers = readers;

            if (readers.Count > 1)
            {
                var deltaReaders = new List<MetadataReader>(readers.Skip(1));
                this.aggregator = new MetadataAggregator(readers[0], deltaReaders);

                this.encMaps = ImmutableArray.CreateRange(deltaReaders.Select(reader => ImmutableArray.CreateRange(reader.GetEditAndContinueMapEntries())));
            }
        }

        public MetadataVisualizer(MetadataReader reader)
            : this(new[] { reader })
        {
            this.reader = reader;
        }

        public void Visualize(int generation = -1)
        {
            this.reader = (generation >= 0) ? readers[generation] : readers.Last();

            GetModule();
            GetTypeRef();
            GetTypeDefs();
            GetFields();
            GetMethods();
            GetParams();
            GetMemberRefs();
            GetConstants();
            GetCustomAttributes();
            GetDeclSecurity();
            GetStandAloneSigs();
            GetEvents();
            GetProperties();
            GetMethodImpls();
            GetModuleRefs();
            GetTypeSpecs();
            GetEnCLogs();
            WriteEnCMap();
            GetAssemblies();
            GetAssemblyRefs();
            GetFiles();
            WriteForwarders();
            GetExportedTypes();
            GetManifestResources();
            GetGenericParams();
            GetMethodSpecs();
            GetGenericParamConstraints();

            GetUserStrings();
            GetStrings();
            GetBlobs();
            GetGuids();
        }

        private bool IsDelta
        {
            get
            {
                return reader.GetTableRowCount(TableIndex.EncLog) > 0;
            }
        }

        private void AddHeader(params string[] header)
        {
            //Debug.Assert(pendingRows.Count == 0);
            //pendingRows.Add(header);
        }

        private void AddRow(params string[] fields)
        {
            //Debug.Assert(pendingRows.Count > 0 && pendingRows.Last().Length == fields.Length);
            //pendingRows.Add(fields);
        }

        private void WriteRows(string title)
        {
            //Debug.Assert(pendingRows.Count > 0);

            //if (pendingRows.Count == 1)
            //{
            //    pendingRows.Clear();
            //    return;
            //}

            //writer.Write(title);
            //writer.WriteLine();

            //string columnSeparator = "  ";
            //int rowNumberWidth = pendingRows.Count.ToString("x").Length;

            //int[] columnWidths = new int[pendingRows.First().Length];
            //foreach (var row in pendingRows)
            //{
            //    for (int c = 0; c < row.Length; c++)
            //    {
            //        columnWidths[c] = Math.Max(columnWidths[c], row[c].Length + columnSeparator.Length);
            //    }
            //}

            //int tableWidth = columnWidths.Sum() + columnWidths.Length;
            //string horizontalSeparator = new string('=', tableWidth);

            //for (int r = 0; r < pendingRows.Count; r++)
            //{
            //    var row = pendingRows[r];

            //    // header
            //    if (r == 0)
            //    {
            //        writer.WriteLine(horizontalSeparator);
            //        writer.Write(new string(' ', rowNumberWidth + 2));
            //    }
            //    else
            //    {
            //        string rowNumber = r.ToString("x");
            //        writer.Write(new string(' ', rowNumberWidth - rowNumber.Length));
            //        writer.Write(rowNumber);
            //        writer.Write(": ");
            //    }

            //    for (int c = 0; c < row.Length; c++)
            //    {
            //        var field = row[c];

            //        writer.Write(field);
            //        writer.Write(new string(' ', columnWidths[c] - field.Length));
            //    }

            //    writer.WriteLine();

            //    // header
            //    if (r == 0)
            //    {
            //        writer.WriteLine(horizontalSeparator);
            //    }
            //}

            //writer.WriteLine();
            //pendingRows.Clear();
        }

        private Handle GetAggregateHandle(Handle generationHandle, int generation)
        {
            var encMap = encMaps[generation - 1];

            int start, count;
            if (!TryGetHandleRange(encMap, generationHandle.Kind, out start, out count))
            {
                throw new BadImageFormatException(string.Format("EncMap is missing record for {0:8X}.", MetadataTokens.GetToken(generationHandle)));
            }

            return encMap[start + reader.GetRowNumber((EntityHandle)generationHandle) - 1];
        }

        private static bool TryGetHandleRange(ImmutableArray<EntityHandle> handles, HandleKind handleType, out int start, out int count)
        {
            TableIndex tableIndex;
            MetadataTokens.TryGetTableIndex(handleType, out tableIndex);

            int mapIndex = handles.BinarySearch(MetadataTokens.EntityHandle(tableIndex, 0), TokenTypeComparer.Instance);
            if (mapIndex < 0)
            {
                start = 0;
                count = 0;
                return false;
            }

            int s = mapIndex;
            while (s >= 0 && handles[s].Kind == handleType)
            {
                s--;
            }

            int e = mapIndex;
            while (e < handles.Length && handles[e].Kind == handleType)
            {
                e++;
            }

            start = s + 1;
            count = e - start;
            return true;
        }

        //private Method GetMethod(MethodHandle handle)
        //{
        //    return Get(handle, (reader, h) => reader.GetMethod((MethodHandle)h));
        //}

        //private BlobHandle GetLocalSignature(LocalSignatureHandle handle)
        //{
        //    return Get(handle, (reader, h) => reader.GetLocalSignature((LocalSignatureHandle)h));
        //}

        private TEntity Get<TEntity>(Handle handle, Func<MetadataReader, Handle, TEntity> getter)
        {
            if (aggregator != null)
            {
                int generation;
                var generationHandle = aggregator.GetGenerationHandle(handle, out generation);
                return getter(readers[generation], generationHandle);
            }
            else
            {
                return getter(this.reader, handle);
            }
        }

        private string Literal(StringHandle handle)
        {
            return Literal(handle, (r, h) => "'" + r.GetString((StringHandle)h) + "'");
        }

        private string Literal(NamespaceDefinitionHandle handle)
        {
            return Literal(handle, (r, h) => "'" + r.GetString((NamespaceDefinitionHandle)h) + "'");
        }

        private string Literal(GuidHandle handle)
        {
            return Literal(handle, (r, h) => "{" + r.GetGuid((GuidHandle)h) + "}");
        }

        private string Literal(BlobHandle handle)
        {
            return Literal(handle, (r, h) => BitConverter.ToString(r.GetBlobBytes((BlobHandle)h)));
        }

        private string Literal(Handle handle, Func<MetadataReader, Handle, string> getValue)
        {
            if (handle.IsNil)
            {
                return "nil";
            }

            if (aggregator != null)
            {
                int generation;
                Handle generationHandle = aggregator.GetGenerationHandle(handle, out generation);

                var generationReader = readers[generation];
                string value = getValue(generationReader, generationHandle);
                int offset = generationReader.GetHeapOffset(handle);
                int generationOffset = generationReader.GetHeapOffset(generationHandle);

                if (offset == generationOffset)
                {
                    return string.Format("{0} (#{1:x})", value, offset);
                }
                else
                {
                    return string.Format("{0} (#{1:x}/{2:x})", value, offset, generationOffset);
                }
            }

            if (IsDelta)
            {
                // we can't resolve the literal without aggregate reader
                return string.Format("#{0:x}", reader.GetHeapOffset(handle));
            }

            return string.Format("{1:x} (#{0:x})", reader.GetHeapOffset(handle), getValue(reader, handle));
        }

        private string Hex(ushort value)
        {
            return "0x" + value.ToString("X4");
        }

        private string Hex(int value)
        {
            return "0x" + value.ToString("X8");
        }

        public string Token(Handle handle, bool displayTable = true)
        {
            if (handle.IsNil)
            {
                return "nil";
            }

            TableIndex table;
            if (displayTable && MetadataTokens.TryGetTableIndex(handle.Kind, out table))
            {
                return string.Format("0x{0:x8} ({1})", reader.GetToken(handle), table);
            }
            else
            {
                return string.Format("0x{0:x8}", reader.GetToken(handle));
            }
        }

        private static string EnumValue<T>(object value) where T : IEquatable<T>
        {
            T integralValue = (T)value;
            if (integralValue.Equals(default(T)))
            {
                return "0";
            }

            return string.Format("0x{0:x8} ({1})", integralValue, value);
        }

        // TODO (tomat): handle collections should implement IReadOnlyCollection<Handle>
        private string TokenRange<THandle>(IReadOnlyCollection<THandle> handles, Func<THandle, Handle> conversion)
        {
            var genericHandles = handles.Select(conversion);
            return (handles.Count == 0) ? "nil" : Token(genericHandles.First(), displayTable: false) + "-" + Token(genericHandles.Last(), displayTable: false);
        }

        public string TokenList(IReadOnlyCollection<InterfaceImplementationHandle> handles, bool displayTable = false)
        {
            if (handles.Count == 0)
            {
                return "nil";
            }

            return string.Join(", ", handles.Select(h => Token(h, displayTable)));
        }

        public class Module
        {
            public int Generation { get; set; }
            public string Name { get; set; }
            public string Mvid { get; set; }
            public string GenerationId { get; set; }
            public string BaseGenerationId { get; set; }
        }

        public IEnumerable<Module> GetModule()
        {
            var def = reader.GetModuleDefinition();

            yield return new Module
            {
                Generation = def.Generation,
                Name = Literal(def.Name),
                Mvid = Literal(def.Mvid),
                GenerationId = Literal(def.GenerationId),
                BaseGenerationId = Literal(def.BaseGenerationId)
            };
        }

        public class TypeRef
        {
            public string Scope { get; set; }
            public string Name { get; set; }
            public string Namespace { get; set; }
        }

        public IEnumerable<TypeRef> GetTypeRef()
        {
            foreach (var handle in reader.TypeReferences)
            {
                var entry = reader.GetTypeReference(handle);

                yield return new TypeRef
                {
                    Scope = Token(entry.ResolutionScope),
                    Name = Literal(entry.Name),
                    Namespace = Literal(entry.Namespace)
                };
            }
        }

        public class TypeDef
        {
            public string Scope { get; set; }
            public string Name { get; set; }
            public string Namespace { get; set; }
            public string EnclosingType { get; set; }
            public string BaseType { get; set; }
            public string Interfaces { get; set; }
            public string Fields { get; set; }
            public string Methods { get; set; }
            public string Attributes { get; set; }
            public string ClassSize { get; set; }
            public string PackingSize { get; set; }
        }

        public IEnumerable<TypeDef> GetTypeDefs()
        {
            foreach (var handle in reader.TypeDefinitions)
            {
                var entry = reader.GetTypeDefinition(handle);

                var layout = entry.GetLayout();

                yield return new TypeDef
                {
                    Name = Literal(entry.Name),
                    Namespace = Literal(entry.Namespace),
                    EnclosingType = Token(entry.GetDeclaringType()),
                    BaseType = Token(entry.BaseType),
                    Interfaces = TokenList(entry.GetInterfaceImplementations()),
                    Fields = TokenRange(entry.GetFields(), h => h),
                    Methods = TokenRange(entry.GetMethods(), h => h),
                    Attributes = EnumValue<int>(entry.Attributes),
                    ClassSize = layout.IsDefault ? "n/a" : layout.Size.ToString(),
                    PackingSize = layout.IsDefault ? "n/a" : layout.PackingSize.ToString()
                };
            }
        }

        public class Field
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public string Attributes { get; set; }
            public string Marshalling { get; set; }
            public int Offset { get; set; }
            public int RVA { get; set; }
        }

        public IEnumerable<Field> GetFields()
        {
            foreach (var handle in reader.FieldDefinitions)
            {
                var entry = reader.GetFieldDefinition(handle);

                int offset = entry.GetOffset();

                yield return new Field
                {
                    Name = Literal(entry.Name),
                    Signature = Literal(entry.Signature),
                    Attributes = EnumValue<int>(entry.Attributes),
                    Marshalling = Literal(entry.GetMarshallingDescriptor()),
                    Offset = offset,
                    RVA = entry.GetRelativeVirtualAddress()
                };
            }
        }

        public class Method
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public int RVA { get; set; }
            public string Parameters { get; set; }
            public string GenericParameters { get; set; }
            public string ImplAttributes { get; set; }
            public string Attributes { get; set; }
            public string ImportAttributes { get; set; }
            public string ImportName { get; set; }
            public string ImportModule { get; set; }
        }

        public IEnumerable<Method> GetMethods()
        {
            foreach (var handle in reader.MethodDefinitions)
            {
                var entry = reader.GetMethodDefinition(handle);
                var import = entry.GetImport();

                yield return new Method
                {
                    Name = Literal(entry.Name),
                    Signature = Literal(entry.Signature),
                    RVA = entry.RelativeVirtualAddress,
                    Parameters = TokenRange(entry.GetParameters(), h => h),
                    GenericParameters = TokenRange(entry.GetGenericParameters(), h => h),
                    ImplAttributes = EnumValue<int>(entry.Attributes),    // TODO: we need better visualizer than the default enum
                    Attributes = EnumValue<int>(entry.ImplAttributes),
                    ImportAttributes = EnumValue<short>(import.Attributes),
                    ImportName = Literal(import.Name),
                    ImportModule = Token(import.Module)
                };
            }
        }

        public class Param
        {
            public string Name { get; set; }
            public int SequenceNumber { get; set; }
            public string Attributes { get; set; }
            public string Marshalling { get; set; }
        }

        public IEnumerable<Param> GetParams()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.Param); i <= count; i++)
            {
                var entry = reader.GetParameter(MetadataTokens.ParameterHandle(i));

                yield return new Param
                {
                    Name = Literal(entry.Name),
                    SequenceNumber = entry.SequenceNumber,
                    Attributes = EnumValue<int>(entry.Attributes),
                    Marshalling = Literal(entry.GetMarshallingDescriptor())
                };
            }
        }

        public class MemberRef
        {
            public string Parent { get; set; }
            public string Name { get; set; }
            public string Signature { get; set; }
        }

        public IEnumerable<MemberRef> GetMemberRefs()
        {
            foreach (var handle in reader.MemberReferences)
            {
                var entry = reader.GetMemberReference(handle);

                yield return new MemberRef
                {
                    Parent = Token(entry.Parent),
                    Name = Literal(entry.Name),
                    Signature = Literal(entry.Signature)
                };
            }
        }

        public class ClrConstant
        {
            public string Parent { get; set; }
            public string TypeCode { get; set; }
            public string Value { get; set; }
        }

        public IEnumerable<ClrConstant> GetConstants()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.Constant); i <= count; i++)
            {
                var entry = reader.GetConstant(MetadataTokens.ConstantHandle(i));

                yield return new ClrConstant
                {
                    Parent = Token(entry.Parent),
                    TypeCode = EnumValue<byte>(entry.TypeCode),
                    Value = Literal(entry.Value)
                };
            }
        }

        public class ClrCustomAttribute
        {
            public string Parent { get; set; }
            public string Constructor { get; set; }
            public string Value { get; set; }
        }

        public IEnumerable<ClrCustomAttribute> GetCustomAttributes()
        {
            foreach (var handle in reader.CustomAttributes)
            {
                var entry = reader.GetCustomAttribute(handle);

                yield return new ClrCustomAttribute
                {
                    Parent = Token(entry.Parent),
                    Constructor = Token(entry.Constructor),
                    Value = Literal(entry.Value)
                };
            }
        }

        public class DeclSecurity
        {
            public string Parent { get; set; }
            public string PermissionSet { get; set; }
            public string Action { get; set; }
        }

        public IEnumerable<DeclSecurity> GetDeclSecurity()
        {
            foreach (var handle in reader.DeclarativeSecurityAttributes)
            {
                var entry = reader.GetDeclarativeSecurityAttribute(handle);

                yield return new DeclSecurity
                {
                    Parent = Token(entry.Parent),
                    PermissionSet = Literal(entry.PermissionSet),
                    Action = EnumValue<short>(entry.Action)
                };
            }
        }

        public class StandAloneSig
        {
            public string Signature { get; set; }
        }

        public IEnumerable<StandAloneSig> GetStandAloneSigs()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.StandAloneSig); i <= count; i++)
            {
                var value = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(i));

                yield return new StandAloneSig
                {
                    Signature = Literal(value.Signature)
                };
            }
        }

        public class Event
        {
            public string Name { get; set; }
            public string Add { get; set; }
            public string Remove { get; set; }
            public string Fire { get; set; }
            public string Attributes { get; set; }
        }

        public IEnumerable<Event> GetEvents()
        {
            foreach (var handle in reader.EventDefinitions)
            {
                var entry = reader.GetEventDefinition(handle);
                var accessors = entry.GetAccessors();

                yield return new Event
                {
                    Name = Literal(entry.Name),
                    Add = Token(accessors.Adder),
                    Remove = Token(accessors.Remover),
                    Fire = Token(accessors.Raiser),
                    Attributes = EnumValue<int>(entry.Attributes)
                };
            }
        }

        public class Property
        {
            public string Name { get; set; }
            public string Get { get; set; }
            public string Set { get; set; }
            public string Attributes { get; set; }
        }

        public IEnumerable<Property> GetProperties()
        {
            foreach (var handle in reader.PropertyDefinitions)
            {
                var entry = reader.GetPropertyDefinition(handle);
                var accessors = entry.GetAccessors();

                yield return new Property
                {
                    Name = Literal(entry.Name),
                    Get = Token(accessors.Getter),
                    Set = Token(accessors.Setter),
                    Attributes = EnumValue<int>(entry.Attributes)
                };
            }
        }

        public class MethodImpl
        {
            public string Type { get; set; }
            public string Body { get; set; }
            public string Declaration { get; set; }
        }

        public IEnumerable<MethodImpl> GetMethodImpls()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.MethodImpl); i <= count; i++)
            {
                var entry = reader.GetMethodImplementation(MetadataTokens.MethodImplementationHandle(i));

                yield return new MethodImpl
                {
                    Type = Token(entry.Type),
                    Body = Token(entry.MethodBody),
                    Declaration = Token(entry.MethodDeclaration)
                };
            }
        }

        public class ModuleRef
        {
            public string Name { get; set; }
        }

        public IEnumerable<ModuleRef> GetModuleRefs()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.ModuleRef); i <= count; i++)
            {
                var value = reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i));
                yield return new ModuleRef { Name = Literal(value.Name) };
            }
        }

        public class TypeSec
        {
            public string Signature { get; set; }
        }

        public IEnumerable<TypeSec> GetTypeSpecs()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.TypeSpec); i <= count; i++)
            {
                var value = reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(i));
                yield return new TypeSec { Signature = Literal(value.Signature) };
            }
        }

        public class EncLog
        {
            public string Entity { get; internal set; }
            public string Operation { get; internal set; }
        }

        public IEnumerable<EncLog> GetEnCLogs()
        {
            foreach (var entry in reader.GetEditAndContinueLogEntries())
            {
                yield return new EncLog
                {
                    Entity = Token(entry.Handle),
                    Operation = EnumValue<int>(entry.Operation)
                };
            }
        }

        public void WriteEnCMap()
        {
            if (aggregator != null)
            {
                AddHeader("Entity", "Gen", "Row", "Edit");
            }
            else
            {
                AddHeader("Entity");
            }


            foreach (var entry in reader.GetEditAndContinueMapEntries())
            {
                if (aggregator != null)
                {
                    int generation;
                    Handle primary = aggregator.GetGenerationHandle(entry, out generation);
                    bool isUpdate = readers[generation] != reader;

                    var primaryModule = readers[generation].GetModuleDefinition();

                    AddRow(
                        Token(entry),
                        primaryModule.Generation.ToString(),
                        "0x" + reader.GetRowNumber((EntityHandle)primary).ToString("x6"),
                        isUpdate ? "update" : "add");
                }
                else
                {
                    AddRow(Token(entry));
                }
            }

            WriteRows("EnC Map (0x1f):");
        }

        public class ClrAssembly
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Culture { get; set; }
            public string PublicKey { get; set; }
            public string Flags { get; set; }
            public string HashAlgorithm { get; set; }
        }

        public IEnumerable<ClrAssembly> GetAssemblies()
        {
            if (reader.IsAssembly)
            {
                var entry = reader.GetAssemblyDefinition();

                yield return new ClrAssembly
                {
                    Name = Literal(entry.Name),
                    Version = entry.Version.Major + "." + entry.Version.Minor + "." + entry.Version.Revision + "." + entry.Version.Build,
                    Culture = Literal(entry.Culture),
                    PublicKey = Literal(entry.PublicKey),
                    Flags = EnumValue<int>(entry.Flags),
                    HashAlgorithm = EnumValue<int>(entry.HashAlgorithm)
                };
            }
        }

        public class AssemblyRef
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Culture { get; set; }
            public string PublicKeyOrToken { get; set; }
            public string Flags { get; set; }
        }

        public IEnumerable<AssemblyRef> GetAssemblyRefs()
        {
            foreach (var handle in reader.AssemblyReferences)
            {
                var entry = reader.GetAssemblyReference(handle);

                yield return new AssemblyRef
                {
                    Name = Literal(entry.Name),
                    Version = entry.Version.Major + "." + entry.Version.Minor + "." + entry.Version.Revision + "." + entry.Version.Build,
                    Culture = Literal(entry.Culture),
                    PublicKeyOrToken = Literal(entry.PublicKeyOrToken),
                    Flags = EnumValue<int>(entry.Flags)
                };
            }
        }

        public class ClrAssemblyFile
        {
            public string Name { get; set; }
            public string Metadata { get; set; }
            public string HashValue { get; set; }
        }

        public IEnumerable<ClrAssemblyFile> GetFiles()
        {
            foreach (var handle in reader.AssemblyFiles)
            {
                var entry = reader.GetAssemblyFile(handle);

                yield return new ClrAssemblyFile
                {
                    Name = Literal(entry.Name),
                    Metadata = entry.ContainsMetadata ? "Yes" : "No",
                    HashValue = Literal(entry.HashValue)
                };
            }
        }

        public void WriteForwarders()
        {
            AddHeader(
                "Name",
                "Namespace",
                "Assembly"
            );

            foreach (var handle in reader.ExportedTypes)
            {
                var entry = reader.GetExportedType(handle);
                AddRow(
                    Literal(entry.Name),
                    Literal(entry.Namespace),
                    Token(entry.Implementation));
            }

            WriteRows("ExportedType - forwarders (0x27):");
        }

        public class ClrExportedType
        {
            public string Name { get; set; }
            public string Namespace { get; set; }
            public string Implementation { get; set; }
            public bool IsForwarder { get; set; }
            public string Attributes { get; set; }
        }

        private IEnumerable<ClrExportedType> GetExportedTypes()
        {
            foreach (var handle in reader.ExportedTypes)
            {
                var entry = reader.GetExportedType(handle);
                yield return new ClrExportedType
                {
                    Name = Literal(entry.Name),
                    Namespace = Literal(entry.Namespace),
                    Implementation = Token(entry.Implementation),
                    IsForwarder = entry.IsForwarder,
                    Attributes = entry.Attributes.ToString()
                };
            }
        }

        public class ClrManifestResource
        {
            public string Name { get; set; }
            public string Attributes { get; set; }
            public long Offset { get; set; }
            public string Implementation { get; set; }
        }

        public IEnumerable<ClrManifestResource> GetManifestResources()
        {
            foreach (var handle in reader.ManifestResources)
            {
                var entry = reader.GetManifestResource(handle);

                yield return new ClrManifestResource
                {
                    Name = Literal(entry.Name),
                    Attributes = entry.Attributes.ToString(),
                    Offset = entry.Offset,
                    Implementation = Token(entry.Implementation)
                };
            }
        }

        public class GenericParam
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string Attributes { get; set; }
            public string TypeConstraints { get; set; }
        }

        public IEnumerable<GenericParam> GetGenericParams()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.GenericParam); i <= count; i++)
            {
                var entry = reader.GetGenericParameter(MetadataTokens.GenericParameterHandle(i));

                yield return new GenericParam
                {
                    Name = Literal(entry.Name),
                    Index = entry.Index,
                    Attributes = EnumValue<int>(entry.Attributes),
                    TypeConstraints = TokenRange(entry.GetConstraints(), h => h)
                };
            }
        }

        public class MethodSpec
        {
            public string Method { get; set; }
            public string Signature { get; set; }
        }

        public IEnumerable<MethodSpec> GetMethodSpecs()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.MethodSpec); i <= count; i++)
            {
                var entry = reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(i));

                yield return new MethodSpec
                {
                    Method = Token(entry.Method),
                    Signature = Literal(entry.Signature)
                };
            }
        }

        public class GenericParamConstraint
        {
            public string Parent { get; set; }
            public string Type { get; set; }
        }

        public IEnumerable<GenericParamConstraint> GetGenericParamConstraints()
        {
            for (int i = 1, count = reader.GetTableRowCount(TableIndex.GenericParamConstraint); i <= count; i++)
            {
                var entry = reader.GetGenericParameterConstraint(MetadataTokens.GenericParameterConstraintHandle(i));

                yield return new GenericParamConstraint
                {
                    Parent = Token(entry.Parameter),
                    Type = Token(entry.Type)
                };
            }
        }

        public class UserString
        {
            public string Value { get; set; }
        }

        public IEnumerable<UserString> GetUserStrings()
        {
            int size = reader.GetHeapSize(HeapIndex.UserString);
            if (size == 0)
            {
                yield break;
            }

            var handle = MetadataTokens.UserStringHandle(0);
            do
            {
                string value = reader.GetUserString(handle);
                yield return new UserString
                {
                    Value = value
                };
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);
        }

        public class ClrString
        {
            public string Value { get; set; }
        }

        public IEnumerable<ClrString> GetStrings()
        {
            int size = reader.GetHeapSize(HeapIndex.String);
            if (size == 0)
            {
                yield break;
            }

            var handle = MetadataTokens.StringHandle(0);
            do
            {
                string value = reader.GetString(handle);
                yield return new ClrString
                {
                    Value = value
                };
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);
        }

        public class Blob
        {
            public byte[] Value { get; set; }
        }

        public IEnumerable<Blob> GetBlobs()
        {
            int size = reader.GetHeapSize(HeapIndex.Blob);
            if (size == 0)
            {
                yield break;
            }

            var handle = MetadataTokens.BlobHandle(0);
            do
            {
                byte[] value = reader.GetBlobBytes(handle);
                yield return new Blob
                {
                    Value = value
                };
                handle = reader.GetNextHandle(handle);
            }
            while (!handle.IsNil);
        }

        public class ClrGuid
        {
            public Guid Value { get; set; }
        }

        public IEnumerable<ClrGuid> GetGuids()
        {
            int size = reader.GetHeapSize(HeapIndex.Guid);
            if (size == 0)
            {
                yield break;
            }

            int i = 1;
            while (i <= size / 16)
            {
                Guid value = reader.GetGuid(MetadataTokens.GuidHandle(i));
                yield return new ClrGuid
                {
                    Value = value
                };
                i++;
            }
        }

        private sealed class TokenTypeComparer : IComparer<EntityHandle>
        {
            public static readonly TokenTypeComparer Instance = new TokenTypeComparer();

            public int Compare(EntityHandle x, EntityHandle y)
            {
                return x.Kind.CompareTo(y.Kind);
            }
        }
    }
}