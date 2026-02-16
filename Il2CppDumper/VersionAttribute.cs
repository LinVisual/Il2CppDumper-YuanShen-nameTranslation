using System;

namespace Il2CppDumper
{
	[AttributeUsage(AttributeTargets.Field)]
	internal class VersionAttribute : Attribute
	{
		public float Min { get; set; } = 0f;

		public float Max { get; set; } = 99f;
	}
}
