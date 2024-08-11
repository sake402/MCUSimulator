using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.Core
{
    public struct MemoryChangingEventArg
    {
        public IMemory Memory { get; set; }
        public int To { get; set; }
    }

    public struct MemoryChangedEventArg
    {
        public IMemory Memory { get; set; }
        public int From { get; set; }
    }

    public interface IMemory
    {
        string Id { get; }
        int Width { get; }
        IEnumerable<IMemoryBitField>? BitFields { get; }
        event EventHandler<MemoryChangingEventArg>? OnChanging;
        event EventHandler<MemoryChangedEventArg>? OnChanged;
    }

    public static class MemoryExtension
    {
        public static T? GetField<T>(this IMemory memory, string? name = null)
        {
            var bitField = (IReadableMemory?)memory.BitFields?.FirstOrDefault(m => m.Type == typeof(T) && (name == null || m.Name == name));
            if (bitField == null)
                return default;
            return (T)(object)bitField.Value;
        }
    }
}
