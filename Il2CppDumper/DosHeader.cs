namespace Il2CppDumper
{
	public class DosHeader
	{
		public ushort Magic;

		public ushort Cblp;

		public ushort Cp;

		public ushort Crlc;

		public ushort Cparhdr;

		public ushort Minalloc;

		public ushort Maxalloc;

		public ushort Ss;

		public ushort Sp;

		public ushort Csum;

		public ushort Ip;

		public ushort Cs;

		public ushort Lfarlc;

		public ushort Ovno;

		[ArrayLength(Length = 4)]
		public ushort[] Res;

		public ushort Oemid;

		public ushort Oeminfo;

		[ArrayLength(Length = 10)]
		public ushort[] Res2;

		public uint Lfanew;
	}
}
