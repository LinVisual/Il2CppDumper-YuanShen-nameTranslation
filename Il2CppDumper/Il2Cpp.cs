using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppDumper
{
	public abstract class Il2Cpp : BinaryStream
	{
		private Il2CppMetadataRegistration pMetadataRegistration;

		private Il2CppCodeRegistration pCodeRegistration;

		public ulong[] methodPointers;

		public ulong[] genericMethodPointers;

		public ulong[] invokerPointers;

		public ulong[] customAttributeGenerators;

		public ulong[] reversePInvokeWrappers;

		public ulong[] unresolvedVirtualCallPointers;

		private ulong[] fieldOffsets;

		public Il2CppType[] types;

		private Dictionary<ulong, Il2CppType> typeDic = new Dictionary<ulong, Il2CppType>();

		public ulong[] metadataUsages;

		private Il2CppGenericMethodFunctionsDefinitions[] genericMethodTable;

		public ulong[] genericInstPointers;

		public Il2CppGenericInst[] genericInsts;

		public Il2CppMethodSpec[] methodSpecs;

		public Dictionary<int, List<Il2CppMethodSpec>> methodDefinitionMethodSpecs = new Dictionary<int, List<Il2CppMethodSpec>>();

		public Dictionary<Il2CppMethodSpec, ulong> methodSpecGenericMethodPointers = new Dictionary<Il2CppMethodSpec, ulong>();

		private bool fieldOffsetsArePointers;

		protected long maxMetadataUsages;

		public Dictionary<string, Il2CppCodeGenModule> codeGenModules;

		public Dictionary<string, ulong[]> codeGenModuleMethodPointers;

		public Dictionary<string, Dictionary<uint, Il2CppRGCTXDefinition[]>> rgctxsDictionary;

		public abstract ulong MapVATR(ulong uiAddr);

		public abstract bool Search();

		public abstract bool PlusSearch(int methodCount, int typeDefinitionsCount);

		public abstract bool SymbolSearch();

		protected Il2Cpp(Stream stream)
			: base(stream)
		{
		}

		public void SetProperties(float version, long maxMetadataUsages)
		{
			Version = version;
			this.maxMetadataUsages = maxMetadataUsages;
		}

		protected bool AutoPlusInit(ulong codeRegistration, ulong metadataRegistration)
		{
			if (codeRegistration != 0L && metadataRegistration != 0)
			{
				if (Version == 24.2f)
				{
					pCodeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
					pMetadataRegistration = MapVATR<Il2CppMetadataRegistration>(metadataRegistration);
					genericMethodTable = MapVATR<Il2CppGenericMethodFunctionsDefinitions>(pMetadataRegistration.genericMethodTable, pMetadataRegistration.genericMethodTableCount);
					int genericMethodPointersCount = genericMethodTable.Max((Il2CppGenericMethodFunctionsDefinitions x) => x.indices.methodIndex) + 1;
					if (pCodeRegistration.reversePInvokeWrapperCount == genericMethodPointersCount)
					{
						Version = 24.3f;
						codeRegistration -= (uint)(Is32Bit ? 8 : 16);
						Console.WriteLine(string.Format("Change il2cpp version to: {0}", Version));
					}
				}
				Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
				Console.WriteLine("MetadataRegistration : {0:x}", metadataRegistration);
				Init(codeRegistration, metadataRegistration);
				return true;
			}
			Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
			Console.WriteLine("MetadataRegistration : {0:x}", metadataRegistration);
			return false;
		}

		public virtual void Init(ulong codeRegistration, ulong metadataRegistration)
		{
			pCodeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
			if (Version == 24.2f && pCodeRegistration.codeGenModules == 0)
			{
				Version = 24.3f;
				Console.WriteLine(string.Format("Change il2cpp version to: {0}", Version));
				pCodeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
			}
			pMetadataRegistration = MapVATR<Il2CppMetadataRegistration>(metadataRegistration);
			genericMethodPointers = MapVATR<ulong>(pCodeRegistration.genericMethodPointers, pCodeRegistration.genericMethodPointersCount);
			invokerPointers = MapVATR<ulong>(pCodeRegistration.invokerPointers, pCodeRegistration.invokerPointersCount);
			if (Version < 27f)
			{
				customAttributeGenerators = MapVATR<ulong>(pCodeRegistration.customAttributeGenerators, pCodeRegistration.customAttributeCount);
			}
			if (Version > 16f && Version < 27f)
			{
				metadataUsages = MapVATR<ulong>(pMetadataRegistration.metadataUsages, maxMetadataUsages);
			}
			if (Version >= 22f)
			{
				if (pCodeRegistration.reversePInvokeWrapperCount != 0)
				{
					reversePInvokeWrappers = MapVATR<ulong>(pCodeRegistration.reversePInvokeWrappers, pCodeRegistration.reversePInvokeWrapperCount);
				}
				if (pCodeRegistration.unresolvedVirtualCallCount != 0)
				{
					unresolvedVirtualCallPointers = MapVATR<ulong>(pCodeRegistration.unresolvedVirtualCallPointers, pCodeRegistration.unresolvedVirtualCallCount);
				}
			}
			genericInstPointers = MapVATR<ulong>(pMetadataRegistration.genericInsts, pMetadataRegistration.genericInstsCount);
			genericInsts = Array.ConvertAll(genericInstPointers, MapVATR<Il2CppGenericInst>);
			fieldOffsetsArePointers = Version > 21f;
			if (Version == 21f)
			{
				uint[] fieldTest = MapVATR<uint>(pMetadataRegistration.fieldOffsets, 6L);
				fieldOffsetsArePointers = fieldTest[0] == 0 && fieldTest[1] == 0 && fieldTest[2] == 0 && fieldTest[3] == 0 && fieldTest[4] == 0 && fieldTest[5] != 0;
			}
			if (fieldOffsetsArePointers)
			{
				fieldOffsets = MapVATR<ulong>(pMetadataRegistration.fieldOffsets, pMetadataRegistration.fieldOffsetsCount);
			}
			else
			{
				fieldOffsets = Array.ConvertAll(MapVATR<uint>(pMetadataRegistration.fieldOffsets, pMetadataRegistration.fieldOffsetsCount), (Converter<uint, ulong>)((uint x) => x));
			}
			ulong[] pTypes = MapVATR<ulong>(pMetadataRegistration.types, pMetadataRegistration.typesCount);
			types = new Il2CppType[pMetadataRegistration.typesCount];
			for (int i = 0; i < pMetadataRegistration.typesCount; i++)
			{
				types[i] = MapVATR<Il2CppType>(pTypes[i]);
				types[i].Init();
				typeDic.Add(pTypes[i], types[i]);
			}
			if (Version >= 24.2f)
			{
				ulong[] pCodeGenModules = MapVATR<ulong>(pCodeRegistration.codeGenModules, pCodeRegistration.codeGenModulesCount);
				codeGenModules = new Dictionary<string, Il2CppCodeGenModule>(pCodeGenModules.Length, StringComparer.Ordinal);
				codeGenModuleMethodPointers = new Dictionary<string, ulong[]>(pCodeGenModules.Length, StringComparer.Ordinal);
				rgctxsDictionary = new Dictionary<string, Dictionary<uint, Il2CppRGCTXDefinition[]>>(pCodeGenModules.Length, StringComparer.Ordinal);
				ulong[] array = pCodeGenModules;
				foreach (ulong pCodeGenModule in array)
				{
					Il2CppCodeGenModule codeGenModule = MapVATR<Il2CppCodeGenModule>(pCodeGenModule);
					string moduleName = ReadStringToNull(MapVATR(codeGenModule.moduleName));
					codeGenModules.Add(moduleName, codeGenModule);
					ulong[] methodPointers;
					try
					{
						methodPointers = MapVATR<ulong>(codeGenModule.methodPointers, codeGenModule.methodPointerCount);
					}
					catch
					{
						methodPointers = new ulong[codeGenModule.methodPointerCount];
					}
					codeGenModuleMethodPointers.Add(moduleName, methodPointers);
					Dictionary<uint, Il2CppRGCTXDefinition[]> rgctxsDefDictionary = new Dictionary<uint, Il2CppRGCTXDefinition[]>();
					rgctxsDictionary.Add(moduleName, rgctxsDefDictionary);
					if (codeGenModule.rgctxsCount > 0)
					{
						Il2CppRGCTXDefinition[] rgctxs = MapVATR<Il2CppRGCTXDefinition>(codeGenModule.rgctxs, codeGenModule.rgctxsCount);
						Il2CppTokenRangePair[] rgctxRanges = MapVATR<Il2CppTokenRangePair>(codeGenModule.rgctxRanges, codeGenModule.rgctxRangesCount);
						Il2CppTokenRangePair[] array2 = rgctxRanges;
						foreach (Il2CppTokenRangePair rgctxRange in array2)
						{
							Il2CppRGCTXDefinition[] rgctxDefs = new Il2CppRGCTXDefinition[rgctxRange.range.length];
							Array.Copy(rgctxs, rgctxRange.range.start, rgctxDefs, 0, rgctxRange.range.length);
							rgctxsDefDictionary.Add(rgctxRange.token, rgctxDefs);
						}
					}
				}
			}
			else
			{
				this.methodPointers = MapVATR<ulong>(pCodeRegistration.methodPointers, pCodeRegistration.methodPointersCount);
			}
			genericMethodTable = MapVATR<Il2CppGenericMethodFunctionsDefinitions>(pMetadataRegistration.genericMethodTable, pMetadataRegistration.genericMethodTableCount);
			methodSpecs = MapVATR<Il2CppMethodSpec>(pMetadataRegistration.methodSpecs, pMetadataRegistration.methodSpecsCount);
			Il2CppGenericMethodFunctionsDefinitions[] array3 = genericMethodTable;
			foreach (Il2CppGenericMethodFunctionsDefinitions table in array3)
			{
				Il2CppMethodSpec methodSpec = methodSpecs[table.genericMethodIndex];
				int methodDefinitionIndex = methodSpec.methodDefinitionIndex;
				List<Il2CppMethodSpec> list;
				if (!methodDefinitionMethodSpecs.TryGetValue(methodDefinitionIndex, out list))
				{
					list = new List<Il2CppMethodSpec>();
					methodDefinitionMethodSpecs.Add(methodDefinitionIndex, list);
				}
				list.Add(methodSpec);
				methodSpecGenericMethodPointers.Add(methodSpec, genericMethodPointers[table.indices.methodIndex]);
			}
		}

		public T MapVATR<T>(ulong addr) where T : new()
		{
			return ReadClass<T>(MapVATR(addr));
		}

		public T[] MapVATR<T>(ulong addr, long count) where T : new()
		{
			return ReadClassArray<T>(MapVATR(addr), count);
		}

		public int GetFieldOffsetFromIndex(int typeIndex, int fieldIndexInType, int fieldIndex, bool isValueType, bool isStatic)
		{
			try
			{
				int offset = -1;
				if (fieldOffsetsArePointers)
				{
					ulong ptr = fieldOffsets[typeIndex];
					if (ptr != 0)
					{
						base.Position = MapVATR(ptr) + (ulong)(4L * (long)fieldIndexInType);
						offset = ReadInt32();
					}
				}
				else
				{
					offset = (int)fieldOffsets[fieldIndex];
				}
				if (offset > 0 && isValueType && !isStatic)
				{
					offset = ((!Is32Bit) ? (offset - 16) : (offset - 8));
				}
				return offset;
			}
			catch
			{
				return -1;
			}
		}

		public Il2CppType GetIl2CppType(ulong pointer)
		{
			return typeDic[pointer];
		}

		public ulong GetMethodPointer(string imageName, Il2CppMethodDefinition methodDef)
		{
			if (Version >= 24.2f)
			{
				uint methodToken = methodDef.token;
				ulong[] ptrs = codeGenModuleMethodPointers[imageName];
				uint methodPointerIndex = methodToken & 0xFFFFFF;
				return ptrs[methodPointerIndex - 1];
			}
			int methodIndex = methodDef.methodIndex;
			if (methodIndex >= 0)
			{
				return methodPointers[methodIndex];
			}
			return 0uL;
		}

		public virtual ulong GetRVA(ulong pointer)
		{
			return pointer;
		}
	}
}
