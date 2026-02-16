namespace Il2CppDumper
{
	public class Il2CppMetadataRegistration
	{
		public long genericClassesCount;

		public ulong genericClasses;

		public long genericInstsCount;

		public ulong genericInsts;

		public long genericMethodTableCount;

		public ulong genericMethodTable;

		public long typesCount;

		public ulong types;

		public long methodSpecsCount;

		public ulong methodSpecs;

		[Version(Max = 16f)]
		public long methodReferencesCount;

		[Version(Max = 16f)]
		public ulong methodReferences;

		public long fieldOffsetsCount;

		public ulong fieldOffsets;

		public long typeDefinitionsSizesCount;

		public ulong typeDefinitionsSizes;

		[Version(Min = 19f)]
		public ulong metadataUsagesCount;

		[Version(Min = 19f)]
		public ulong metadataUsages;
	}
}
