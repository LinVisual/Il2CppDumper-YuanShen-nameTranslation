using System.Collections.Generic;

namespace Il2CppDumper
{
	public class ScriptJson
	{
		public List<ScriptMethod> ScriptMethod = new List<ScriptMethod>();

		public List<ScriptString> ScriptString = new List<ScriptString>();

		public List<ScriptMetadata> ScriptMetadata = new List<ScriptMetadata>();

		public List<ScriptMetadataMethod> ScriptMetadataMethod = new List<ScriptMetadataMethod>();

		public List<ulong> Addresses = new List<ulong>();
	}
}
