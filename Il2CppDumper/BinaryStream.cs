using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Il2CppDumper
{
	public class BinaryStream : IDisposable
	{
		public float Version;

		public bool Is32Bit;

		private Stream stream;

		private BinaryReader reader;

		private BinaryWriter writer;

		private MethodInfo readClass;

		private MethodInfo readClassArray;

		private Dictionary<Type, MethodInfo> genericMethodCache = new Dictionary<Type, MethodInfo>();

		private Dictionary<FieldInfo, VersionAttribute> attributeCache = new Dictionary<FieldInfo, VersionAttribute>();

		public ulong Position
		{
			get
			{
				return (ulong)stream.Position;
			}
			set
			{
				stream.Position = (long)value;
			}
		}

		public ulong Length
		{
			get
			{
				return (ulong)stream.Length;
			}
		}

		public ulong PointerSize
		{
			get
			{
				return (ulong)(Is32Bit ? 4 : 8);
			}
		}

		public BinaryStream(Stream input)
		{
			stream = input;
			reader = new BinaryReader(stream, Encoding.UTF8, true);
			writer = new BinaryWriter(stream, Encoding.UTF8, true);
			readClass = GetType().GetMethod("ReadClass", Type.EmptyTypes);
			readClassArray = GetType().GetMethod("ReadClassArray", new Type[1] { typeof(long) });
		}

		public bool ReadBoolean()
		{
			return reader.ReadBoolean();
		}

		public byte ReadByte()
		{
			return reader.ReadByte();
		}

		public byte[] ReadBytes(int count)
		{
			return reader.ReadBytes(count);
		}

		public sbyte ReadSByte()
		{
			return reader.ReadSByte();
		}

		public short ReadInt16()
		{
			return reader.ReadInt16();
		}

		public ushort ReadUInt16()
		{
			return reader.ReadUInt16();
		}

		public int ReadInt32()
		{
			return reader.ReadInt32();
		}

		public uint ReadUInt32()
		{
			return reader.ReadUInt32();
		}

		public long ReadInt64()
		{
			return reader.ReadInt64();
		}

		public ulong ReadUInt64()
		{
			return reader.ReadUInt64();
		}

		public float ReadSingle()
		{
			return reader.ReadSingle();
		}

		public double ReadDouble()
		{
			return reader.ReadDouble();
		}

		public uint ReadULeb128()
		{
			uint value = reader.ReadByte();
			if (value >= 128)
			{
				int bitshift = 0;
				value &= 0x7F;
				byte b;
				do
				{
					b = reader.ReadByte();
					bitshift += 7;
					value |= (uint)((b & 0x7F) << bitshift);
				}
				while (b >= 128);
			}
			return value;
		}

		public void Write(bool value)
		{
			writer.Write(value);
		}

		public void Write(byte value)
		{
			writer.Write(value);
		}

		public void Write(sbyte value)
		{
			writer.Write(value);
		}

		public void Write(short value)
		{
			writer.Write(value);
		}

		public void Write(ushort value)
		{
			writer.Write(value);
		}

		public void Write(int value)
		{
			writer.Write(value);
		}

		public void Write(uint value)
		{
			writer.Write(value);
		}

		public void Write(long value)
		{
			writer.Write(value);
		}

		public void Write(ulong value)
		{
			writer.Write(value);
		}

		public void Write(float value)
		{
			writer.Write(value);
		}

		public void Write(double value)
		{
			writer.Write(value);
		}

		private object ReadPrimitive(Type type)
		{
			switch (type.Name)
			{
			case "Int32":
				return ReadInt32();
			case "UInt32":
				return ReadUInt32();
			case "Int16":
				return ReadInt16();
			case "UInt16":
				return ReadUInt16();
			case "Byte":
				return ReadByte();
			case "Int64":
				if (Is32Bit)
				{
					return (long)ReadInt32();
				}
				return ReadInt64();
			case "UInt64":
				if (Is32Bit)
				{
					return (ulong)ReadUInt32();
				}
				return ReadUInt64();
			default:
				throw new NotSupportedException();
			}
		}

		public T ReadClass<T>(ulong addr) where T : new()
		{
			Position = addr;
			return ReadClass<T>();
		}

		public T ReadClass<T>() where T : new()
		{
			Type type = typeof(T);
			if (type.IsPrimitive)
			{
				return (T)ReadPrimitive(type);
			}
			T t = new T();
			FieldInfo[] fields = t.GetType().GetFields();
			foreach (FieldInfo i2 in fields)
			{
				VersionAttribute versionAttribute;
				if (!attributeCache.TryGetValue(i2, out versionAttribute) && Attribute.IsDefined(i2, typeof(VersionAttribute)))
				{
					versionAttribute = i2.GetCustomAttribute<VersionAttribute>();
					attributeCache.Add(i2, versionAttribute);
				}
				if (versionAttribute != null && (Version < versionAttribute.Min || Version > versionAttribute.Max))
				{
					continue;
				}
				Type fieldType = i2.FieldType;
				if (fieldType.IsPrimitive)
				{
					i2.SetValue(t, ReadPrimitive(fieldType));
				}
				else if (fieldType.IsEnum)
				{
					Type e = fieldType.GetField("value__").FieldType;
					i2.SetValue(t, ReadPrimitive(e));
				}
				else if (fieldType.IsArray)
				{
					ArrayLengthAttribute arrayLengthAttribute = i2.GetCustomAttribute<ArrayLengthAttribute>();
					MethodInfo methodInfo;
					if (!genericMethodCache.TryGetValue(fieldType, out methodInfo))
					{
						methodInfo = readClassArray.MakeGenericMethod(fieldType.GetElementType());
						genericMethodCache.Add(fieldType, methodInfo);
					}
					i2.SetValue(t, methodInfo.Invoke(this, new object[1] { arrayLengthAttribute.Length }));
				}
				else
				{
					MethodInfo methodInfo2;
					if (!genericMethodCache.TryGetValue(fieldType, out methodInfo2))
					{
						methodInfo2 = readClass.MakeGenericMethod(fieldType);
						genericMethodCache.Add(fieldType, methodInfo2);
					}
					i2.SetValue(t, methodInfo2.Invoke(this, null));
				}
			}
			return t;
		}

		public T[] ReadClassArray<T>(long count) where T : new()
		{
			T[] t = new T[count];
			for (int i = 0; i < count; i++)
			{
				t[i] = ReadClass<T>();
			}
			return t;
		}

		public T[] ReadClassArray<T>(ulong addr, long count) where T : new()
		{
			Position = addr;
			return ReadClassArray<T>(count);
		}

		public string ReadStringToNull(ulong addr)
		{
			Position = addr;
			List<byte> bytes = new List<byte>();
			byte b;
			while ((b = ReadByte()) != 0)
			{
				bytes.Add(b);
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}

		public long ReadIntPtr()
		{
			return Is32Bit ? ReadInt32() : ReadInt64();
		}

		public ulong ReadUIntPtr()
		{
			return Is32Bit ? ReadUInt32() : ReadUInt64();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				reader.Close();
				writer.Close();
				stream.Close();
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
