namespace Il2CppDumper
{
	public class MT19937_64
	{
		private const ulong N = 312uL;

		private const ulong M = 156uL;

		private const ulong MATRIX_A = 13043109905998158313uL;

		private const ulong UPPER_MASK = 18446744071562067968uL;

		private const ulong LOWER_MASK = 2147483647uL;

		private static ulong[] mt = new ulong[313];

		private static ulong mti = 313uL;

		public MT19937_64(ulong seed)
		{
			Seed(seed);
		}

		public void Seed(ulong seed)
		{
			mt[0] = seed;
			for (mti = 1uL; mti < 312; mti++)
			{
				mt[mti] = 6364136223846793005L * (mt[mti - 1] ^ (mt[mti - 1] >> 62)) + mti;
			}
		}

		public ulong Int63()
		{
			ulong x = 0uL;
			ulong[] mag01 = new ulong[2] { 0uL, 13043109905998158313uL };
			if (mti >= 312)
			{
				if (mti == 313)
				{
					Seed(5489uL);
				}
				ulong kk;
				for (kk = 0uL; kk < 156; kk++)
				{
					x = (mt[kk] & 0xFFFFFFFF80000000uL) | (mt[kk + 1] & 0x7FFFFFFF);
					mt[kk] = mt[kk + 156] ^ (x >> 1) ^ mag01[x & 1];
				}
				for (; kk < 311; kk++)
				{
					x = (mt[kk] & 0xFFFFFFFF80000000uL) | (mt[kk + 1] & 0x7FFFFFFF);
					mt[kk] = mt[kk - 156] ^ (x >> 1) ^ mag01[x & 1];
				}
				x = (mt[311] & 0xFFFFFFFF80000000uL) | (mt[0] & 0x7FFFFFFF);
				mt[311] = mt[155] ^ (x >> 1) ^ mag01[x & 1];
				mti = 0uL;
			}
			x = mt[mti++];
			x ^= (x >> 29) & 0x5555555555555555L;
			x ^= (x << 17) & 0x71D67FFFEDA60000L;
			x ^= (x << 37) & 0xFFF7EEE000000000uL;
			return x ^ (x >> 43);
		}

		public ulong IntN(ulong value)
		{
			return Int63() % value;
		}
	}
}
