using System.Collections;
using System.Net.Mail;

namespace MCUSimulator.Core
{
    public class MCURegisterCollection : IEnumerable<IMemory>
    {
        //Dictionaries will be faster to address than IEnumerable
        Dictionary<string, IReadWriteMemory> memories;
        Dictionary<string, IRegister> registers;
        Dictionary<int, IRandomAccessMemory> sram;
        Dictionary<string, IMemoryBitField> bitFields;
        //List<IRegister> dirty = new List<IRegister>();
        public MCURegisterCollection(IEnumerable<IReadWriteMemory> memories)
        {
            this.memories = memories.ToDictionary(e => e.Id, e => e);
            registers = memories.Where(m => m is IRegister).Cast<IRegister>().ToDictionary(e => e.Name!, e => e);
            sram = memories.Where(m => m is IRandomAccessMemory).Cast<IRandomAccessMemory>().ToDictionary(e => e.Address, e => e);
            bitFields = memories.SelectMany(m => m.BitFields ?? Enumerable.Empty<IMemoryBitField>()).ToDictionary(e => e.Id, e => e);
            //foreach(var mem in memories)
            //{
            //    mem.OnChanged += (s, e) =>
            //    {
            //        dirty.Add((IRegister)e.Memory);
            //    };
            //}
        }

        public IEnumerable<IReadWriteMemory> Memories => memories.Values;
        public IEnumerable<IRegister> Registers => registers.Values;
        public IEnumerable<IRandomAccessMemory> SRAM => sram.Values;
        public IEnumerable<IMemoryBitField> BitFields => bitFields.Values;

        public IEnumerable<IReadableMemory> Readables => memories.Values.Where(m => m is IReadableMemory).Cast<IReadableMemory>().Concat(bitFields.Values.Where(i => i is IReadableMemoryBitField).Cast<IReadableMemoryBitField>());
        public IEnumerable<IWritableMemory> Writables => memories.Values.Where(m => m is IWritableMemory).Cast<IWritableMemory>().Concat(bitFields.Values.Where(i => i is IWritableMemoryBitField).Cast<IWritableMemoryBitField>());
        public IEnumerable<IReadableMemoryBitField> ReadableBitFields => bitFields.Values.Where(i => i is IReadableMemoryBitField).Cast<IReadableMemoryBitField>();
        public IEnumerable<IWritableMemoryBitField> WritableBitFields => bitFields.Values.Where(i => i is IWritableMemoryBitField).Cast<IWritableMemoryBitField>();

        public IMemory? GetById(string? id)
        {
            return (IMemory?)memories.GetValueOrDefault(id ?? "") ??
                (IMemory?)bitFields.GetValueOrDefault(id ?? "");
        }

        public IMemoryBitField? GetBitField(string name)
        {
            return bitFields[name];
        }

        public IRegister this[string name]
        {
            get
            {
                return registers[name];
            }
        }

        public IRandomAccessMemory this[int address]
        {
            get
            {
                return sram[address];
            }
        }

        public IEnumerator<IMemory> GetEnumerator()
        {
            return memories.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return memories.GetEnumerator();
        }

        internal void Reset()
        {
            foreach (var mem in memories.Values)
            {
                switch (mem)
                {
                    case MCURegisterWithAddress reg:
                        if (reg.ResetValue != null)
                            reg.Value = reg.ResetValue.Value;
                        break;
                    case MCUStaticRAM reg:
                        if (reg.ResetValue != null)
                            reg.Value = reg.ResetValue.Value;
                        break;
                    case MCURegister reg:
                        if (reg.ResetValue != null)
                            reg.Value = reg.ResetValue.Value;
                        break;
                }
            }
        }

        public void Dump()
        {
            foreach (var mem in memories)
            {
                Console.Write($"{mem}, ");
            }
        }
    }
}
