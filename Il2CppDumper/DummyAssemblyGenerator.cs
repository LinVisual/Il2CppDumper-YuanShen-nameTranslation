using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Il2CppDumper
{
	public class DummyAssemblyGenerator
	{
		public List<AssemblyDefinition> Assemblies = new List<AssemblyDefinition>();

		private Il2CppExecutor executor;

		private Metadata metadata;

		private Il2Cpp il2Cpp;

		private Dictionary<Il2CppTypeDefinition, TypeDefinition> typeDefinitionDic = new Dictionary<Il2CppTypeDefinition, TypeDefinition>();

		private Dictionary<Il2CppGenericParameter, GenericParameter> genericParameterDic = new Dictionary<Il2CppGenericParameter, GenericParameter>();

		private MethodDefinition attributeAttribute;

		private TypeReference stringType;

		private Dictionary<string, MethodDefinition> knownAttributes = new Dictionary<string, MethodDefinition>();

		private static readonly string[] knownAttributeNames = new string[16]
		{
			"System.Runtime.CompilerServices.ExtensionAttribute", "System.Runtime.CompilerServices.NullableAttribute", "System.Runtime.CompilerServices.NullableContextAttribute", "System.Runtime.CompilerServices.IsReadOnlyAttribute", "System.Diagnostics.DebuggerHiddenAttribute", "System.Diagnostics.DebuggerStepThroughAttribute", "System.FlagsAttribute", "System.Runtime.CompilerServices.IsByRefLikeAttribute", "System.NonSerializedAttribute", "System.Runtime.InteropServices.PreserveSigAttribute",
			"System.ParamArrayAttribute", "System.Runtime.CompilerServices.CallerMemberNameAttribute", "System.Runtime.CompilerServices.CallerFilePathAttribute", "System.Runtime.CompilerServices.CallerLineNumberAttribute", "System.Runtime.CompilerServices.IsUnmanagedAttribute", "UnityEngine.SerializeField"
		};

		public DummyAssemblyGenerator(Il2CppExecutor il2CppExecutor, bool generateAttributes = false)
		{
			executor = il2CppExecutor;
			metadata = il2CppExecutor.metadata;
			il2Cpp = il2CppExecutor.il2Cpp;
			MyAssemblyResolver resolver = new MyAssemblyResolver();
			ModuleParameters moduleParameters = new ModuleParameters
			{
				Kind = ModuleKind.Dll,
				AssemblyResolver = resolver
			};
			AssemblyDefinition il2CppDummyDll = Il2CppDummyDll.Create();
			MethodDefinition addressAttribute = il2CppDummyDll.MainModule.Types.First((TypeDefinition x) => x.Name == "AddressAttribute").Methods[0];
			MethodDefinition fieldOffsetAttribute = il2CppDummyDll.MainModule.Types.First((TypeDefinition x) => x.Name == "FieldOffsetAttribute").Methods[0];
			attributeAttribute = il2CppDummyDll.MainModule.Types.First((TypeDefinition x) => x.Name == "AttributeAttribute").Methods[0];
			MethodDefinition metadataOffsetAttribute = il2CppDummyDll.MainModule.Types.First((TypeDefinition x) => x.Name == "MetadataOffsetAttribute").Methods[0];
			MethodDefinition tokenAttribute = il2CppDummyDll.MainModule.Types.First((TypeDefinition x) => x.Name == "TokenAttribute").Methods[0];
			stringType = il2CppDummyDll.MainModule.TypeSystem.String;
			if (generateAttributes)
			{
				Assemblies.Add(il2CppDummyDll);
				resolver.Register(il2CppDummyDll);
			}
			Dictionary<int, FieldDefinition> fieldDefinitionDic = new Dictionary<int, FieldDefinition>();
			Dictionary<int, MethodDefinition> methodDefinitionDic = new Dictionary<int, MethodDefinition>();
			Dictionary<int, ParameterDefinition> parameterDefinitionDic = new Dictionary<int, ParameterDefinition>();
			Dictionary<int, PropertyDefinition> propertyDefinitionDic = new Dictionary<int, PropertyDefinition>();
			Dictionary<int, EventDefinition> eventDefinitionDic = new Dictionary<int, EventDefinition>();
			Il2CppImageDefinition[] imageDefs = metadata.imageDefs;
			foreach (Il2CppImageDefinition imageDef in imageDefs)
			{
				string imageName = metadata.GetStringFromIndexWithTranslate(imageDef.nameIndex);
				AssemblyNameDefinition assemblyName = new AssemblyNameDefinition(imageName.Replace(".dll", ""), new Version("3.7.1.6"));
				AssemblyDefinition assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, imageName, moduleParameters);
				resolver.Register(assemblyDefinition);
				Assemblies.Add(assemblyDefinition);
				ModuleDefinition moduleDefinition = assemblyDefinition.MainModule;
				moduleDefinition.Types.Clear();
				long typeEnd = imageDef.typeStart + imageDef.typeCount;
				for (int index = imageDef.typeStart; index < typeEnd; index++)
				{
					Il2CppTypeDefinition typeDef = metadata.typeDefs[index];
					string namespaceName = metadata.GetStringFromIndexWithTranslate(typeDef.namespaceIndex);
					string typeName = metadata.GetTypeStringFromIndexWithTranslate(typeDef.nameIndex);
					int lastDotIndex = typeName.LastIndexOf('.');
					if (lastDotIndex >= 0)
					{
						if (namespaceName == "")
						{
							namespaceName = typeName.Substring(0, lastDotIndex);
						}
						typeName = typeName.Substring(lastDotIndex + 1);
					}
					TypeDefinition typeDefinition = new TypeDefinition(namespaceName, typeName, (TypeAttributes)typeDef.flags);
					typeDefinitionDic.Add(typeDef, typeDefinition);
					if (typeDef.declaringTypeIndex == -1)
					{
						moduleDefinition.Types.Add(typeDefinition);
					}
				}
			}
			for (int index2 = 0; index2 < metadata.typeDefs.Length; index2++)
			{
				Il2CppTypeDefinition typeDef2 = metadata.typeDefs[index2];
				TypeDefinition typeDefinition2 = typeDefinitionDic[typeDef2];
				for (int i = 0; i < typeDef2.nested_type_count; i++)
				{
					int nestedIndex = metadata.nestedTypeIndices[typeDef2.nestedTypesStart + i];
					Il2CppTypeDefinition nestedTypeDef = metadata.typeDefs[nestedIndex];
					TypeDefinition nestedTypeDefinition = typeDefinitionDic[nestedTypeDef];
					typeDefinition2.NestedTypes.Add(nestedTypeDefinition);
				}
			}
			for (int index3 = 0; index3 < metadata.typeDefs.Length; index3++)
			{
				Il2CppTypeDefinition typeDef3 = metadata.typeDefs[index3];
				TypeDefinition typeDefinition3 = typeDefinitionDic[typeDef3];
				if (generateAttributes)
				{
					CustomAttribute customTokenAttribute = new CustomAttribute(typeDefinition3.Module.ImportReference(tokenAttribute))
					{
						Fields = 
						{
							new CustomAttributeNamedArgument("Token", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", typeDef3.token)))
						}
					};
					typeDefinition3.CustomAttributes.Add(customTokenAttribute);
				}
				if (typeDef3.genericContainerIndex >= 0)
				{
					Il2CppGenericContainer genericContainer = metadata.genericContainers[typeDef3.genericContainerIndex];
					for (int i2 = 0; i2 < genericContainer.type_argc; i2++)
					{
						int genericParameterIndex = genericContainer.genericParameterStart + i2;
						Il2CppGenericParameter param = metadata.genericParameters[genericParameterIndex];
						GenericParameter genericParameter = CreateGenericParameter(param, typeDefinition3);
						typeDefinition3.GenericParameters.Add(genericParameter);
					}
				}
				if (typeDef3.parentIndex >= 0)
				{
					Il2CppType parentType = il2Cpp.types[typeDef3.parentIndex];
					TypeReference parentTypeRef = GetTypeReference(typeDefinition3, parentType);
					typeDefinition3.BaseType = parentTypeRef;
				}
				for (int i3 = 0; i3 < typeDef3.interfaces_count; i3++)
				{
					Il2CppType interfaceType = il2Cpp.types[metadata.interfaceIndices[typeDef3.interfacesStart + i3]];
					TypeReference interfaceTypeRef = GetTypeReference(typeDefinition3, interfaceType);
					typeDefinition3.Interfaces.Add(new InterfaceImplementation(interfaceTypeRef));
				}
			}
			Il2CppImageDefinition[] imageDefs2 = metadata.imageDefs;
			foreach (Il2CppImageDefinition imageDef2 in imageDefs2)
			{
				string imageName2 = metadata.GetStringFromIndexWithTranslate(imageDef2.nameIndex);
				long typeEnd2 = imageDef2.typeStart + imageDef2.typeCount;
				for (int index4 = imageDef2.typeStart; index4 < typeEnd2; index4++)
				{
					Il2CppTypeDefinition typeDef4 = metadata.typeDefs[index4];
					TypeDefinition typeDefinition4 = typeDefinitionDic[typeDef4];
					int fieldEnd = typeDef4.fieldStart + typeDef4.field_count;
					for (int i4 = typeDef4.fieldStart; i4 < fieldEnd; i4++)
					{
						Il2CppFieldDefinition fieldDef = metadata.fieldDefs[i4];
						Il2CppType fieldType = il2Cpp.types[fieldDef.typeIndex];
						string fieldName = metadata.GetStringFromIndexWithTranslate(fieldDef.nameIndex);
						TypeReference fieldTypeRef = GetTypeReference(typeDefinition4, fieldType);
						FieldDefinition fieldDefinition = new FieldDefinition(fieldName, (FieldAttributes)fieldType.attrs, fieldTypeRef);
						typeDefinition4.Fields.Add(fieldDefinition);
						fieldDefinitionDic.Add(i4, fieldDefinition);
						if (generateAttributes)
						{
							CustomAttribute customTokenAttribute2 = new CustomAttribute(typeDefinition4.Module.ImportReference(tokenAttribute))
							{
								Fields = 
								{
									new CustomAttributeNamedArgument("Token", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", fieldDef.token)))
								}
							};
							fieldDefinition.CustomAttributes.Add(customTokenAttribute2);
						}
						Il2CppFieldDefaultValue fieldDefault;
						if (metadata.GetFieldDefaultValueFromIndex(i4, out fieldDefault) && fieldDefault.dataIndex != -1)
						{
							object value;
							if (TryGetDefaultValue(fieldDefault.typeIndex, fieldDefault.dataIndex, out value))
							{
								fieldDefinition.Constant = value;
							}
							else if (generateAttributes)
							{
								CustomAttribute customAttribute = new CustomAttribute(typeDefinition4.Module.ImportReference(metadataOffsetAttribute));
								CustomAttributeNamedArgument offset = new CustomAttributeNamedArgument("Offset", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", value)));
								customAttribute.Fields.Add(offset);
								fieldDefinition.CustomAttributes.Add(customAttribute);
							}
						}
						if (!fieldDefinition.IsLiteral && generateAttributes)
						{
							int fieldOffset = il2Cpp.GetFieldOffsetFromIndex(index4, i4 - typeDef4.fieldStart, i4, typeDefinition4.IsValueType, fieldDefinition.IsStatic);
							if (fieldOffset >= 0)
							{
								CustomAttribute customAttribute2 = new CustomAttribute(typeDefinition4.Module.ImportReference(fieldOffsetAttribute));
								CustomAttributeNamedArgument offset2 = new CustomAttributeNamedArgument("Offset", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", fieldOffset)));
								customAttribute2.Fields.Add(offset2);
								fieldDefinition.CustomAttributes.Add(customAttribute2);
							}
						}
					}
					int methodEnd = typeDef4.methodStart + typeDef4.method_count;
					for (int i5 = typeDef4.methodStart; i5 < methodEnd; i5++)
					{
						Il2CppMethodDefinition methodDef = metadata.methodDefs[i5];
						string methodName = metadata.GetStringFromIndexWithTranslate(methodDef.nameIndex);
						MethodDefinition methodDefinition = new MethodDefinition(methodName, (MethodAttributes)methodDef.flags, typeDefinition4.Module.ImportReference(typeof(void)))
						{
							ImplAttributes = (MethodImplAttributes)methodDef.iflags
						};
						typeDefinition4.Methods.Add(methodDefinition);
						if (methodDef.genericContainerIndex >= 0)
						{
							Il2CppGenericContainer genericContainer2 = metadata.genericContainers[methodDef.genericContainerIndex];
							for (int j = 0; j < genericContainer2.type_argc; j++)
							{
								int genericParameterIndex2 = genericContainer2.genericParameterStart + j;
								Il2CppGenericParameter param2 = metadata.genericParameters[genericParameterIndex2];
								GenericParameter genericParameter2 = CreateGenericParameter(param2, methodDefinition);
								methodDefinition.GenericParameters.Add(genericParameter2);
							}
						}
						Il2CppType methodReturnType = il2Cpp.types[methodDef.returnType];
						TypeReference returnType = (methodDefinition.ReturnType = GetTypeReferenceWithByRef(methodDefinition, methodReturnType));
						if (generateAttributes)
						{
							CustomAttribute customTokenAttribute3 = new CustomAttribute(typeDefinition4.Module.ImportReference(tokenAttribute))
							{
								Fields = 
								{
									new CustomAttributeNamedArgument("Token", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", methodDef.token)))
								}
							};
							methodDefinition.CustomAttributes.Add(customTokenAttribute3);
						}
						if (methodDefinition.HasBody)
						{
							TypeReference baseType = typeDefinition4.BaseType;
							if (((baseType != null) ? baseType.FullName : null) != "System.MulticastDelegate")
							{
								ILProcessor ilprocessor = methodDefinition.Body.GetILProcessor();
								if (returnType.FullName == "System.Void")
								{
									ilprocessor.Append(ilprocessor.Create(OpCodes.Ret));
								}
								else if (returnType.IsValueType)
								{
									VariableDefinition variable = new VariableDefinition(returnType);
									methodDefinition.Body.Variables.Add(variable);
									ilprocessor.Append(ilprocessor.Create(OpCodes.Ldloca_S, variable));
									ilprocessor.Append(ilprocessor.Create(OpCodes.Initobj, returnType));
									ilprocessor.Append(ilprocessor.Create(OpCodes.Ldloc_0));
									ilprocessor.Append(ilprocessor.Create(OpCodes.Ret));
								}
								else
								{
									ilprocessor.Append(ilprocessor.Create(OpCodes.Ldnull));
									ilprocessor.Append(ilprocessor.Create(OpCodes.Ret));
								}
							}
						}
						methodDefinitionDic.Add(i5, methodDefinition);
						for (int j2 = 0; j2 < methodDef.parameterCount; j2++)
						{
							Il2CppParameterDefinition parameterDef = metadata.parameterDefs[methodDef.parameterStart + j2];
							string parameterName = metadata.GetStringFromIndexWithTranslate(parameterDef.nameIndex);
							Il2CppType parameterType = il2Cpp.types[parameterDef.typeIndex];
							TypeReference parameterTypeRef = GetTypeReferenceWithByRef(methodDefinition, parameterType);
							ParameterDefinition parameterDefinition = new ParameterDefinition(parameterName, (ParameterAttributes)parameterType.attrs, parameterTypeRef);
							methodDefinition.Parameters.Add(parameterDefinition);
							parameterDefinitionDic.Add(methodDef.parameterStart + j2, parameterDefinition);
							Il2CppParameterDefaultValue parameterDefault;
							if (metadata.GetParameterDefaultValueFromIndex(methodDef.parameterStart + j2, out parameterDefault) && parameterDefault.dataIndex != -1)
							{
								object value2;
								if (TryGetDefaultValue(parameterDefault.typeIndex, parameterDefault.dataIndex, out value2))
								{
									parameterDefinition.Constant = value2;
								}
								else if (generateAttributes)
								{
									CustomAttribute customAttribute3 = new CustomAttribute(typeDefinition4.Module.ImportReference(metadataOffsetAttribute));
									CustomAttributeNamedArgument offset3 = new CustomAttributeNamedArgument("Offset", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", value2)));
									customAttribute3.Fields.Add(offset3);
									parameterDefinition.CustomAttributes.Add(customAttribute3);
								}
							}
						}
						if (!generateAttributes)
						{
							continue;
						}
						ulong methodPointer = il2Cpp.GetMethodPointer(imageName2, methodDef);
						if (methodPointer != 0)
						{
							CustomAttribute customAttribute4 = new CustomAttribute(typeDefinition4.Module.ImportReference(addressAttribute));
							ulong fixedMethodPointer = il2Cpp.GetRVA(methodPointer);
							CustomAttributeNamedArgument rva = new CustomAttributeNamedArgument("RVA", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", fixedMethodPointer)));
							CustomAttributeNamedArgument offset4 = new CustomAttributeNamedArgument("Offset", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", il2Cpp.MapVATR(methodPointer))));
							CustomAttributeNamedArgument va = new CustomAttributeNamedArgument("VA", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", methodPointer)));
							customAttribute4.Fields.Add(rva);
							customAttribute4.Fields.Add(offset4);
							customAttribute4.Fields.Add(va);
							if (methodDef.slot != ushort.MaxValue)
							{
								CustomAttributeNamedArgument slot = new CustomAttributeNamedArgument("Slot", new CustomAttributeArgument(stringType, methodDef.slot.ToString()));
								customAttribute4.Fields.Add(slot);
							}
							methodDefinition.CustomAttributes.Add(customAttribute4);
						}
					}
					int propertyEnd = typeDef4.propertyStart + typeDef4.property_count;
					for (int i6 = typeDef4.propertyStart; i6 < propertyEnd; i6++)
					{
						Il2CppPropertyDefinition propertyDef = metadata.propertyDefs[i6];
						string propertyName = metadata.GetStringFromIndexWithTranslate(propertyDef.nameIndex);
						TypeReference propertyType = null;
						MethodDefinition GetMethod = null;
						MethodDefinition SetMethod = null;
						if (propertyDef.get >= 0)
						{
							GetMethod = methodDefinitionDic[typeDef4.methodStart + propertyDef.get];
							propertyType = GetMethod.ReturnType;
						}
						if (propertyDef.set >= 0)
						{
							SetMethod = methodDefinitionDic[typeDef4.methodStart + propertyDef.set];
							if (propertyType == null)
							{
								propertyType = SetMethod.Parameters[0].ParameterType;
							}
						}
						PropertyDefinition propertyDefinition = new PropertyDefinition(propertyName, (PropertyAttributes)propertyDef.attrs, propertyType)
						{
							GetMethod = GetMethod,
							SetMethod = SetMethod
						};
						typeDefinition4.Properties.Add(propertyDefinition);
						propertyDefinitionDic.Add(i6, propertyDefinition);
						if (generateAttributes)
						{
							CustomAttribute customTokenAttribute4 = new CustomAttribute(typeDefinition4.Module.ImportReference(tokenAttribute))
							{
								Fields = 
								{
									new CustomAttributeNamedArgument("Token", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", propertyDef.token)))
								}
							};
							propertyDefinition.CustomAttributes.Add(customTokenAttribute4);
						}
					}
					int eventEnd = typeDef4.eventStart + typeDef4.event_count;
					for (int i7 = typeDef4.eventStart; i7 < eventEnd; i7++)
					{
						Il2CppEventDefinition eventDef = metadata.eventDefs[i7];
						string eventName = metadata.GetStringFromIndexWithTranslate(eventDef.nameIndex);
						Il2CppType eventType = il2Cpp.types[eventDef.typeIndex];
						TypeReference eventTypeRef = GetTypeReference(typeDefinition4, eventType);
						EventDefinition eventDefinition = new EventDefinition(eventName, (EventAttributes)eventType.attrs, eventTypeRef);
						if (eventDef.add >= 0)
						{
							eventDefinition.AddMethod = methodDefinitionDic[typeDef4.methodStart + eventDef.add];
						}
						if (eventDef.remove >= 0)
						{
							eventDefinition.RemoveMethod = methodDefinitionDic[typeDef4.methodStart + eventDef.remove];
						}
						if (eventDef.raise >= 0)
						{
							eventDefinition.InvokeMethod = methodDefinitionDic[typeDef4.methodStart + eventDef.raise];
						}
						typeDefinition4.Events.Add(eventDefinition);
						eventDefinitionDic.Add(i7, eventDefinition);
						if (generateAttributes)
						{
							CustomAttribute customTokenAttribute5 = new CustomAttribute(typeDefinition4.Module.ImportReference(tokenAttribute))
							{
								Fields = 
								{
									new CustomAttributeNamedArgument("Token", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", eventDef.token)))
								}
							};
							eventDefinition.CustomAttributes.Add(customTokenAttribute5);
						}
					}
				}
			}
			if (!(il2Cpp.Version > 20f && generateAttributes))
			{
				return;
			}
			PrepareCustomAttribute();
			Il2CppImageDefinition[] imageDefs3 = metadata.imageDefs;
			foreach (Il2CppImageDefinition imageDef3 in imageDefs3)
			{
				long typeEnd3 = imageDef3.typeStart + imageDef3.typeCount;
				for (int index5 = imageDef3.typeStart; index5 < typeEnd3; index5++)
				{
					Il2CppTypeDefinition typeDef5 = metadata.typeDefs[index5];
					TypeDefinition typeDefinition5 = typeDefinitionDic[typeDef5];
					CreateCustomAttribute(imageDef3, typeDef5.customAttributeIndex, typeDef5.token, typeDefinition5.Module, typeDefinition5.CustomAttributes);
					int fieldEnd2 = typeDef5.fieldStart + typeDef5.field_count;
					for (int i8 = typeDef5.fieldStart; i8 < fieldEnd2; i8++)
					{
						Il2CppFieldDefinition fieldDef2 = metadata.fieldDefs[i8];
						FieldDefinition fieldDefinition2 = fieldDefinitionDic[i8];
						CreateCustomAttribute(imageDef3, fieldDef2.customAttributeIndex, fieldDef2.token, typeDefinition5.Module, fieldDefinition2.CustomAttributes);
					}
					int methodEnd2 = typeDef5.methodStart + typeDef5.method_count;
					for (int i9 = typeDef5.methodStart; i9 < methodEnd2; i9++)
					{
						Il2CppMethodDefinition methodDef2 = metadata.methodDefs[i9];
						MethodDefinition methodDefinition2 = methodDefinitionDic[i9];
						CreateCustomAttribute(imageDef3, methodDef2.customAttributeIndex, methodDef2.token, typeDefinition5.Module, methodDefinition2.CustomAttributes);
						for (int j3 = 0; j3 < methodDef2.parameterCount; j3++)
						{
							Il2CppParameterDefinition parameterDef2 = metadata.parameterDefs[methodDef2.parameterStart + j3];
							ParameterDefinition parameterDefinition2 = parameterDefinitionDic[methodDef2.parameterStart + j3];
							CreateCustomAttribute(imageDef3, parameterDef2.customAttributeIndex, parameterDef2.token, typeDefinition5.Module, parameterDefinition2.CustomAttributes);
						}
					}
					int propertyEnd2 = typeDef5.propertyStart + typeDef5.property_count;
					for (int i10 = typeDef5.propertyStart; i10 < propertyEnd2; i10++)
					{
						Il2CppPropertyDefinition propertyDef2 = metadata.propertyDefs[i10];
						PropertyDefinition propertyDefinition2 = propertyDefinitionDic[i10];
						CreateCustomAttribute(imageDef3, propertyDef2.customAttributeIndex, propertyDef2.token, typeDefinition5.Module, propertyDefinition2.CustomAttributes);
					}
					int eventEnd2 = typeDef5.eventStart + typeDef5.event_count;
					for (int i11 = typeDef5.eventStart; i11 < eventEnd2; i11++)
					{
						Il2CppEventDefinition eventDef2 = metadata.eventDefs[i11];
						EventDefinition eventDefinition2 = eventDefinitionDic[i11];
						CreateCustomAttribute(imageDef3, eventDef2.customAttributeIndex, eventDef2.token, typeDefinition5.Module, eventDefinition2.CustomAttributes);
					}
				}
			}
		}

		private TypeReference GetTypeReferenceWithByRef(MemberReference memberReference, Il2CppType il2CppType)
		{
			TypeReference typeReference = GetTypeReference(memberReference, il2CppType);
			if (il2CppType.byref == 1)
			{
				return new ByReferenceType(typeReference);
			}
			return typeReference;
		}

		private TypeReference GetTypeReference(MemberReference memberReference, Il2CppType il2CppType)
		{
			ModuleDefinition moduleDefinition = memberReference.Module;
			switch (il2CppType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_OBJECT:
				return moduleDefinition.ImportReference(typeof(object));
			case Il2CppTypeEnum.IL2CPP_TYPE_VOID:
				return moduleDefinition.ImportReference(typeof(void));
			case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
				return moduleDefinition.ImportReference(typeof(bool));
			case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
				return moduleDefinition.ImportReference(typeof(char));
			case Il2CppTypeEnum.IL2CPP_TYPE_I1:
				return moduleDefinition.ImportReference(typeof(sbyte));
			case Il2CppTypeEnum.IL2CPP_TYPE_U1:
				return moduleDefinition.ImportReference(typeof(byte));
			case Il2CppTypeEnum.IL2CPP_TYPE_I2:
				return moduleDefinition.ImportReference(typeof(short));
			case Il2CppTypeEnum.IL2CPP_TYPE_U2:
				return moduleDefinition.ImportReference(typeof(ushort));
			case Il2CppTypeEnum.IL2CPP_TYPE_I4:
				return moduleDefinition.ImportReference(typeof(int));
			case Il2CppTypeEnum.IL2CPP_TYPE_U4:
				return moduleDefinition.ImportReference(typeof(uint));
			case Il2CppTypeEnum.IL2CPP_TYPE_I:
				return moduleDefinition.ImportReference(typeof(IntPtr));
			case Il2CppTypeEnum.IL2CPP_TYPE_U:
				return moduleDefinition.ImportReference(typeof(UIntPtr));
			case Il2CppTypeEnum.IL2CPP_TYPE_I8:
				return moduleDefinition.ImportReference(typeof(long));
			case Il2CppTypeEnum.IL2CPP_TYPE_U8:
				return moduleDefinition.ImportReference(typeof(ulong));
			case Il2CppTypeEnum.IL2CPP_TYPE_R4:
				return moduleDefinition.ImportReference(typeof(float));
			case Il2CppTypeEnum.IL2CPP_TYPE_R8:
				return moduleDefinition.ImportReference(typeof(double));
			case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
				return moduleDefinition.ImportReference(typeof(string));
			case Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF:
				return moduleDefinition.ImportReference(typeof(TypedReference));
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			{
				Il2CppTypeDefinition typeDef2 = executor.GetTypeDefinitionFromIl2CppType(il2CppType);
				TypeDefinition typeDefinition3 = typeDefinitionDic[typeDef2];
				return moduleDefinition.ImportReference(typeDefinition3);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
			{
				Il2CppArrayType arrayType = il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
				Il2CppType oriType4 = il2Cpp.GetIl2CppType(arrayType.etype);
				return new ArrayType(GetTypeReference(memberReference, oriType4), arrayType.rank);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
			{
				Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
				Il2CppTypeDefinition typeDef = executor.GetGenericClassTypeDefinition(genericClass);
				TypeDefinition typeDefinition2 = typeDefinitionDic[typeDef];
				GenericInstanceType genericInstanceType = new GenericInstanceType(moduleDefinition.ImportReference(typeDefinition2));
				Il2CppGenericInst genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(genericClass.context.class_inst);
				ulong[] pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
				ulong[] array = pointers;
				foreach (ulong pointer in array)
				{
					Il2CppType oriType3 = il2Cpp.GetIl2CppType(pointer);
					genericInstanceType.GenericArguments.Add(GetTypeReference(memberReference, oriType3));
				}
				return genericInstanceType;
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
			{
				Il2CppType oriType2 = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return new ArrayType(GetTypeReference(memberReference, oriType2));
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
			{
				MethodDefinition methodDefinition2 = memberReference as MethodDefinition;
				if (methodDefinition2 != null)
				{
					return CreateGenericParameter(executor.GetGenericParameteFromIl2CppType(il2CppType), methodDefinition2.DeclaringType);
				}
				TypeDefinition typeDefinition = (TypeDefinition)memberReference;
				return CreateGenericParameter(executor.GetGenericParameteFromIl2CppType(il2CppType), typeDefinition);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
			{
				MethodDefinition methodDefinition = (MethodDefinition)memberReference;
				return CreateGenericParameter(executor.GetGenericParameteFromIl2CppType(il2CppType), methodDefinition);
			}
			case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
			{
				Il2CppType oriType = il2Cpp.GetIl2CppType(il2CppType.data.type);
				return new PointerType(GetTypeReference(memberReference, oriType));
			}
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		private bool TryGetDefaultValue(int typeIndex, int dataIndex, out object value)
		{
			uint pointer = metadata.GetDefaultValueFromIndex(dataIndex);
			Il2CppType defaultValueType = il2Cpp.types[typeIndex];
			metadata.Position = pointer;
			switch (defaultValueType.type)
			{
			case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
				value = metadata.ReadBoolean();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_U1:
				value = metadata.ReadByte();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_I1:
				value = metadata.ReadSByte();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
				value = BitConverter.ToChar(metadata.ReadBytes(2), 0);
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_U2:
				value = metadata.ReadUInt16();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_I2:
				value = metadata.ReadInt16();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_U4:
				value = metadata.ReadUInt32();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_I4:
				value = metadata.ReadInt32();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_U8:
				value = metadata.ReadUInt64();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_I8:
				value = metadata.ReadInt64();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_R4:
				value = metadata.ReadSingle();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_R8:
				value = metadata.ReadDouble();
				return true;
			case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
			{
				int len = metadata.ReadInt32();
				value = metadata.ReadString(len);
				return true;
			}
			default:
				value = pointer;
				return false;
			}
		}

		private void PrepareCustomAttribute()
		{
			string[] array = knownAttributeNames;
			foreach (string attributeName in array)
			{
				foreach (AssemblyDefinition assemblyDefinition in Assemblies)
				{
					TypeDefinition attributeType = assemblyDefinition.MainModule.GetType(attributeName);
					if (attributeType != null)
					{
						knownAttributes.Add(attributeName, attributeType.Methods.First((MethodDefinition x) => x.Name == ".ctor"));
						break;
					}
				}
			}
		}

		private void CreateCustomAttribute(Il2CppImageDefinition imageDef, int customAttributeIndex, uint token, ModuleDefinition moduleDefinition, Collection<CustomAttribute> customAttributes)
		{
			int attributeIndex = metadata.GetCustomAttributeIndex(imageDef, customAttributeIndex, token);
			if (attributeIndex < 0)
			{
				return;
			}
			Il2CppCustomAttributeTypeRange attributeTypeRange = metadata.attributeTypeRanges[attributeIndex];
			for (int i = 0; i < attributeTypeRange.count; i++)
			{
				int attributeTypeIndex = metadata.attributeTypes[attributeTypeRange.start + i];
				Il2CppType attributeType = il2Cpp.types[attributeTypeIndex];
				Il2CppTypeDefinition typeDef = executor.GetTypeDefinitionFromIl2CppType(attributeType);
				TypeDefinition typeDefinition = typeDefinitionDic[typeDef];
				MethodDefinition methodDefinition;
				if (knownAttributes.TryGetValue(typeDefinition.FullName, out methodDefinition))
				{
					CustomAttribute customAttribute = new CustomAttribute(moduleDefinition.ImportReference(methodDefinition));
					customAttributes.Add(customAttribute);
					continue;
				}
				ulong methodPointer = executor.customAttributeGenerators[attributeIndex];
				ulong fixedMethodPointer = il2Cpp.GetRVA(methodPointer);
				CustomAttribute customAttribute2 = new CustomAttribute(moduleDefinition.ImportReference(attributeAttribute));
				CustomAttributeNamedArgument name = new CustomAttributeNamedArgument("Name", new CustomAttributeArgument(stringType, typeDefinition.Name));
				CustomAttributeNamedArgument rva = new CustomAttributeNamedArgument("RVA", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", fixedMethodPointer)));
				CustomAttributeNamedArgument offset = new CustomAttributeNamedArgument("Offset", new CustomAttributeArgument(stringType, string.Format("0x{0:X}", il2Cpp.MapVATR(methodPointer))));
				customAttribute2.Fields.Add(name);
				customAttribute2.Fields.Add(rva);
				customAttribute2.Fields.Add(offset);
				customAttributes.Add(customAttribute2);
			}
		}

		private GenericParameter CreateGenericParameter(Il2CppGenericParameter param, IGenericParameterProvider iGenericParameterProvider)
		{
			GenericParameter genericParameter;
			if (!genericParameterDic.TryGetValue(param, out genericParameter))
			{
				string genericName = metadata.GetStringFromIndexWithTranslate(param.nameIndex);
				genericParameter = new GenericParameter(genericName, iGenericParameterProvider);
				genericParameter.Attributes = (GenericParameterAttributes)param.flags;
				genericParameterDic.Add(param, genericParameter);
				for (int i = 0; i < param.constraintsCount; i++)
				{
					Il2CppType il2CppType = il2Cpp.types[metadata.constraintIndices[param.constraintsStart + i]];
					genericParameter.Constraints.Add(new GenericParameterConstraint(GetTypeReference((MemberReference)iGenericParameterProvider, il2CppType)));
				}
			}
			return genericParameter;
		}
	}
}
