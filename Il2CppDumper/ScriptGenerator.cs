using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Il2CppDumper
{
	public class ScriptGenerator
	{
		private Il2CppExecutor executor;

		private Metadata metadata;

		private Il2Cpp il2Cpp;

		private Dictionary<Il2CppTypeDefinition, string> typeDefImageNames = new Dictionary<Il2CppTypeDefinition, string>();

		private HashSet<string> structNameHashSet = new HashSet<string>(StringComparer.Ordinal);

		private List<StructInfo> structInfoList = new List<StructInfo>();

		private Dictionary<string, StructInfo> structInfoWithStructName = new Dictionary<string, StructInfo>();

		private HashSet<StructInfo> structCache = new HashSet<StructInfo>();

		private Dictionary<Il2CppTypeDefinition, string> structNameDic = new Dictionary<Il2CppTypeDefinition, string>();

		private Dictionary<ulong, string> genericClassStructNameDic = new Dictionary<ulong, string>();

		private Dictionary<string, Il2CppType> nameGenericClassDic = new Dictionary<string, Il2CppType>();

		private List<ulong> genericClassList = new List<ulong>();

		private StringBuilder arrayClassHeader = new StringBuilder();

		private StringBuilder methodInfoHeader = new StringBuilder();

		private static HashSet<string> keyword = new HashSet<string>(StringComparer.Ordinal)
		{
			"klass", "monitor", "register", "_cs", "auto", "friend", "template", "near", "far", "flat",
			"default", "_ds", "interrupt", "inline", "unsigned", "signed", "asm", "if", "case", "break",
			"continue", "do", "new", "_", "short", "union", "static"
		};

		public ScriptGenerator(Il2CppExecutor il2CppExecutor)
		{
			executor = il2CppExecutor;
			metadata = il2CppExecutor.metadata;
			il2Cpp = il2CppExecutor.il2Cpp;
		}

		public void WriteScript(string outputDir)
		{
			ScriptJson json = new ScriptJson();
			for (int imageIndex = 0; imageIndex < metadata.imageDefs.Length; imageIndex++)
			{
				Il2CppImageDefinition imageDef = metadata.imageDefs[imageIndex];
				string imageName = metadata.GetStringFromIndexWithTranslate(imageDef.nameIndex);
				long typeEnd = imageDef.typeStart + imageDef.typeCount;
				for (int typeIndex = imageDef.typeStart; typeIndex < typeEnd; typeIndex++)
				{
					Il2CppTypeDefinition typeDef = metadata.typeDefs[typeIndex];
					typeDefImageNames.Add(typeDef, imageName);
					CreateStructNameDic(typeDef);
				}
			}
			foreach (Il2CppType il2CppType in il2Cpp.types.Where((Il2CppType x) => x.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST))
			{
				Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				Il2CppTypeDefinition typeDef2 = executor.GetGenericClassTypeDefinition(genericClass);
				if (typeDef2 != null)
				{
					string typeBaseName = structNameDic[typeDef2];
					string typeToReplaceName = FixName(executor.GetTypeDefName(typeDef2, true, true));
					string typeReplaceName = FixName(executor.GetTypeName(il2CppType, true, false));
					string typeStructName = typeBaseName.Replace(typeToReplaceName, typeReplaceName);
					nameGenericClassDic[typeStructName] = il2CppType;
					genericClassStructNameDic[il2CppType.data.generic_class] = typeStructName;
				}
			}
			Il2CppImageDefinition[] imageDefs = metadata.imageDefs;
			foreach (Il2CppImageDefinition imageDef2 in imageDefs)
			{
				string imageName2 = metadata.GetStringFromIndexWithTranslate(imageDef2.nameIndex);
				long typeEnd2 = imageDef2.typeStart + imageDef2.typeCount;
				for (int typeIndex2 = imageDef2.typeStart; typeIndex2 < typeEnd2; typeIndex2++)
				{
					Il2CppTypeDefinition typeDef3 = metadata.typeDefs[typeIndex2];
					AddStruct(typeDef3);
					string methodInfoName = string.Format("MethodInfo_{0}", typeIndex2);
					string structTypeName = structNameDic[typeDef3];
					GenerateMethodInfo(structTypeName, methodInfoName);
					string typeName = executor.GetTypeDefName(typeDef3, true, true);
					int methodEnd = typeDef3.methodStart + typeDef3.method_count;
					for (int i = typeDef3.methodStart; i < methodEnd; i++)
					{
						Il2CppMethodDefinition methodDef = metadata.methodDefs[i];
						string methodName = metadata.GetStringFromIndexWithTranslate(methodDef.nameIndex);
						ulong methodPointer = il2Cpp.GetMethodPointer(imageName2, methodDef);
						if (methodPointer != 0)
						{
							ScriptMethod scriptMethod = new ScriptMethod();
							json.ScriptMethod.Add(scriptMethod);
							scriptMethod.Address = il2Cpp.GetRVA(methodPointer);
							string methodFullName = (scriptMethod.Name = typeName + "$$" + methodName);
							Il2CppType methodReturnType = il2Cpp.types[methodDef.returnType];
							string returnType = ParseType(methodReturnType);
							if (methodReturnType.byref == 1)
							{
								returnType += "*";
							}
							string signature = returnType + " " + FixName(methodFullName) + " (";
							List<string> parameterStrs = new List<string>();
							if ((methodDef.flags & 0x10) == 0)
							{
								string thisType = ParseType(il2Cpp.types[typeDef3.byvalTypeIndex]);
								parameterStrs.Add(thisType + " __this");
							}
							else if (il2Cpp.Version <= 24f)
							{
								parameterStrs.Add("Il2CppObject* __this");
							}
							for (int j = 0; j < methodDef.parameterCount; j++)
							{
								Il2CppParameterDefinition parameterDef = metadata.parameterDefs[methodDef.parameterStart + j];
								string parameterName = metadata.GetStringFromIndexWithTranslate(parameterDef.nameIndex);
								Il2CppType parameterType = il2Cpp.types[parameterDef.typeIndex];
								string parameterCType = ParseType(parameterType);
								if (parameterType.byref == 1)
								{
									parameterCType += "*";
								}
								parameterStrs.Add(parameterCType + " " + FixName(parameterName));
							}
							parameterStrs.Add("const MethodInfo* method");
							signature += string.Join(", ", parameterStrs);
							signature += ");";
							scriptMethod.Signature = signature;
						}
						List<Il2CppMethodSpec> methodSpecs;
						if (!il2Cpp.methodDefinitionMethodSpecs.TryGetValue(i, out methodSpecs))
						{
							continue;
						}
						foreach (Il2CppMethodSpec methodSpec in methodSpecs)
						{
							ulong genericMethodPointer = il2Cpp.methodSpecGenericMethodPointers[methodSpec];
							if (genericMethodPointer == 0)
							{
								continue;
							}
							ScriptMethod scriptMethod2 = new ScriptMethod();
							json.ScriptMethod.Add(scriptMethod2);
							scriptMethod2.Address = il2Cpp.GetRVA(genericMethodPointer);
							ValueTuple<string, string> methodSpecName = executor.GetMethodSpecName(methodSpec, true);
							string methodSpecTypeName = methodSpecName.Item1;
							string methodSpecMethodName = methodSpecName.Item2;
							string methodFullName2 = (scriptMethod2.Name = methodSpecTypeName + "$$" + methodSpecMethodName);
							Il2CppGenericContext genericContext = executor.GetMethodSpecGenericContext(methodSpec);
							Il2CppType methodReturnType2 = il2Cpp.types[methodDef.returnType];
							string returnType2 = ParseType(methodReturnType2, genericContext);
							if (methodReturnType2.byref == 1)
							{
								returnType2 += "*";
							}
							string signature2 = returnType2 + " " + FixName(methodFullName2) + " (";
							List<string> parameterStrs2 = new List<string>();
							if ((methodDef.flags & 0x10) == 0)
							{
								string thisType2;
								if (methodSpec.classIndexIndex != -1)
								{
									string typeBaseName2 = structNameDic[typeDef3];
									string typeToReplaceName2 = FixName(typeName);
									string typeReplaceName2 = FixName(methodSpecTypeName);
									string typeStructName2 = typeBaseName2.Replace(typeToReplaceName2, typeReplaceName2);
									Il2CppType il2CppType2;
									thisType2 = ((!nameGenericClassDic.TryGetValue(typeStructName2, out il2CppType2)) ? ParseType(il2Cpp.types[typeDef3.byvalTypeIndex]) : ParseType(il2CppType2));
								}
								else
								{
									thisType2 = ParseType(il2Cpp.types[typeDef3.byvalTypeIndex]);
								}
								parameterStrs2.Add(thisType2 + " __this");
							}
							else if (il2Cpp.Version <= 24f)
							{
								parameterStrs2.Add("Il2CppObject* __this");
							}
							for (int j2 = 0; j2 < methodDef.parameterCount; j2++)
							{
								Il2CppParameterDefinition parameterDef2 = metadata.parameterDefs[methodDef.parameterStart + j2];
								string parameterName2 = metadata.GetStringFromIndexWithTranslate(parameterDef2.nameIndex);
								Il2CppType parameterType2 = il2Cpp.types[parameterDef2.typeIndex];
								string parameterCType2 = ParseType(parameterType2, genericContext);
								if (parameterType2.byref == 1)
								{
									parameterCType2 += "*";
								}
								parameterStrs2.Add(parameterCType2 + " " + FixName(parameterName2));
							}
							parameterStrs2.Add("const " + methodInfoName + "* method");
							signature2 += string.Join(", ", parameterStrs2);
							signature2 += ");";
							scriptMethod2.Signature = signature2;
						}
					}
				}
			}
			foreach (KeyValuePair<uint, uint> i2 in metadata.metadataUsageDic[1u])
			{
				Il2CppType type = il2Cpp.types[i2.Value];
				string typeName2 = executor.GetTypeName(type, true, false);
				ScriptMetadata scriptMetadata = new ScriptMetadata();
				json.ScriptMetadata.Add(scriptMetadata);
				scriptMetadata.Address = il2Cpp.GetRVA(il2Cpp.metadataUsages[i2.Key]);
				scriptMetadata.Name = typeName2 + "_TypeInfo";
				string signature3 = GetIl2CppStructName(type);
				if (signature3.EndsWith("_array"))
				{
					scriptMetadata.Signature = "Il2CppClass*";
				}
				else
				{
					scriptMetadata.Signature = FixName(signature3) + "_c*";
				}
			}
			foreach (KeyValuePair<uint, uint> i3 in metadata.metadataUsageDic[2u])
			{
				Il2CppType type2 = il2Cpp.types[i3.Value];
				string typeName3 = executor.GetTypeName(type2, true, false);
				ScriptMetadata scriptMetadata2 = new ScriptMetadata();
				json.ScriptMetadata.Add(scriptMetadata2);
				scriptMetadata2.Address = il2Cpp.GetRVA(il2Cpp.metadataUsages[i3.Key]);
				scriptMetadata2.Name = typeName3 + "_var";
				scriptMetadata2.Signature = "Il2CppType*";
			}
			foreach (KeyValuePair<uint, uint> i4 in metadata.metadataUsageDic[3u])
			{
				Il2CppMethodDefinition methodDef2 = metadata.methodDefs[i4.Value];
				Il2CppTypeDefinition typeDef4 = metadata.typeDefs[methodDef2.declaringType];
				string typeName4 = executor.GetTypeDefName(typeDef4, true, true);
				string methodName2 = typeName4 + "." + metadata.GetStringFromIndexWithTranslate(methodDef2.nameIndex) + "()";
				ScriptMetadataMethod scriptMetadataMethod = new ScriptMetadataMethod();
				json.ScriptMetadataMethod.Add(scriptMetadataMethod);
				scriptMetadataMethod.Address = il2Cpp.GetRVA(il2Cpp.metadataUsages[i4.Key]);
				scriptMetadataMethod.Name = "Method$" + methodName2;
				string imageName3 = typeDefImageNames[typeDef4];
				ulong methodPointer2 = il2Cpp.GetMethodPointer(imageName3, methodDef2);
				if (methodPointer2 != 0)
				{
					scriptMetadataMethod.MethodAddress = il2Cpp.GetRVA(methodPointer2);
				}
			}
			foreach (KeyValuePair<uint, uint> i5 in metadata.metadataUsageDic[4u])
			{
				Il2CppFieldRef fieldRef = metadata.fieldRefs[i5.Value];
				Il2CppType type3 = il2Cpp.types[fieldRef.typeIndex];
				Il2CppTypeDefinition typeDef5 = GetTypeDefinition(type3);
				Il2CppFieldDefinition fieldDef = metadata.fieldDefs[typeDef5.fieldStart + fieldRef.fieldIndex];
				string fieldName = executor.GetTypeName(type3, true, false) + "." + metadata.GetStringFromIndexWithTranslate(fieldDef.nameIndex);
				ScriptMetadata scriptMetadata3 = new ScriptMetadata();
				json.ScriptMetadata.Add(scriptMetadata3);
				scriptMetadata3.Address = il2Cpp.GetRVA(il2Cpp.metadataUsages[i5.Key]);
				scriptMetadata3.Name = "Field$" + fieldName;
			}
			foreach (KeyValuePair<uint, uint> i6 in metadata.metadataUsageDic[5u])
			{
				ScriptString scriptString = new ScriptString();
				json.ScriptString.Add(scriptString);
				scriptString.Address = il2Cpp.GetRVA(il2Cpp.metadataUsages[i6.Key]);
				scriptString.Value = metadata.GetStringLiteralFromIndex(i6.Value);
			}
			foreach (KeyValuePair<uint, uint> i7 in metadata.metadataUsageDic[6u])
			{
				Il2CppMethodSpec methodSpec2 = il2Cpp.methodSpecs[i7.Value];
				ScriptMetadataMethod scriptMetadataMethod2 = new ScriptMetadataMethod();
				json.ScriptMetadataMethod.Add(scriptMetadataMethod2);
				scriptMetadataMethod2.Address = il2Cpp.GetRVA(il2Cpp.metadataUsages[i7.Key]);
				ValueTuple<string, string> methodSpecName2 = executor.GetMethodSpecName(methodSpec2, true);
				string methodSpecTypeName2 = methodSpecName2.Item1;
				string methodSpecMethodName2 = methodSpecName2.Item2;
				scriptMetadataMethod2.Name = "Method$" + methodSpecTypeName2 + "." + methodSpecMethodName2 + "()";
				ulong genericMethodPointer2 = il2Cpp.methodSpecGenericMethodPointers[methodSpec2];
				if (genericMethodPointer2 != 0)
				{
					scriptMetadataMethod2.MethodAddress = il2Cpp.GetRVA(genericMethodPointer2);
				}
			}
			List<ulong> orderedPointers;
			if (il2Cpp.Version >= 24.2f)
			{
				orderedPointers = new List<ulong>();
				foreach (KeyValuePair<string, ulong[]> codeGenModuleMethodPointer in il2Cpp.codeGenModuleMethodPointers)
				{
					orderedPointers.AddRange(codeGenModuleMethodPointer.Value);
				}
			}
			else
			{
				orderedPointers = il2Cpp.methodPointers.ToList();
			}
			orderedPointers.AddRange(il2Cpp.genericMethodPointers);
			orderedPointers.AddRange(il2Cpp.invokerPointers);
			orderedPointers.AddRange(executor.customAttributeGenerators);
			if (il2Cpp.Version >= 22f)
			{
				if (il2Cpp.reversePInvokeWrappers != null)
				{
					orderedPointers.AddRange(il2Cpp.reversePInvokeWrappers);
				}
				if (il2Cpp.unresolvedVirtualCallPointers != null)
				{
					orderedPointers.AddRange(il2Cpp.unresolvedVirtualCallPointers);
				}
			}
			orderedPointers = (from x in orderedPointers.Distinct()
				orderby x
				select x).ToList();
			orderedPointers.Remove(0uL);
			for (int i8 = 0; i8 < orderedPointers.Count; i8++)
			{
				orderedPointers[i8] = il2Cpp.GetRVA(orderedPointers[i8]);
			}
			json.Addresses = orderedPointers;
			var stringLiterals = json.ScriptString.Select(x => new
			{
				value = x.Value,
				address = $"0x{x.Address:X}"
			}).ToArray();
			File.WriteAllText(Path.Combine(outputDir, "stringliteral.json"), JsonConvert.SerializeObject(stringLiterals, Formatting.Indented));
			File.WriteAllText(Path.Combine(outputDir, "script.json"), JsonConvert.SerializeObject(json, Formatting.Indented));
			for (int i9 = 0; i9 < genericClassList.Count; i9++)
			{
				ulong pointer = genericClassList[i9];
				AddGenericClassStruct(pointer);
			}
			StringBuilder headerStruct = new StringBuilder();
			foreach (StructInfo info in structInfoList)
			{
				structInfoWithStructName.Add(info.TypeName + "_o", info);
			}
			foreach (StructInfo info2 in structInfoList)
			{
				headerStruct.Append(RecursionStructInfo(info2));
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(HeaderConstants.GenericHeader);
			float version = il2Cpp.Version;
			float num2 = version;
			if (num2 != 22f)
			{
				if (num2 != 23f && num2 != 24f)
				{
					if (num2 != 24.1f)
					{
						if (num2 != 24.2f && num2 != 24.3f)
						{
							if (num2 != 27f)
							{
								Console.WriteLine(string.Format("WARNING: This il2cpp version [{0}] does not support generating .h files", il2Cpp.Version));
								return;
							}
							sb.Append(HeaderConstants.HeaderV27);
						}
						else
						{
							sb.Append(HeaderConstants.HeaderV242);
						}
					}
					else
					{
						sb.Append(HeaderConstants.HeaderV241);
					}
				}
				else
				{
					sb.Append(HeaderConstants.HeaderV240);
				}
			}
			else
			{
				sb.Append(HeaderConstants.HeaderV22);
			}
			sb.Append(headerStruct);
			sb.Append(arrayClassHeader);
			sb.Append(methodInfoHeader);
			File.WriteAllText(Path.Combine(outputDir, "il2cpp.h"), sb.ToString());
		}

		private static string FixName(string str)
		{
			if (keyword.Contains(str))
			{
				str = "_" + str;
			}
			if (Regex.IsMatch(str, "^[0-9]"))
			{
				return "_" + str;
			}
			return Regex.Replace(str, "[^a-zA-Z0-9_]", "_");
		}

		private string ParseType(Il2CppType il2CppType, Il2CppGenericContext context = null)
		{
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_VOID:
				return "void";
			case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
				return "bool";
			case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
				return "uint16_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_I1:
				return "int8_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_U1:
				return "uint8_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_I2:
				return "int16_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_U2:
				return "uint16_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_I4:
				return "int32_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_U4:
				return "uint32_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_I8:
				return "int64_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_U8:
				return "uint64_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_R4:
				return "float";
			case Il2CppTypeEnum.IL2CPP_TYPE_R8:
				return "double";
			case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
				return "System_String_o*";
			case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
			{
				Il2CppType oriType = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return ParseType(oriType) + "*";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			{
				Il2CppTypeDefinition typeDef3 = executor.GetTypeDefinitionFromIl2CppType(il2CppType);
				if (typeDef3.IsEnum)
				{
					return ParseType(il2Cpp.types[typeDef3.elementTypeIndex]);
				}
				return structNameDic[typeDef3] + "_o";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			{
				Il2CppTypeDefinition typeDef2 = executor.GetTypeDefinitionFromIl2CppType(il2CppType);
				return structNameDic[typeDef2] + "_o*";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter2 = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst2 = il2Cpp.MapVATR<Il2CppGenericInst>(context.class_inst);
					ulong[] pointers2 = il2Cpp.MapVATR<ulong>(genericInst2.type_argv, genericInst2.type_argc);
					ulong pointer2 = pointers2[genericParameter2.num];
					Il2CppType type2 = il2Cpp.GetIl2CppType(pointer2);
					return ParseType(type2);
				}
				return "Il2CppObject*";
			case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
			{
				Il2CppArrayType arrayType = il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
				Il2CppType elementType2 = il2Cpp.GetIl2CppType(arrayType.etype);
				string elementStructName2 = GetIl2CppStructName(elementType2, context);
				string typeStructName3 = elementStructName2 + "_array";
				if (structNameHashSet.Add(typeStructName3))
				{
					ParseArrayClassStruct(elementType2, context);
				}
				return typeStructName3 + "*";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				Il2CppTypeDefinition typeDef = executor.GetGenericClassTypeDefinition(genericClass);
				string typeStructName2 = genericClassStructNameDic[il2CppType.data.generic_class];
				if (structNameHashSet.Add(typeStructName2))
				{
					genericClassList.Add(il2CppType.data.generic_class);
				}
				if (typeDef.IsValueType)
				{
					if (typeDef.IsEnum)
					{
						return ParseType(il2Cpp.types[typeDef.elementTypeIndex]);
					}
					return typeStructName2 + "_o";
				}
				return typeStructName2 + "_o*";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF:
				return "Il2CppObject*";
			case Il2CppTypeEnum.IL2CPP_TYPE_I:
				return "intptr_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_U:
				return "uintptr_t";
			case Il2CppTypeEnum.IL2CPP_TYPE_OBJECT:
				return "Il2CppObject*";
			case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
			{
				Il2CppType elementType = il2Cpp.GetIl2CppType(il2CppType.data.type);
				string elementStructName = GetIl2CppStructName(elementType, context);
				string typeStructName = elementStructName + "_array";
				if (structNameHashSet.Add(typeStructName))
				{
					ParseArrayClassStruct(elementType, context);
				}
				return typeStructName + "*";
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(context.method_inst);
					ulong[] pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
					ulong pointer = pointers[genericParameter.num];
					Il2CppType type = il2Cpp.GetIl2CppType(pointer);
					return ParseType(type);
				}
				return "Il2CppObject*";
			default:
				throw new NotSupportedException();
			}
		}

		private void AddStruct(Il2CppTypeDefinition typeDef)
		{
			StructInfo structInfo = new StructInfo();
			structInfoList.Add(structInfo);
			structInfo.TypeName = structNameDic[typeDef];
			structInfo.IsValueType = typeDef.IsValueType;
			AddParents(typeDef, structInfo);
			AddFields(typeDef, structInfo, null);
			AddVTableMethod(structInfo, typeDef);
			AddRGCTX(structInfo, typeDef);
		}

		private void AddGenericClassStruct(ulong pointer)
		{
			Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(pointer);
			Il2CppTypeDefinition typeDef = executor.GetGenericClassTypeDefinition(genericClass);
			StructInfo structInfo = new StructInfo();
			structInfoList.Add(structInfo);
			structInfo.TypeName = genericClassStructNameDic[pointer];
			structInfo.IsValueType = typeDef.IsValueType;
			AddParents(typeDef, structInfo);
			AddFields(typeDef, structInfo, genericClass.context);
			AddVTableMethod(structInfo, typeDef);
		}

		private void AddParents(Il2CppTypeDefinition typeDef, StructInfo structInfo)
		{
			if (!typeDef.IsValueType && !typeDef.IsEnum && typeDef.parentIndex >= 0)
			{
				Il2CppType parent = il2Cpp.types[typeDef.parentIndex];
				if (parent.type != Il2CppTypeEnum.IL2CPP_TYPE_OBJECT)
				{
					structInfo.Parent = GetIl2CppStructName(parent);
				}
			}
		}

		private void AddFields(Il2CppTypeDefinition typeDef, StructInfo structInfo, Il2CppGenericContext context)
		{
			if (typeDef.field_count <= 0)
			{
				return;
			}
			int fieldEnd = typeDef.fieldStart + typeDef.field_count;
			HashSet<string> cache = new HashSet<string>(StringComparer.Ordinal);
			for (int i = typeDef.fieldStart; i < fieldEnd; i++)
			{
				Il2CppFieldDefinition fieldDef = metadata.fieldDefs[i];
				Il2CppType fieldType = il2Cpp.types[fieldDef.typeIndex];
				if ((fieldType.attrs & 0x40) == 0)
				{
					StructFieldInfo structFieldInfo = new StructFieldInfo();
					structFieldInfo.FieldTypeName = ParseType(fieldType, context);
					string fieldName = FixName(metadata.GetStringFromIndexWithTranslate(fieldDef.nameIndex));
					if (!cache.Add(fieldName))
					{
						fieldName = string.Format("_{0}_{1}", i - typeDef.fieldStart, fieldName);
					}
					structFieldInfo.FieldName = fieldName;
					structFieldInfo.IsValueType = IsValueType(fieldType, context);
					structFieldInfo.IsCustomType = IsCustomType(fieldType, context);
					if ((fieldType.attrs & 0x10) != 0)
					{
						structInfo.StaticFields.Add(structFieldInfo);
					}
					else
					{
						structInfo.Fields.Add(structFieldInfo);
					}
				}
			}
		}

		private void AddVTableMethod(StructInfo structInfo, Il2CppTypeDefinition typeDef)
		{
			SortedDictionary<int, Il2CppMethodDefinition> dic = new SortedDictionary<int, Il2CppMethodDefinition>();
			for (int i = 0; i < typeDef.vtable_count; i++)
			{
				int vTableIndex = typeDef.vtableStart + i;
				uint encodedMethodIndex = metadata.vtableMethods[vTableIndex];
				uint usage = metadata.GetEncodedIndexType(encodedMethodIndex);
				uint index = metadata.GetDecodedMethodIndex(encodedMethodIndex);
				Il2CppMethodDefinition methodDef;
				if (usage == 6)
				{
					Il2CppMethodSpec methodSpec = il2Cpp.methodSpecs[index];
					methodDef = metadata.methodDefs[methodSpec.methodDefinitionIndex];
				}
				else
				{
					methodDef = metadata.methodDefs[index];
				}
				dic[methodDef.slot] = methodDef;
			}
			foreach (KeyValuePair<int, Il2CppMethodDefinition> i2 in dic)
			{
				StructVTableMethodInfo methodInfo = new StructVTableMethodInfo();
				structInfo.VTableMethod.Add(methodInfo);
				Il2CppMethodDefinition methodDef2 = i2.Value;
				methodInfo.MethodName = string.Format("_{0}_{1}", methodDef2.slot, FixName(metadata.GetStringFromIndexWithTranslate(methodDef2.nameIndex)));
			}
		}

		private void AddRGCTX(StructInfo structInfo, Il2CppTypeDefinition typeDef)
		{
			string imageName = typeDefImageNames[typeDef];
			Il2CppRGCTXDefinition[] collection = executor.GetTypeRGCTXDefinition(imageName, typeDef);
			if (collection == null)
			{
				return;
			}
			Il2CppRGCTXDefinition[] array = collection;
			foreach (Il2CppRGCTXDefinition definitionData in array)
			{
				StructRGCTXInfo structRGCTXInfo = new StructRGCTXInfo();
				structInfo.RGCTXs.Add(structRGCTXInfo);
				structRGCTXInfo.Type = definitionData.type;
				switch (definitionData.type)
				{
				case Il2CppRGCTXDataType.IL2CPP_RGCTX_DATA_TYPE:
				{
					Il2CppType il2CppType2 = il2Cpp.types[definitionData.data.typeIndex];
					structRGCTXInfo.TypeName = FixName(executor.GetTypeName(il2CppType2, true, false));
					break;
				}
				case Il2CppRGCTXDataType.IL2CPP_RGCTX_DATA_CLASS:
				{
					Il2CppType il2CppType = il2Cpp.types[definitionData.data.typeIndex];
					structRGCTXInfo.ClassName = FixName(executor.GetTypeName(il2CppType, true, false));
					break;
				}
				case Il2CppRGCTXDataType.IL2CPP_RGCTX_DATA_METHOD:
				{
					Il2CppMethodSpec methodSpec = il2Cpp.methodSpecs[definitionData.data.methodIndex];
					ValueTuple<string, string> methodSpecName = executor.GetMethodSpecName(methodSpec, true);
					string methodSpecTypeName = methodSpecName.Item1;
					string methodSpecMethodName = methodSpecName.Item2;
					structRGCTXInfo.MethodName = FixName(methodSpecTypeName + "." + methodSpecMethodName);
					break;
				}
				}
			}
		}

		private void ParseArrayClassStruct(Il2CppType il2CppType, Il2CppGenericContext context)
		{
			string structName = GetIl2CppStructName(il2CppType, context);
			arrayClassHeader.Append("struct " + structName + "_array {\n\tIl2CppObject obj;\n\tIl2CppArrayBounds *bounds;\n\til2cpp_array_size_t max_length;\n\t" + ParseType(il2CppType, context) + " m_Items[65535];\n};\n");
		}

		private Il2CppTypeDefinition GetTypeDefinition(Il2CppType il2CppType)
		{
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_VOID:
			case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
			case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
			case Il2CppTypeEnum.IL2CPP_TYPE_I1:
			case Il2CppTypeEnum.IL2CPP_TYPE_U1:
			case Il2CppTypeEnum.IL2CPP_TYPE_I2:
			case Il2CppTypeEnum.IL2CPP_TYPE_U2:
			case Il2CppTypeEnum.IL2CPP_TYPE_I4:
			case Il2CppTypeEnum.IL2CPP_TYPE_U4:
			case Il2CppTypeEnum.IL2CPP_TYPE_I8:
			case Il2CppTypeEnum.IL2CPP_TYPE_U8:
			case Il2CppTypeEnum.IL2CPP_TYPE_R4:
			case Il2CppTypeEnum.IL2CPP_TYPE_R8:
			case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			case Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF:
			case Il2CppTypeEnum.IL2CPP_TYPE_I:
			case Il2CppTypeEnum.IL2CPP_TYPE_U:
			case Il2CppTypeEnum.IL2CPP_TYPE_OBJECT:
				return executor.GetTypeDefinitionFromIl2CppType(il2CppType);
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				return executor.GetGenericClassTypeDefinition(genericClass);
			}
			default:
				throw new NotSupportedException();
			}
		}

		private void CreateStructNameDic(Il2CppTypeDefinition typeDef)
		{
			string typeName = executor.GetTypeDefName(typeDef, true, true);
			string typeStructName = FixName(typeName);
			string uniqueName = GetUniqueName(typeStructName);
			structNameDic.Add(typeDef, uniqueName);
		}

		private string GetUniqueName(string name)
		{
			string fixName = name;
			int i = 1;
			while (!structNameHashSet.Add(fixName))
			{
				fixName = string.Format("{0}_{1}", name, i++);
			}
			return fixName;
		}

		private string RecursionStructInfo(StructInfo info)
		{
			if (!structCache.Add(info))
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder();
			StringBuilder pre = new StringBuilder();
			if (info.Parent != null)
			{
				string parentStructName = info.Parent + "_o";
				pre.Append(RecursionStructInfo(structInfoWithStructName[parentStructName]));
				sb.Append("struct " + info.TypeName + "_Fields {\n");
				sb.Append("\t" + info.Parent + "_Fields _;\n");
			}
			else if (il2Cpp is PE && !info.IsValueType)
			{
				if (il2Cpp.Is32Bit)
				{
					sb.Append("struct __declspec(align(4)) " + info.TypeName + "_Fields {\n");
				}
				else
				{
					sb.Append("struct __declspec(align(8)) " + info.TypeName + "_Fields {\n");
				}
			}
			else
			{
				sb.Append("struct " + info.TypeName + "_Fields {\n");
			}
			foreach (StructFieldInfo field in info.Fields)
			{
				if (field.IsValueType)
				{
					StructInfo fieldInfo = structInfoWithStructName[field.FieldTypeName];
					pre.Append(RecursionStructInfo(fieldInfo));
				}
				if (field.IsCustomType)
				{
					sb.Append("\tstruct " + field.FieldTypeName + " " + field.FieldName + ";\n");
				}
				else
				{
					sb.Append("\t" + field.FieldTypeName + " " + field.FieldName + ";\n");
				}
			}
			sb.Append("};\n");
			sb.Append("struct " + info.TypeName + "_RGCTXs {\n");
			for (int i = 0; i < info.RGCTXs.Count; i++)
			{
				StructRGCTXInfo rgctx = info.RGCTXs[i];
				switch (rgctx.Type)
				{
				case Il2CppRGCTXDataType.IL2CPP_RGCTX_DATA_TYPE:
					sb.Append(string.Format("\tIl2CppType* _{0}_{1};\n", i, rgctx.TypeName));
					break;
				case Il2CppRGCTXDataType.IL2CPP_RGCTX_DATA_CLASS:
					sb.Append(string.Format("\tIl2CppClass* _{0}_{1};\n", i, rgctx.ClassName));
					break;
				case Il2CppRGCTXDataType.IL2CPP_RGCTX_DATA_METHOD:
					sb.Append(string.Format("\tMethodInfo* _{0}_{1};\n", i, rgctx.MethodName));
					break;
				}
			}
			sb.Append("};\n");
			sb.Append("struct " + info.TypeName + "_VTable {\n");
			foreach (StructVTableMethodInfo method in info.VTableMethod)
			{
				sb.Append("\tVirtualInvokeData " + method.MethodName + ";\n");
			}
			sb.Append("};\n");
			sb.Append("struct " + info.TypeName + "_c {\n\tIl2CppClass_1 _1;\n\tstruct " + info.TypeName + "_StaticFields* static_fields;\n\t" + info.TypeName + "_RGCTXs* rgctx_data;\n\tIl2CppClass_2 _2;\n\t" + info.TypeName + "_VTable vtable;\n};\n");
			sb.Append("struct " + info.TypeName + "_o {\n");
			if (!info.IsValueType)
			{
				sb.Append("\t" + info.TypeName + "_c *klass;\n");
				sb.Append("\tvoid *monitor;\n");
			}
			sb.Append("\t" + info.TypeName + "_Fields fields;\n");
			sb.Append("};\n");
			sb.Append("struct " + info.TypeName + "_StaticFields {\n");
			foreach (StructFieldInfo field2 in info.StaticFields)
			{
				if (field2.IsValueType)
				{
					StructInfo fieldInfo2 = structInfoWithStructName[field2.FieldTypeName];
					pre.Append(RecursionStructInfo(fieldInfo2));
				}
				if (field2.IsCustomType)
				{
					sb.Append("\tstruct " + field2.FieldTypeName + " " + field2.FieldName + ";\n");
				}
				else
				{
					sb.Append("\t" + field2.FieldTypeName + " " + field2.FieldName + ";\n");
				}
			}
			sb.Append("};\n");
			return pre.Append(sb).ToString();
		}

		private string GetIl2CppStructName(Il2CppType il2CppType, Il2CppGenericContext context = null)
		{
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_VOID:
			case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
			case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
			case Il2CppTypeEnum.IL2CPP_TYPE_I1:
			case Il2CppTypeEnum.IL2CPP_TYPE_U1:
			case Il2CppTypeEnum.IL2CPP_TYPE_I2:
			case Il2CppTypeEnum.IL2CPP_TYPE_U2:
			case Il2CppTypeEnum.IL2CPP_TYPE_I4:
			case Il2CppTypeEnum.IL2CPP_TYPE_U4:
			case Il2CppTypeEnum.IL2CPP_TYPE_I8:
			case Il2CppTypeEnum.IL2CPP_TYPE_U8:
			case Il2CppTypeEnum.IL2CPP_TYPE_R4:
			case Il2CppTypeEnum.IL2CPP_TYPE_R8:
			case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			case Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF:
			case Il2CppTypeEnum.IL2CPP_TYPE_I:
			case Il2CppTypeEnum.IL2CPP_TYPE_U:
			case Il2CppTypeEnum.IL2CPP_TYPE_OBJECT:
			{
				Il2CppTypeDefinition typeDef = executor.GetTypeDefinitionFromIl2CppType(il2CppType);
				return structNameDic[typeDef];
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
			{
				Il2CppType oriType = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return GetIl2CppStructName(oriType);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
			{
				Il2CppArrayType arrayType = il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
				Il2CppType elementType = il2Cpp.GetIl2CppType(arrayType.etype);
				string elementStructName = GetIl2CppStructName(elementType, context);
				string typeStructName = elementStructName + "_array";
				if (structNameHashSet.Add(typeStructName))
				{
					ParseArrayClassStruct(elementType, context);
				}
				return typeStructName;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
			{
				Il2CppType elementType2 = il2Cpp.GetIl2CppType(il2CppType.data.type);
				string elementStructName2 = GetIl2CppStructName(elementType2, context);
				string typeStructName2 = elementStructName2 + "_array";
				if (structNameHashSet.Add(typeStructName2))
				{
					ParseArrayClassStruct(elementType2, context);
				}
				return typeStructName2;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				string typeStructName3 = genericClassStructNameDic[il2CppType.data.generic_class];
				if (structNameHashSet.Add(typeStructName3))
				{
					genericClassList.Add(il2CppType.data.generic_class);
				}
				return typeStructName3;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter2 = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst2 = il2Cpp.MapVATR<Il2CppGenericInst>(context.class_inst);
					ulong[] pointers2 = il2Cpp.MapVATR<ulong>(genericInst2.type_argv, genericInst2.type_argc);
					ulong pointer2 = pointers2[genericParameter2.num];
					Il2CppType type2 = il2Cpp.GetIl2CppType(pointer2);
					return GetIl2CppStructName(type2);
				}
				return "System_Object";
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(context.method_inst);
					ulong[] pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
					ulong pointer = pointers[genericParameter.num];
					Il2CppType type = il2Cpp.GetIl2CppType(pointer);
					return GetIl2CppStructName(type);
				}
				return "System_Object";
			default:
				throw new NotSupportedException();
			}
		}

		private bool IsValueType(Il2CppType il2CppType, Il2CppGenericContext context)
		{
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			{
				Il2CppTypeDefinition typeDef2 = executor.GetTypeDefinitionFromIl2CppType(il2CppType);
				return !typeDef2.IsEnum;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				Il2CppTypeDefinition typeDef = executor.GetGenericClassTypeDefinition(genericClass);
				return typeDef.IsValueType && !typeDef.IsEnum;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter2 = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst2 = il2Cpp.MapVATR<Il2CppGenericInst>(context.class_inst);
					ulong[] pointers2 = il2Cpp.MapVATR<ulong>(genericInst2.type_argv, genericInst2.type_argc);
					ulong pointer2 = pointers2[genericParameter2.num];
					Il2CppType type2 = il2Cpp.GetIl2CppType(pointer2);
					return IsValueType(type2, null);
				}
				return false;
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(context.method_inst);
					ulong[] pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
					ulong pointer = pointers[genericParameter.num];
					Il2CppType type = il2Cpp.GetIl2CppType(pointer);
					return IsValueType(type, null);
				}
				return false;
			default:
				return false;
			}
		}

		private bool IsCustomType(Il2CppType il2CppType, Il2CppGenericContext context)
		{
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
			{
				Il2CppType oriType = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return IsCustomType(oriType, context);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
			case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			{
				Il2CppTypeDefinition typeDef2 = executor.GetTypeDefinitionFromIl2CppType(il2CppType);
				if (typeDef2.IsEnum)
				{
					return IsCustomType(il2Cpp.types[typeDef2.elementTypeIndex], context);
				}
				return true;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				Il2CppTypeDefinition typeDef = executor.GetGenericClassTypeDefinition(genericClass);
				if (typeDef.IsEnum)
				{
					return IsCustomType(il2Cpp.types[typeDef.elementTypeIndex], context);
				}
				return true;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter2 = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst2 = il2Cpp.MapVATR<Il2CppGenericInst>(context.class_inst);
					ulong[] pointers2 = il2Cpp.MapVATR<ulong>(genericInst2.type_argv, genericInst2.type_argc);
					ulong pointer2 = pointers2[genericParameter2.num];
					Il2CppType type2 = il2Cpp.GetIl2CppType(pointer2);
					return IsCustomType(type2, null);
				}
				return false;
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
				if (context != null)
				{
					Il2CppGenericParameter genericParameter = executor.GetGenericParameteFromIl2CppType(il2CppType);
					Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(context.method_inst);
					ulong[] pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
					ulong pointer = pointers[genericParameter.num];
					Il2CppType type = il2Cpp.GetIl2CppType(pointer);
					return IsCustomType(type, null);
				}
				return false;
			default:
				return false;
			}
		}

		private void GenerateMethodInfo(string structTypeName, string methodInfoName)
		{
			methodInfoHeader.Append("struct " + methodInfoName + " {\n");
			methodInfoHeader.Append("\tIl2CppMethodPointer methodPointer;\n");
			methodInfoHeader.Append("\tvoid* invoker_method;\n");
			methodInfoHeader.Append("\tconst char* name;\n");
			if (il2Cpp.Version <= 24f)
			{
				methodInfoHeader.Append("\t" + structTypeName + "_c *declaring_type;\n");
			}
			else
			{
				methodInfoHeader.Append("\t" + structTypeName + "_c *klass;\n");
			}
			methodInfoHeader.Append("\tconst Il2CppType *return_type;\n");
			methodInfoHeader.Append("\tconst void* parameters;\n");
			methodInfoHeader.Append("\tunion\n");
			methodInfoHeader.Append("\t{\n");
			methodInfoHeader.Append("\t\tconst Il2CppRGCTXData* rgctx_data;\n");
			methodInfoHeader.Append("\t\tconst void* methodDefinition;\n");
			methodInfoHeader.Append("\t};\n");
			methodInfoHeader.Append("\tunion\n");
			methodInfoHeader.Append("\t{\n");
			methodInfoHeader.Append("\t\tconst void* genericMethod;\n");
			methodInfoHeader.Append("\t\tconst void* genericContainer;\n");
			methodInfoHeader.Append("\t};\n");
			if (il2Cpp.Version <= 24f)
			{
				methodInfoHeader.Append("\tint32_t customAttributeIndex;\n");
			}
			methodInfoHeader.Append("\tuint32_t token;\n");
			methodInfoHeader.Append("\tuint16_t flags;\n");
			methodInfoHeader.Append("\tuint16_t iflags;\n");
			methodInfoHeader.Append("\tuint16_t slot;\n");
			methodInfoHeader.Append("\tuint8_t parameters_count;\n");
			methodInfoHeader.Append("\tuint8_t bitflags;\n");
			methodInfoHeader.Append("};\n");
		}
	}
}
