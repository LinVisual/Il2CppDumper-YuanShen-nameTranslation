using System.Collections.Generic;

namespace Il2CppDumper
{
	public class StructInfo
	{
		public string TypeName;

		public bool IsValueType;

		public string Parent;

		public List<StructFieldInfo> Fields = new List<StructFieldInfo>();

		public List<StructFieldInfo> StaticFields = new List<StructFieldInfo>();

		public List<StructVTableMethodInfo> VTableMethod = new List<StructVTableMethodInfo>();

		public List<StructRGCTXInfo> RGCTXs = new List<StructRGCTXInfo>();
	}
}
