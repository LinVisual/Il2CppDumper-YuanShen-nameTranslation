using System.IO;
using Mono.Cecil;

namespace Il2CppDumper
{
	public static class DummyAssemblyExporter
	{
		public static void Export(Il2CppExecutor il2CppExecutor, string outputDir, bool generateAttributes = false)
		{
			Directory.SetCurrentDirectory(outputDir);
			if (Directory.Exists("DummyDll"))
			{
				Directory.Delete("DummyDll", true);
			}
			Directory.CreateDirectory("DummyDll");
			Directory.SetCurrentDirectory("DummyDll");
			DummyAssemblyGenerator dummy = new DummyAssemblyGenerator(il2CppExecutor, generateAttributes);
			for (int i = 0; i < dummy.Assemblies.Count; i++)
			{
				AssemblyDefinition assembly = dummy.Assemblies[i];
				using (MemoryStream stream = new MemoryStream())
				{
					assembly.Write(stream);
					File.WriteAllBytes(assembly.MainModule.Name, stream.ToArray());
				}
			}
		}
	}
}
