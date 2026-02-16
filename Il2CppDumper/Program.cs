using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Il2CppDumper
{
	internal class Program
	{
		[STAThread]
		private static void Main(string[] args)
		{
			string il2cppPath = null;
			string metadataPath = null;
			string nameTranslationPath = null;
			string outputDir = null;
			bool generateAttributes = false;
			if (outputDir == null)
			{
				outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
			}
			if (!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}
			if (il2cppPath == null)
			{
				OpenFileDialog ofd = new OpenFileDialog();
				ofd.Filter = "UserAssembly|UserAssembly.dll";
				if (ofd.ShowDialog() != DialogResult.OK)
				{
					return;
				}
				il2cppPath = ofd.FileName;
				ofd.Filter = "global-metadata|global-metadata.dat";
				if (ofd.ShowDialog() != DialogResult.OK)
				{
					return;
				}
				metadataPath = ofd.FileName;
				ofd.Title = "Open nameTranslation.txt if you have one, otherwise just hit cancel";
				ofd.Filter = "BeeByte Obfuscator mappings|nameTranslation.txt";
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					nameTranslationPath = ofd.FileName;
				}
				Console.WriteLine("Need to generate Il2CppDumper attributes for DummyDll? (Y/N)");
				string input = Console.ReadLine();
				if (input == "Y" || input == "y")
				{
					generateAttributes = true;
				}
			}
			if (il2cppPath == null)
			{
				return;
			}
			try
			{
				Metadata metadata;
				Il2Cpp il2Cpp;
				if (Init(il2cppPath, metadataPath, nameTranslationPath, out metadata, out il2Cpp))
				{
					Dump(metadata, il2Cpp, outputDir, generateAttributes);
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey(true);
		}

		private static bool Init(string il2cppPath, string metadataPath, string nameTranslationPath, out Metadata metadata, out Il2Cpp il2Cpp)
		{
			Console.WriteLine("Initializing metadata...");
			byte[] metadataBytes = File.ReadAllBytes(metadataPath);
			MetadataDecryption.StringDecryptionData stringDecryptionInfo = MetadataDecryption.DecryptMetadata(metadataBytes);
			metadata = new Metadata(new MemoryStream(metadataBytes), stringDecryptionInfo, nameTranslationPath);
			Console.WriteLine(string.Format("Metadata Version: {0}", metadata.Version));
			Console.WriteLine("Initializing il2cpp file...");
			byte[] il2cppBytes = File.ReadAllBytes(il2cppPath);
			uint il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
			MemoryStream il2CppMemory = new MemoryStream(il2cppBytes);
			uint num = il2cppMagic;
			if (num != 9460301)
			{
				throw new NotSupportedException("ERROR: il2cpp file not supported.");
			}
			il2Cpp = new PE(il2CppMemory);
			float version = metadata.Version;
			il2Cpp.SetProperties(version, metadata.maxMetadataUsages);
			Console.WriteLine(string.Format("Il2Cpp Version: {0}", il2Cpp.Version));
			Console.WriteLine("Searching...");
			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && il2Cpp is PE)
				{
					Console.WriteLine("Use custom PE loader");
					il2Cpp = PELoader.Load(il2cppPath);
					il2Cpp.SetProperties(version, metadata.maxMetadataUsages);
				}
				ProcessModuleCollection pms = Process.GetCurrentProcess().Modules;
				ulong baseaddr = 0uL;
				ProcessModule targetModule = null;
				foreach (ProcessModule pm in pms)
				{
					if (pm.ModuleName == "UserAssembly.dll")
					{
						baseaddr = (ulong)(long)pm.BaseAddress;
						targetModule = pm;
						break;
					}
				}
				Console.WriteLine("baseadr: 0x" + baseaddr.ToString("x2"));
				ulong codeRegistration = 0uL;
				ulong metadataRegistration = 0uL;
				ulong text_start = ((PE)il2Cpp).Sections[0].VirtualAddress + baseaddr;
				ulong text_end = text_start + ((PE)il2Cpp).Sections[0].VirtualSize;
				byte[] d = new byte[22];
				for (ulong ptr = text_start; ptr < text_end - 22; ptr += 16)
				{
					Marshal.Copy((IntPtr)(long)ptr, d, 0, 22);
					if (d[0] == 76 && d[1] == 141 && d[2] == 5 && d[7] == 72 && d[8] == 141 && d[9] == 21 && d[14] == 72 && d[15] == 141 && d[16] == 13 && d[21] == 233)
					{
						codeRegistration = ptr + 21 + BitConverter.ToUInt32(d, 17);
						metadataRegistration = ptr + 14 + BitConverter.ToUInt32(d, 10);
						Console.WriteLine("Found the offsets! codeRegistration: 0x" + (codeRegistration - baseaddr).ToString("X2") + ", metadataRegistration: 0x" + (metadataRegistration - baseaddr).ToString("X2"));
						break;
					}
				}
				if (codeRegistration == 0L && metadataRegistration == 0)
				{
					Console.WriteLine("Failed to find CodeRegistration and MetadataRegistration, go yell at Khang");
					return false;
				}
				il2Cpp.Init(codeRegistration, metadataRegistration);
				return true;
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
				Console.WriteLine("ERROR: An error occurred while processing.");
				return false;
			}
		}

		private static void Dump(Metadata metadata, Il2Cpp il2Cpp, string outputDir, bool generateAttributes = false)
		{
			Console.WriteLine("Dumping...");
			Il2CppExecutor executor = new Il2CppExecutor(metadata, il2Cpp);
			Console.WriteLine("Done!");
			Console.WriteLine("Generate script...");
			ScriptGenerator scriptGenerator = new ScriptGenerator(executor);
			scriptGenerator.WriteScript(outputDir);
			Console.WriteLine("Done!");
			Console.WriteLine("Generate dummy dll...");
			DummyAssemblyExporter.Export(executor, outputDir, generateAttributes);
			Console.WriteLine("Done!");
		}
	}
}
