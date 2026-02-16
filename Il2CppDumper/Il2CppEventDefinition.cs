namespace Il2CppDumper
{
	public class Il2CppEventDefinition
	{
		public uint nameIndex;

		public int typeIndex;

		public int add;

		public int remove;

		public int raise;

		[Version(Max = 24f)]
		public int customAttributeIndex;

		[Version(Min = 19f)]
		public uint token;
	}
}
