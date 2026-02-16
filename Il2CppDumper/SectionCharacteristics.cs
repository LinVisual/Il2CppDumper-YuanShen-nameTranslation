using System;

namespace Il2CppDumper
{
	[Flags]
	public enum SectionCharacteristics : uint
	{
		IMAGE_SCN_MEM_EXECUTE = 0x20000000u,
		IMAGE_SCN_MEM_READ = 0x40000000u,
		IMAGE_SCN_MEM_WRITE = 0x80000000u
	}
}
