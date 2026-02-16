namespace Il2CppDumper
{
	public class Il2CppCodeRegistration
	{
		[Version(Max = 24.1f)]
		public long methodPointersCount;

		[Version(Max = 24.1f)]
		public ulong methodPointers;

		[Version(Max = 21f)]
		public ulong delegateWrappersFromNativeToManagedCount;

		[Version(Max = 21f)]
		public ulong delegateWrappersFromNativeToManaged;

		[Version(Min = 22f)]
		public long reversePInvokeWrapperCount;

		[Version(Min = 22f)]
		public ulong reversePInvokeWrappers;

		[Version(Max = 22f)]
		public ulong delegateWrappersFromManagedToNativeCount;

		[Version(Max = 22f)]
		public ulong delegateWrappersFromManagedToNative;

		[Version(Max = 22f)]
		public ulong marshalingFunctionsCount;

		[Version(Max = 22f)]
		public ulong marshalingFunctions;

		[Version(Min = 21f, Max = 22f)]
		public ulong ccwMarshalingFunctionsCount;

		[Version(Min = 21f, Max = 22f)]
		public ulong ccwMarshalingFunctions;

		public long genericMethodPointersCount;

		public ulong genericMethodPointers;

		public long invokerPointersCount;

		public ulong invokerPointers;

		[Version(Max = 24.3f)]
		public long customAttributeCount;

		[Version(Max = 24.3f)]
		public ulong customAttributeGenerators;

		[Version(Min = 21f, Max = 22f)]
		public long guidCount;

		[Version(Min = 21f, Max = 22f)]
		public ulong guids;

		[Version(Min = 22f)]
		public long unresolvedVirtualCallCount;

		[Version(Min = 22f)]
		public ulong unresolvedVirtualCallPointers;

		[Version(Min = 23f)]
		public ulong interopDataCount;

		[Version(Min = 23f)]
		public ulong interopData;

		[Version(Min = 24.3f)]
		public ulong windowsRuntimeFactoryCount;

		[Version(Min = 24.3f)]
		public ulong windowsRuntimeFactoryTable;

		[Version(Min = 24.2f)]
		public long codeGenModulesCount;

		[Version(Min = 24.2f)]
		public ulong codeGenModules;
	}
}
