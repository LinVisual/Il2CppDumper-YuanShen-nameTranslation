namespace Il2CppDumper
{
	public class Il2CppType
	{
		public class Union
		{
			public ulong dummy;

			public long klassIndex
			{
				get
				{
					return (long)dummy;
				}
			}

			public ulong typeHandle
			{
				get
				{
					return dummy;
				}
			}

			public ulong type
			{
				get
				{
					return dummy;
				}
			}

			public ulong array
			{
				get
				{
					return dummy;
				}
			}

			public long genericParameterIndex
			{
				get
				{
					return (long)dummy;
				}
			}

			public ulong genericParameterHandle
			{
				get
				{
					return dummy;
				}
			}

			public ulong generic_class
			{
				get
				{
					return dummy;
				}
			}
		}

		public ulong datapoint;

		public uint bits;

		public Union data { get; set; }

		public uint attrs { get; set; }

		public Il2CppTypeEnum type { get; set; }

		public uint num_mods { get; set; }

		public uint byref { get; set; }

		public uint pinned { get; set; }

		public void Init()
		{
			attrs = bits & 0xFFFF;
			type = (Il2CppTypeEnum)((bits >> 16) & 0xFF);
			num_mods = (bits >> 24) & 0x3F;
			byref = (bits >> 30) & 1;
			pinned = bits >> 31;
			data = new Union
			{
				dummy = datapoint
			};
		}
	}
}
