using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Il2CppDumper
{
	public sealed class Metadata : BinaryStream
	{
		public Il2CppGlobalMetadataHeader header;

		public Il2CppImageDefinition[] imageDefs;

		public Il2CppTypeDefinition[] typeDefs;

		public Il2CppMethodDefinition[] methodDefs;

		public Il2CppParameterDefinition[] parameterDefs;

		public Il2CppFieldDefinition[] fieldDefs;

		private Dictionary<int, Il2CppFieldDefaultValue> fieldDefaultValuesDic;

		private Dictionary<int, Il2CppParameterDefaultValue> parameterDefaultValuesDic;

		public Il2CppPropertyDefinition[] propertyDefs;

		public Il2CppCustomAttributeTypeRange[] attributeTypeRanges;

		private Dictionary<Il2CppImageDefinition, Dictionary<uint, int>> attributeTypeRangesDic;

		private Il2CppStringLiteral[] stringLiterals;

		private Il2CppMetadataUsageList[] metadataUsageLists;

		private Il2CppMetadataUsagePair[] metadataUsagePairs;

		public int[] attributeTypes;

		public int[] interfaceIndices;

		public Dictionary<uint, SortedDictionary<uint, uint>> metadataUsageDic;

		public long maxMetadataUsages;

		public int[] nestedTypeIndices;

		public Il2CppEventDefinition[] eventDefs;

		public Il2CppGenericContainer[] genericContainers;

		public Il2CppFieldRef[] fieldRefs;

		public Il2CppGenericParameter[] genericParameters;

		public int[] constraintIndices;

		public uint[] vtableMethods;

		public Il2CppRGCTXDefinition[] rgctxEntries;

		private Dictionary<uint, string> stringCache = new Dictionary<uint, string>();

		public ulong Address;

		private byte[] stringDecryptionBlob = null;

		private Dictionary<string, string> nameTranslation = new Dictionary<string, string>();

		private Regex nameTranslationMemberRegex = new Regex(".+\\/<(.+)>", RegexOptions.Compiled);

		private Dictionary<uint, bool> indexlist = new Dictionary<uint, bool>();

		public Metadata(Stream stream, MetadataDecryption.StringDecryptionData decData, string nameTranslationPath)
			: base(stream)
		{
			Version = 24f;
			header = ReadClass<Il2CppGlobalMetadataHeader>(0uL);
			stringDecryptionBlob = decData.stringDecryptionBlob;
			header.stringCount ^= (int)decData.stringCountXor;
			header.stringOffset ^= decData.stringOffsetXor;
			header.stringLiteralOffset ^= decData.stringLiteralOffsetXor;
			header.stringLiteralDataCount ^= (int)decData.stringLiteralDataCountXor;
			header.stringLiteralDataOffset ^= decData.stringLiteralDataOffsetXor;
			if (nameTranslationPath != null)
			{
				string[] nameTranslationFile = File.ReadAllLines(nameTranslationPath);
				string[] array = nameTranslationFile;
				foreach (string line in array)
				{
					if (!line.StartsWith("#"))
					{
						string[] split = line.Split('⇨');
						if (split.Length != 2)
						{
							throw new NotSupportedException(string.Format("unexpected split.Length {0}", split.Length));
						}
						nameTranslation.Add(split[0], split[1]);
					}
				}
				Console.WriteLine(string.Format("Loaded {0} lookup values", nameTranslation.Count));
			}
			imageDefs = ReadMetadataClassArray<Il2CppImageDefinition>(header.imagesOffset, header.imagesCount);
			typeDefs = ReadMetadataClassArray<Il2CppTypeDefinition>(header.typeDefinitionsOffset, header.typeDefinitionsCount);
			methodDefs = ReadMetadataClassArray<Il2CppMethodDefinition>(header.methodsOffset, header.methodsCount);
			parameterDefs = ReadMetadataClassArray<Il2CppParameterDefinition>(header.parametersOffset, header.parametersCount);
			fieldDefs = ReadMetadataClassArray<Il2CppFieldDefinition>(header.fieldsOffset, header.fieldsCount);
			Il2CppFieldDefaultValue[] fieldDefaultValues = ReadMetadataClassArray<Il2CppFieldDefaultValue>(header.fieldDefaultValuesOffset, header.fieldDefaultValuesCount);
			Il2CppParameterDefaultValue[] parameterDefaultValues = ReadMetadataClassArray<Il2CppParameterDefaultValue>(header.parameterDefaultValuesOffset, header.parameterDefaultValuesCount);
			fieldDefaultValuesDic = fieldDefaultValues.ToDictionary((Il2CppFieldDefaultValue x) => x.fieldIndex);
			parameterDefaultValuesDic = parameterDefaultValues.ToDictionary((Il2CppParameterDefaultValue x) => x.parameterIndex);
			propertyDefs = ReadMetadataClassArray<Il2CppPropertyDefinition>(header.propertiesOffset, header.propertiesCount);
			interfaceIndices = ReadClassArray<int>(header.interfacesOffset, header.interfacesCount / 4);
			nestedTypeIndices = ReadClassArray<int>(header.nestedTypesOffset, header.nestedTypesCount / 4);
			eventDefs = ReadMetadataClassArray<Il2CppEventDefinition>(header.eventsOffset, header.eventsCount);
			genericContainers = ReadMetadataClassArray<Il2CppGenericContainer>(header.genericContainersOffset, header.genericContainersCount);
			genericParameters = ReadMetadataClassArray<Il2CppGenericParameter>(header.genericParametersOffset, header.genericParametersCount);
			constraintIndices = ReadClassArray<int>(header.genericParameterConstraintsOffset, header.genericParameterConstraintsCount / 4);
			vtableMethods = ReadClassArray<uint>(header.vtableMethodsOffset, header.vtableMethodsCount / 4);
			stringLiterals = ReadMetadataClassArray<Il2CppStringLiteral>(header.stringLiteralOffset, header.stringLiteralCount);
			if (Version > 16f && Version < 27f)
			{
				metadataUsageLists = ReadMetadataClassArray<Il2CppMetadataUsageList>(header.metadataUsageListsOffset, header.metadataUsageListsCount);
				metadataUsagePairs = ReadMetadataClassArray<Il2CppMetadataUsagePair>(header.metadataUsagePairsOffset, header.metadataUsagePairsCount);
				ProcessingMetadataUsage();
				fieldRefs = ReadMetadataClassArray<Il2CppFieldRef>(header.fieldRefsOffset, header.fieldRefsCount);
			}
			if (Version > 20f)
			{
				attributeTypeRanges = ReadMetadataClassArray<Il2CppCustomAttributeTypeRange>(header.attributesInfoOffset, header.attributesInfoCount);
				attributeTypes = ReadClassArray<int>(header.attributeTypesOffset, header.attributeTypesCount / 4);
			}
			if (Version <= 24.1f)
			{
				rgctxEntries = ReadMetadataClassArray<Il2CppRGCTXDefinition>(header.rgctxEntriesOffset, header.rgctxEntriesCount);
			}
		}

		private T[] ReadMetadataClassArray<T>(uint addr, int count) where T : new()
		{
			return ReadClassArray<T>(addr, count / SizeOf(typeof(T)));
		}

		public bool GetFieldDefaultValueFromIndex(int index, out Il2CppFieldDefaultValue value)
		{
			return fieldDefaultValuesDic.TryGetValue(index, out value);
		}

		public bool GetParameterDefaultValueFromIndex(int index, out Il2CppParameterDefaultValue value)
		{
			return parameterDefaultValuesDic.TryGetValue(index, out value);
		}

		public uint GetDefaultValueFromIndex(int index)
		{
			return (uint)(header.fieldAndParameterDefaultValueDataOffset + index);
		}

		public string GetStringFromIndex(uint index)
		{
			string result;
			if (!stringCache.TryGetValue(index, out result))
			{
				result = ReadStringToNull(header.stringOffset + index);
				stringCache.Add(index, result);
			}
			return result;
		}

		public string GetStringFromIndexWithTranslate(uint index)
		{
			return LookupNameTranslation(GetStringFromIndex(index));
		}

		public string GetTypeStringFromIndexWithTranslate(uint index)
		{
			string type = GetStringFromIndexWithTranslate(index);
			if (type.Contains("/"))
			{
				string[] parts = type.Split('/');
				return parts[parts.Length - 1];
			}
			return type;
		}

		public int GetCustomAttributeIndex(Il2CppImageDefinition imageDef, int customAttributeIndex, uint token)
		{
			if (Version > 24f)
			{
				int index;
				if (attributeTypeRangesDic[imageDef].TryGetValue(token, out index))
				{
					return index;
				}
				return -1;
			}
			return customAttributeIndex;
		}

		public string GetStringLiteralFromIndex(uint index)
		{
			Il2CppStringLiteral stringLiteral = stringLiterals[index];
			base.Position = (ulong)(SizeOf(typeof(Il2CppGlobalMetadataHeader)) + stringLiteral.dataIndex);
			byte[] buffer = ReadBytes((int)stringLiteral.length);
			for (int i = 0; i < stringLiteral.length; i++)
			{
				byte cl = (byte)(buffer[i] ^ stringDecryptionBlob[(5120 + i) % 20480]);
				byte al = (byte)(stringDecryptionBlob[i % 10240 + index % 10240] + i);
				buffer[i] = (byte)(cl ^ al);
			}
			return Encoding.UTF8.GetString(buffer);
		}

		private void ProcessingMetadataUsage()
		{
			metadataUsageDic = new Dictionary<uint, SortedDictionary<uint, uint>>();
			for (uint i = 1u; i <= 6; i++)
			{
				metadataUsageDic[i] = new SortedDictionary<uint, uint>();
			}
			Il2CppMetadataUsageList[] array = metadataUsageLists;
			foreach (Il2CppMetadataUsageList metadataUsageList in array)
			{
				for (int k = 0; k < metadataUsageList.count; k++)
				{
					long offset = metadataUsageList.start + k;
					Il2CppMetadataUsagePair metadataUsagePair = metadataUsagePairs[offset];
					uint usage = GetEncodedIndexType(metadataUsagePair.encodedSourceIndex);
					uint decodedIndex = GetDecodedMethodIndex(metadataUsagePair.encodedSourceIndex);
					metadataUsageDic[usage][metadataUsagePair.destinationIndex] = decodedIndex;
				}
			}
			maxMetadataUsages = metadataUsageDic.Max((KeyValuePair<uint, SortedDictionary<uint, uint>> x) => x.Value.Max((KeyValuePair<uint, uint> y) => y.Key)) + 1;
		}

		public uint GetEncodedIndexType(uint index)
		{
			return (index & 0xE0000000u) >> 29;
		}

		public uint GetDecodedMethodIndex(uint index)
		{
			if (Version >= 27f)
			{
				return (index & 0x1FFFFFFE) >> 1;
			}
			return index & 0x1FFFFFFF;
		}

		public int SizeOf(Type type)
		{
			int size = 0;
			FieldInfo[] fields = type.GetFields();
			foreach (FieldInfo i2 in fields)
			{
				VersionAttribute attr = (VersionAttribute)Attribute.GetCustomAttribute(i2, typeof(VersionAttribute));
				if (attr == null || (!(Version < attr.Min) && !(Version > attr.Max)))
				{
					Type fieldType = i2.FieldType;
					if (fieldType.IsPrimitive)
					{
						size += _003CSizeOf_003Eg__GetPrimitiveTypeSize_007C45_0(fieldType.Name);
					}
					else if (fieldType.IsEnum)
					{
						Type e = fieldType.GetField("value__").FieldType;
						size += _003CSizeOf_003Eg__GetPrimitiveTypeSize_007C45_0(e.Name);
					}
					else
					{
						size += SizeOf(fieldType);
					}
				}
			}
			return size;
		}

		public string ReadString(int numChars)
		{
			ulong start = base.Position;
			string str = Encoding.UTF8.GetString(ReadBytes(numChars * 4)).Substring(0, numChars);
			base.Position = start;
			ReadBytes(Encoding.UTF8.GetByteCount(str));
			return str;
		}

		public string LookupNameTranslation(string obfuscated)
		{
			string original;
			if (nameTranslation.TryGetValue(obfuscated, out original))
			{
				return original;
			}
			return obfuscated;
		}

		[CompilerGenerated]
		internal static int _003CSizeOf_003Eg__GetPrimitiveTypeSize_007C45_0(string name)
		{
			switch (name)
			{
			case "Int32":
			case "UInt32":
				return 4;
			case "Int16":
			case "UInt16":
				return 2;
			default:
				return 0;
			}
		}
	}
}
