# Il2CppDumper
Modified version of Il2CppDumper allows you to dump methods of UserAssembly.dll of the game Genshin Impact
I forked it from [https://github.com/khang06/Il2CppDumper-YuanShen](https://github.com/khang06/Il2CppDumper-YuanShen) and fixed the issue where BeeByte Obfuscator mappings failed to load.

## Usage

Run `Il2CppDumper.exe` and choose the il2cpp executable file and `global-metadata.dat` file, then enter the information as prompted

The program will then generate all the output files in current working directory

### Outputs

#### Generated/DummyDll

Folder, containing all restored dll files

Use [dnSpy](https://github.com/0xd4d/dnSpy), [ILSpy](https://github.com/icsharpcode/ILSpy) or other .Net decompiler tools to view

Can be used to extract Unity `MonoBehaviour` and `MonoScript`, for [UtinyRipper](https://github.com/mafaca/UtinyRipper), [UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor)

#### Scripts/ida.py

For IDA

#### Scripts/ida_with_struct.py

For IDA, read il2cpp.h file and apply structure information in IDA

#### Scripts/ghidra.py

For Ghidra

#### Generated/il2cpp.h

structure information header file

#### Generated/script.json

For ida.py and ghidra.py

#### Generated/stringliteral.json

Contains all stringLiteral information