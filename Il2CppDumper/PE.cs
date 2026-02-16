using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppDumper
{
	public sealed class PE : Il2Cpp
	{
		private SectionHeader[] sections;

		private ulong imageBase;

		public SectionHeader[] Sections
		{
			get
			{
				return sections;
			}
		}

		public PE(Stream stream)
			: base(stream)
		{
			DosHeader dosHeader = ReadClass<DosHeader>();
			if (dosHeader.Magic != 23117)
			{
				throw new InvalidDataException("ERROR: Invalid PE file");
			}
			base.Position = dosHeader.Lfanew;
			if (ReadUInt32() != 17744)
			{
				throw new InvalidDataException("ERROR: Invalid PE file");
			}
			FileHeader fileHeader = ReadClass<FileHeader>();
			ulong pos = base.Position;
			if (fileHeader.Machine == 332)
			{
				Is32Bit = true;
				OptionalHeader optionalHeader = ReadClass<OptionalHeader>();
				imageBase = optionalHeader.ImageBase;
			}
			else
			{
				if (fileHeader.Machine != 34404)
				{
					throw new NotSupportedException("ERROR: Unsupported machine.");
				}
				OptionalHeader64 optionalHeader2 = ReadClass<OptionalHeader64>();
				imageBase = optionalHeader2.ImageBase;
			}
			base.Position = pos + fileHeader.SizeOfOptionalHeader;
			sections = ReadClassArray<SectionHeader>(fileHeader.NumberOfSections);
		}

		public void LoadFromMemory(ulong addr)
		{
			imageBase = addr;
			SectionHeader[] array = sections;
			foreach (SectionHeader section in array)
			{
				section.PointerToRawData = section.VirtualAddress;
				section.SizeOfRawData = section.VirtualSize;
			}
		}

		public override ulong MapVATR(ulong absAddr)
		{
			ulong addr = absAddr - imageBase;
			SectionHeader section = sections.FirstOrDefault((SectionHeader x) => addr >= x.VirtualAddress && addr <= x.VirtualAddress + x.VirtualSize);
			if (section == null)
			{
				return 0uL;
			}
			return addr - (section.VirtualAddress - section.PointerToRawData);
		}

		public override bool Search()
		{
			return false;
		}

		public override bool PlusSearch(int methodCount, int typeDefinitionsCount)
		{
			List<SectionHeader> execList = new List<SectionHeader>();
			List<SectionHeader> dataList = new List<SectionHeader>();
			SectionHeader[] array = sections;
			foreach (SectionHeader section in array)
			{
				switch (section.Characteristics)
				{
				case 1610612768u:
					execList.Add(section);
					break;
				case 3221225536u:
				case 1073741888u:
					dataList.Add(section);
					break;
				}
			}
			PlusSearch plusSearch = new PlusSearch(this, methodCount, typeDefinitionsCount, maxMetadataUsages);
			SectionHeader[] data = dataList.ToArray();
			SectionHeader[] exec = execList.ToArray();
			plusSearch.SetSection(SearchSectionType.Exec, imageBase, exec);
			plusSearch.SetSection(SearchSectionType.Data, imageBase, data);
			plusSearch.SetSection(SearchSectionType.Bss, imageBase, data);
			ulong codeRegistration = plusSearch.FindCodeRegistration();
			ulong metadataRegistration = plusSearch.FindMetadataRegistration();
			return AutoPlusInit(codeRegistration, metadataRegistration);
		}

		public override bool SymbolSearch()
		{
			return false;
		}

		public override ulong GetRVA(ulong pointer)
		{
			return pointer - imageBase;
		}
	}
}
