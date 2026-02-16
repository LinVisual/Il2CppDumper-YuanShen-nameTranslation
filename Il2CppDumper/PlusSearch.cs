using System.Collections.Generic;
using System.Linq;

namespace Il2CppDumper
{
	public class PlusSearch
	{
		private Il2Cpp il2Cpp;

		private int methodCount;

		private int typeDefinitionsCount;

		private long maxMetadataUsages;

		private List<SearchSection> exec;

		private List<SearchSection> data;

		private List<SearchSection> bss;

		private static readonly byte[] featureBytes2019 = new byte[13]
		{
			109, 115, 99, 111, 114, 108, 105, 98, 46, 100,
			108, 108, 0
		};

		private static readonly byte[] featureBytes2020dot2 = new byte[20]
		{
			65, 115, 115, 101, 109, 98, 108, 121, 45, 67,
			83, 104, 97, 114, 112, 46, 100, 108, 108, 0
		};

		public PlusSearch(Il2Cpp il2Cpp, int methodCount, int typeDefinitionsCount, long maxMetadataUsages)
		{
			this.il2Cpp = il2Cpp;
			this.methodCount = methodCount;
			this.typeDefinitionsCount = typeDefinitionsCount;
			this.maxMetadataUsages = maxMetadataUsages;
		}

		public void SetSection(SearchSectionType type, ulong imageBase, SectionHeader[] sections)
		{
			List<SearchSection> secs = new List<SearchSection>();
			foreach (SectionHeader section in sections)
			{
				if (section != null)
				{
					secs.Add(new SearchSection
					{
						offset = section.PointerToRawData,
						offsetEnd = section.PointerToRawData + section.SizeOfRawData,
						address = section.VirtualAddress + imageBase,
						addressEnd = section.VirtualAddress + section.VirtualSize + imageBase
					});
				}
			}
			SetSection(type, secs);
		}

		public void SetSection(SearchSectionType type, params SearchSection[] secs)
		{
			SetSection(type, secs.ToList());
		}

		private void SetSection(SearchSectionType type, List<SearchSection> secs)
		{
			switch (type)
			{
			case SearchSectionType.Exec:
				exec = secs;
				break;
			case SearchSectionType.Data:
				data = secs;
				break;
			case SearchSectionType.Bss:
				bss = secs;
				break;
			}
		}

		public ulong FindCodeRegistration()
		{
			if ((double)il2Cpp.Version >= 24.2)
			{
				return FindCodeRegistration2019();
			}
			return FindCodeRegistrationOld();
		}

		public ulong FindMetadataRegistration()
		{
			if (il2Cpp.Version < 19f)
			{
				return 0uL;
			}
			if (il2Cpp.Version >= 27f)
			{
				return FindMetadataRegistrationV21();
			}
			return FindMetadataRegistrationOld();
		}

		private ulong FindCodeRegistrationOld()
		{
			foreach (SearchSection section in data)
			{
				il2Cpp.Position = section.offset;
				while (il2Cpp.Position < section.offsetEnd)
				{
					ulong addr = il2Cpp.Position;
					if (il2Cpp.ReadIntPtr() == methodCount)
					{
						try
						{
							ulong pointer = il2Cpp.MapVATR(il2Cpp.ReadUIntPtr());
							if (CheckPointerRangeDataRa(pointer))
							{
								ulong[] pointers = il2Cpp.ReadClassArray<ulong>(pointer, methodCount);
								if (CheckPointerRangeExecVa(pointers))
								{
									return addr - section.offset + section.address;
								}
							}
						}
						catch
						{
						}
					}
					il2Cpp.Position = addr + il2Cpp.PointerSize;
				}
			}
			return 0uL;
		}

		private ulong FindMetadataRegistrationOld()
		{
			foreach (SearchSection section in data)
			{
				il2Cpp.Position = section.offset;
				while (il2Cpp.Position < section.offsetEnd)
				{
					ulong addr = il2Cpp.Position;
					if (il2Cpp.ReadIntPtr() == typeDefinitionsCount)
					{
						try
						{
							il2Cpp.Position += il2Cpp.PointerSize * 2;
							ulong pointer = il2Cpp.MapVATR(il2Cpp.ReadUIntPtr());
							if (CheckPointerRangeDataRa(pointer))
							{
								ulong[] pointers = il2Cpp.ReadClassArray<ulong>(pointer, maxMetadataUsages);
								if (CheckPointerRangeBssVa(pointers))
								{
									return addr - il2Cpp.PointerSize * 12 - section.offset + section.address;
								}
							}
						}
						catch
						{
						}
					}
					il2Cpp.Position = addr + il2Cpp.PointerSize;
				}
			}
			return 0uL;
		}

		private ulong FindMetadataRegistrationV21()
		{
			foreach (SearchSection section in data)
			{
				il2Cpp.Position = section.offset;
				while (il2Cpp.Position < section.offsetEnd)
				{
					ulong addr = il2Cpp.Position;
					if (il2Cpp.ReadIntPtr() == typeDefinitionsCount)
					{
						il2Cpp.Position += il2Cpp.PointerSize;
						if (il2Cpp.ReadIntPtr() == typeDefinitionsCount)
						{
							ulong pointer = il2Cpp.MapVATR(il2Cpp.ReadUIntPtr());
							if (CheckPointerRangeDataRa(pointer))
							{
								ulong[] pointers = il2Cpp.ReadClassArray<ulong>(pointer, typeDefinitionsCount);
								if (CheckPointerRangeDataVa(pointers))
								{
									return addr - il2Cpp.PointerSize * 10 - section.offset + section.address;
								}
							}
						}
					}
					il2Cpp.Position = addr + il2Cpp.PointerSize;
				}
			}
			return 0uL;
		}

		private bool CheckPointerRangeDataRa(ulong pointer)
		{
			return data.Any((SearchSection x) => pointer >= x.offset && pointer <= x.offsetEnd);
		}

		private bool CheckPointerRangeExecVa(ulong[] pointers)
		{
			return pointers.All((ulong x) => exec.Any((SearchSection y) => x >= y.address && x <= y.addressEnd));
		}

		private bool CheckPointerRangeDataVa(ulong[] pointers)
		{
			return pointers.All((ulong x) => data.Any((SearchSection y) => x >= y.address && x <= y.addressEnd));
		}

		private bool CheckPointerRangeBssVa(ulong[] pointers)
		{
			return pointers.All((ulong x) => bss.Any((SearchSection y) => x >= y.address && x <= y.addressEnd));
		}

		private ulong FindCodeRegistration2019()
		{
			byte[] featureBytes = ((il2Cpp.Version >= 27f) ? featureBytes2020dot2 : featureBytes2019);
			List<SearchSection> secs = data;
			foreach (SearchSection sec in secs)
			{
				il2Cpp.Position = sec.offset;
				byte[] buff = il2Cpp.ReadBytes((int)(sec.offsetEnd - sec.offset));
				foreach (int index in buff.Search(featureBytes))
				{
					ulong va = (ulong)index + sec.address;
					foreach (SearchSection dataSec in data)
					{
						il2Cpp.Position = dataSec.offset;
						while (il2Cpp.Position < dataSec.offsetEnd)
						{
							ulong offset = il2Cpp.Position;
							if (il2Cpp.ReadUIntPtr() == va)
							{
								ulong va2 = offset - dataSec.offset + dataSec.address;
								foreach (SearchSection dataSec2 in data)
								{
									il2Cpp.Position = dataSec2.offset;
									while (il2Cpp.Position < dataSec2.offsetEnd)
									{
										ulong offset2 = il2Cpp.Position;
										if (il2Cpp.ReadUIntPtr() == va2)
										{
											ulong va3 = offset2 - dataSec2.offset + dataSec2.address;
											foreach (SearchSection dataSec3 in data)
											{
												il2Cpp.Position = dataSec3.offset;
												while (il2Cpp.Position < dataSec3.offsetEnd)
												{
													ulong offset3 = il2Cpp.Position;
													if (il2Cpp.ReadUIntPtr() == va3)
													{
														ulong offset4 = offset3 - dataSec3.offset + dataSec3.address;
														return offset4 - il2Cpp.PointerSize * 13;
													}
												}
											}
										}
										il2Cpp.Position = offset2 + il2Cpp.PointerSize;
									}
								}
							}
							il2Cpp.Position = offset + il2Cpp.PointerSize;
						}
					}
				}
			}
			return 0uL;
		}
	}
}
