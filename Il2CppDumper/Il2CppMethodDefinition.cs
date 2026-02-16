namespace Il2CppDumper
{
	public class Il2CppMethodDefinition
	{
		public int returnType;

		public int declaringType;

		public uint Padding1;

		public uint nameIndex;

		public int parameterStart;

		public int genericContainerIndex;

		public int customAttributeIndex;

		public uint Padding2;

		public uint Padding3;

		public int methodIndex;

		public int invokerIndex;

		public int rgctxCount;

		public int rgctxStartIndex;

		public ushort parameterCount;

		public ushort flags;

		public ushort slot;

		public ushort iflags;

		public uint token;
	}
}
