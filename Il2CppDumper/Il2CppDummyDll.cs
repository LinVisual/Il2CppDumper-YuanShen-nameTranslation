using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Il2CppDumper
{
	internal static class Il2CppDummyDll
	{
		private static Type attributeType;

		private static ConstructorInfo attributeConstructor;

		static Il2CppDummyDll()
		{
			attributeType = typeof(Attribute);
			attributeConstructor = attributeType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
		}

		public static AssemblyDefinition Create()
		{
			AssemblyNameDefinition assemblyName = new AssemblyNameDefinition("Il2CppDummyDll", new Version("3.7.1.6"));
			AssemblyDefinition assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, "Il2CppDummyDll.dll", ModuleKind.Dll);
			TypeReference stringTypeReference = assemblyDefinition.MainModule.TypeSystem.String;
			TypeReference attributeTypeReference = assemblyDefinition.MainModule.ImportReference(attributeType);
			Collection<TypeDefinition> types = assemblyDefinition.MainModule.Types;
			string namespaceName = "Il2CppDummyDll";
			TypeDefinition addressAttribute = new TypeDefinition(namespaceName, "AddressAttribute", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit, attributeTypeReference);
			addressAttribute.Fields.Add(new FieldDefinition("RVA", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			addressAttribute.Fields.Add(new FieldDefinition("Offset", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			addressAttribute.Fields.Add(new FieldDefinition("VA", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			addressAttribute.Fields.Add(new FieldDefinition("Slot", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			types.Add(addressAttribute);
			CreateDefaultConstructor(addressAttribute);
			TypeDefinition fieldOffsetAttribute = new TypeDefinition(namespaceName, "FieldOffsetAttribute", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit, attributeTypeReference);
			fieldOffsetAttribute.Fields.Add(new FieldDefinition("Offset", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			types.Add(fieldOffsetAttribute);
			CreateDefaultConstructor(fieldOffsetAttribute);
			TypeDefinition attributeAttribute = new TypeDefinition(namespaceName, "AttributeAttribute", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit, attributeTypeReference);
			attributeAttribute.Fields.Add(new FieldDefinition("Name", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			attributeAttribute.Fields.Add(new FieldDefinition("RVA", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			attributeAttribute.Fields.Add(new FieldDefinition("Offset", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			types.Add(attributeAttribute);
			CreateDefaultConstructor(attributeAttribute);
			TypeDefinition metadataOffsetAttribute = new TypeDefinition(namespaceName, "MetadataOffsetAttribute", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit, attributeTypeReference);
			metadataOffsetAttribute.Fields.Add(new FieldDefinition("Offset", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			types.Add(metadataOffsetAttribute);
			CreateDefaultConstructor(metadataOffsetAttribute);
			TypeDefinition tokenAttribute = new TypeDefinition(namespaceName, "TokenAttribute", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit, attributeTypeReference);
			tokenAttribute.Fields.Add(new FieldDefinition("Token", Mono.Cecil.FieldAttributes.Public, stringTypeReference));
			types.Add(tokenAttribute);
			CreateDefaultConstructor(tokenAttribute);
			return assemblyDefinition;
		}

		private static void CreateDefaultConstructor(TypeDefinition typeDefinition)
		{
			ModuleDefinition module = typeDefinition.Module;
			MethodDefinition defaultConstructor = new MethodDefinition(".ctor", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.ImportReference(typeof(void)));
			ILProcessor processor = defaultConstructor.Body.GetILProcessor();
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, module.ImportReference(attributeConstructor));
			processor.Emit(OpCodes.Ret);
			typeDefinition.Methods.Add(defaultConstructor);
		}
	}
}
