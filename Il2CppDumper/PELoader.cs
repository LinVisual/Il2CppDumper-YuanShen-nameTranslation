using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Il2CppDumper
{
	public class PELoader
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr LoadLibrary(string path);

		public static PE Load(string fileName)
		{
			byte[] buff = File.ReadAllBytes(fileName);
			using (BinaryStream reader = new BinaryStream(new MemoryStream(buff)))
			{
				DosHeader dosHeader = reader.ReadClass<DosHeader>();
				if (dosHeader.Magic != 23117)
				{
					throw new InvalidDataException("ERROR: Invalid PE file");
				}
				reader.Position = dosHeader.Lfanew;
				if (reader.ReadUInt32() != 17744)
				{
					throw new InvalidDataException("ERROR: Invalid PE file");
				}
				FileHeader fileHeader = reader.ReadClass<FileHeader>();
				if ((fileHeader.Machine == 332 && Environment.Is64BitProcess) || (fileHeader.Machine == 34404 && !Environment.Is64BitProcess))
				{
					return new PE(new MemoryStream(buff));
				}
				ulong pos = reader.Position;
				reader.Position = pos + fileHeader.SizeOfOptionalHeader;
				SectionHeader[] sections = reader.ReadClassArray<SectionHeader>(fileHeader.NumberOfSections);
				SectionHeader last = sections.Last();
				uint size = last.VirtualAddress + last.VirtualSize;
				byte[] peBuff = new byte[size];
				IntPtr handle = LoadLibrary(fileName);
				if (handle == IntPtr.Zero)
				{
					return new PE(new MemoryStream(buff));
				}
				SectionHeader[] array = sections;
				foreach (SectionHeader section in array)
				{
					uint characteristics = section.Characteristics;
					uint num = characteristics;
					if (num == 1073741888 || num == 1610612768 || num == 3221225536u)
					{
						Marshal.Copy(new IntPtr(handle.ToInt64() + section.VirtualAddress), peBuff, (int)section.VirtualAddress, (int)section.VirtualSize);
					}
				}
				MemoryStream peMemory = new MemoryStream(peBuff);
				BinaryWriter writer = new BinaryWriter(peMemory, Encoding.UTF8, true);
				ulong headerSize = reader.Position;
				reader.Position = 0uL;
				byte[] buff2 = reader.ReadBytes((int)headerSize);
				writer.Write(buff2);
				writer.Flush();
				writer.Close();
				peMemory.Position = 0L;
				PE pe = new PE(peMemory);
				pe.LoadFromMemory((ulong)handle.ToInt64());
				return pe;
			}
		}
	}
}
