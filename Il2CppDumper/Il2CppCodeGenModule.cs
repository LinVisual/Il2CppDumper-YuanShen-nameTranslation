namespace Il2CppDumper
{
	public class Il2CppCodeGenModule
	{
		public ulong moduleName;

		public long methodPointerCount;

		public ulong methodPointers;

		public ulong invokerIndices;

		public ulong reversePInvokeWrapperCount;

		public ulong reversePInvokeWrapperIndices;

		public long rgctxRangesCount;

		public ulong rgctxRanges;

		public long rgctxsCount;

		public ulong rgctxs;

		public ulong debuggerMetadata;

		[Version(Min = 27f)]
		public ulong customAttributeCacheGenerator;

		[Version(Min = 27f)]
		public ulong moduleInitializer;

		[Version(Min = 27f)]
		public ulong staticConstructorTypeIndices;

		[Version(Min = 27f)]
		public ulong metadataRegistration;

		[Version(Min = 27f)]
		public ulong codeRegistaration;
	}
}
