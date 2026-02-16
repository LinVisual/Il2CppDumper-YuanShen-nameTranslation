namespace Il2CppDumper
{
	public class Il2CppTypeDefinition
	{
		public uint nameIndex;

		public uint namespaceIndex;

		public int customAttributeIndex;

		public int byvalTypeIndex;

		public int byrefTypeIndex;

		public int declaringTypeIndex;

		public int parentIndex;

		public int elementTypeIndex;

		public int rgctxStartIndex;

		public int rgctxCount;

		public int genericContainerIndex;

		public uint flags;

		public int fieldStart;

		public int propertyStart;

		public int methodStart;

		public int eventStart;

		public int nestedTypesStart;

		public int interfacesStart;

		public int interfaceOffsetsStart;

		public int vtableStart;

		public ushort event_count;

		public ushort method_count;

		public ushort property_count;

		public ushort field_count;

		public ushort vtable_count;

		public ushort interfaces_count;

		public ushort interface_offsets_count;

		public ushort nested_type_count;

		public uint bitfield;

		public uint token;

		public bool IsValueType
		{
			get
			{
				return (bitfield & 1) == 1;
			}
		}

		public bool IsEnum
		{
			get
			{
				return ((bitfield >> 1) & 1) == 1;
			}
		}
	}
}
