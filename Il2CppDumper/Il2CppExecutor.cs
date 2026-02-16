using System;
using System.Collections.Generic;

namespace Il2CppDumper
{
	public class Il2CppExecutor
	{
		public Metadata metadata;

		public Il2Cpp il2Cpp;

		private static readonly Dictionary<int, string> TypeString = new Dictionary<int, string>
		{
			{ 1, "void" },
			{ 2, "bool" },
			{ 3, "char" },
			{ 4, "sbyte" },
			{ 5, "byte" },
			{ 6, "short" },
			{ 7, "ushort" },
			{ 8, "int" },
			{ 9, "uint" },
			{ 10, "long" },
			{ 11, "ulong" },
			{ 12, "float" },
			{ 13, "double" },
			{ 14, "string" },
			{ 22, "TypedReference" },
			{ 24, "IntPtr" },
			{ 25, "UIntPtr" },
			{ 28, "object" }
		};

		public ulong[] customAttributeGenerators;

		public Il2CppExecutor(Metadata metadata, Il2Cpp il2Cpp)
		{
			this.metadata = metadata;
			this.il2Cpp = il2Cpp;
			if (!(il2Cpp.Version >= 27f))
			{
				customAttributeGenerators = il2Cpp.customAttributeGenerators;
			}
		}

		public string GetTypeName(Il2CppType il2CppType, bool addNamespace, bool is_nested)
		{
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
			{
				Il2CppArrayType arrayType = il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
				Il2CppType elementType = il2Cpp.GetIl2CppType(arrayType.etype);
				return GetTypeName(elementType, addNamespace, false) + "[" + new string(',', arrayType.rank - 1) + "]";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
			{
				Il2CppType elementType2 = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return GetTypeName(elementType2, addNamespace, false) + "[]";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
			{
				Il2CppType oriType = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return GetTypeName(oriType, addNamespace, false) + "*";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
			{
				Il2CppGenericParameter param = GetGenericParameteFromIl2CppType(il2CppType);
				return metadata.GetStringFromIndexWithTranslate(param.nameIndex);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				string str = string.Empty;
				Il2CppGenericClass genericClass = null;
				Il2CppTypeDefinition typeDef;
				if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
				{
					genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
					typeDef = GetGenericClassTypeDefinition(genericClass);
				}
				else
				{
					typeDef = GetTypeDefinitionFromIl2CppType(il2CppType);
				}
				string typeName = metadata.GetTypeStringFromIndexWithTranslate(typeDef.nameIndex);
				int lastDotIndex = typeName.LastIndexOf('.');
				string namespacePart = "";
				if (lastDotIndex >= 0)
				{
					namespacePart = typeName.Substring(0, lastDotIndex);
					typeName = typeName.Substring(lastDotIndex + 1);
				}
				if (typeDef.declaringTypeIndex != -1)
				{
					str += GetTypeName(il2Cpp.types[typeDef.declaringTypeIndex], addNamespace, true);
					str += ".";
				}
				else if (addNamespace)
				{
					string @namespace = metadata.GetStringFromIndexWithTranslate(typeDef.namespaceIndex);
					if (@namespace != "")
					{
						str = str + @namespace + ".";
					}
					else if (namespacePart != "")
					{
						str = str + namespacePart + ".";
					}
				}
				int index = typeName.IndexOf("`");
				str = ((index == -1) ? (str + typeName) : (str + typeName.Substring(0, index)));
				if (is_nested)
				{
					return str;
				}
				if (genericClass != null)
				{
					Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(genericClass.context.class_inst);
					str += GetGenericInstParams(genericInst);
				}
				else if (typeDef.genericContainerIndex >= 0)
				{
					Il2CppGenericContainer genericContainer = metadata.genericContainers[typeDef.genericContainerIndex];
					str += GetGenericContainerParams(genericContainer);
				}
				return str;
			}
			default:
				return TypeString[(int)il2CppType.type];
			}
		}

		public string GetTypeDefName(Il2CppTypeDefinition typeDef, bool addNamespace, bool genericParameter)
		{
			string prefix = string.Empty;
			string typeName = metadata.GetTypeStringFromIndexWithTranslate(typeDef.nameIndex);
			int lastDotIndex = typeName.LastIndexOf('.');
			string namespacePart = "";
			if (lastDotIndex >= 0)
			{
				namespacePart = typeName.Substring(0, lastDotIndex);
				typeName = typeName.Substring(lastDotIndex + 1);
			}
			if (typeDef.declaringTypeIndex != -1)
			{
				prefix = GetTypeName(il2Cpp.types[typeDef.declaringTypeIndex], addNamespace, true) + ".";
			}
			else if (addNamespace)
			{
				string @namespace = metadata.GetStringFromIndexWithTranslate(typeDef.namespaceIndex);
				if (@namespace != "")
				{
					prefix = @namespace + ".";
				}
				else if (namespacePart != "")
				{
					prefix = prefix + namespacePart + ".";
				}
			}
			if (typeDef.genericContainerIndex >= 0)
			{
				int index = typeName.IndexOf("`");
				if (index != -1)
				{
					typeName = typeName.Substring(0, index);
				}
				if (genericParameter)
				{
					Il2CppGenericContainer genericContainer = metadata.genericContainers[typeDef.genericContainerIndex];
					typeName += GetGenericContainerParams(genericContainer);
				}
			}
			return prefix + typeName;
		}

		public string GetGenericInstParams(Il2CppGenericInst genericInst)
		{
			List<string> genericParameterNames = new List<string>();
			ulong[] pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
			for (int i = 0; i < genericInst.type_argc; i++)
			{
				Il2CppType il2CppType = il2Cpp.GetIl2CppType(pointers[i]);
				genericParameterNames.Add(GetTypeName(il2CppType, false, false));
			}
			return "<" + string.Join(", ", genericParameterNames) + ">";
		}

		public string GetGenericContainerParams(Il2CppGenericContainer genericContainer)
		{
			List<string> genericParameterNames = new List<string>();
			for (int i = 0; i < genericContainer.type_argc; i++)
			{
				int genericParameterIndex = genericContainer.genericParameterStart + i;
				Il2CppGenericParameter genericParameter = metadata.genericParameters[genericParameterIndex];
				genericParameterNames.Add(metadata.GetStringFromIndexWithTranslate(genericParameter.nameIndex));
			}
			return "<" + string.Join(", ", genericParameterNames) + ">";
		}

		public ValueTuple<string, string> GetMethodSpecName(Il2CppMethodSpec methodSpec, bool addNamespace = false)
		{
			Il2CppMethodDefinition methodDef = metadata.methodDefs[methodSpec.methodDefinitionIndex];
			Il2CppTypeDefinition typeDef = metadata.typeDefs[methodDef.declaringType];
			string typeName = GetTypeDefName(typeDef, addNamespace, false);
			if (methodSpec.classIndexIndex != -1)
			{
				Il2CppGenericInst classInst = il2Cpp.genericInsts[methodSpec.classIndexIndex];
				typeName += GetGenericInstParams(classInst);
			}
			string methodName = metadata.GetStringFromIndexWithTranslate(methodDef.nameIndex);
			if (methodSpec.methodIndexIndex != -1)
			{
				Il2CppGenericInst methodInst = il2Cpp.genericInsts[methodSpec.methodIndexIndex];
				methodName += GetGenericInstParams(methodInst);
			}
			return new ValueTuple<string, string>(typeName, methodName);
		}

		public Il2CppGenericContext GetMethodSpecGenericContext(Il2CppMethodSpec methodSpec)
		{
			ulong classInstPointer = 0uL;
			ulong methodInstPointer = 0uL;
			if (methodSpec.classIndexIndex != -1)
			{
				classInstPointer = il2Cpp.genericInstPointers[methodSpec.classIndexIndex];
			}
			if (methodSpec.methodIndexIndex != -1)
			{
				methodInstPointer = il2Cpp.genericInstPointers[methodSpec.methodIndexIndex];
			}
			return new Il2CppGenericContext
			{
				class_inst = classInstPointer,
				method_inst = methodInstPointer
			};
		}

		public Il2CppRGCTXDefinition[] GetTypeRGCTXDefinition(string imageName, Il2CppTypeDefinition typeDef)
		{
			Il2CppRGCTXDefinition[] collection = null;
			if (il2Cpp.Version >= 24.2f)
			{
				il2Cpp.rgctxsDictionary[imageName].TryGetValue(typeDef.token, out collection);
			}
			else if (typeDef.rgctxCount > 0)
			{
				collection = new Il2CppRGCTXDefinition[typeDef.rgctxCount];
				Array.Copy(metadata.rgctxEntries, typeDef.rgctxStartIndex, collection, 0, typeDef.rgctxCount);
			}
			return collection;
		}

		public Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass)
		{
			if (il2Cpp.Version >= 27f)
			{
				Il2CppType il2CppType = il2Cpp.GetIl2CppType(genericClass.type);
				return GetTypeDefinitionFromIl2CppType(il2CppType);
			}
			if (genericClass.typeDefinitionIndex == uint.MaxValue || genericClass.typeDefinitionIndex == -1)
			{
				return null;
			}
			return metadata.typeDefs[genericClass.typeDefinitionIndex];
		}

		public Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType)
		{
			return metadata.typeDefs[il2CppType.data.klassIndex];
		}

		public Il2CppGenericParameter GetGenericParameteFromIl2CppType(Il2CppType il2CppType)
		{
			return metadata.genericParameters[il2CppType.data.genericParameterIndex];
		}
	}
}
