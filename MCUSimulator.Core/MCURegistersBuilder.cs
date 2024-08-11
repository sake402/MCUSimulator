namespace MCUSimulator.Core
{
    public class MCURegistersBuilder
    {
        int defaultRegisterBits;

        public MCURegistersBuilder(int defaultRegisterBits)
        {
            this.defaultRegisterBits = defaultRegisterBits;
        }

        List<MCURegisterBuilder> registeredBuilder = new List<MCURegisterBuilder>();
        void CheckForDuplicate(int address)
        {
            if (registeredBuilder.Any(a => a.Address == address))
                throw new InvalidOperationException($"Register address conflict at {address}");
        }

        void CheckForDuplicate(string name)
        {
            if (registeredBuilder.Any(a => a.Name == name))
                throw new InvalidOperationException($"Register name conflict at {name}");
        }

        public MCURegistersBuilder Define(
            string name,
            MemoryAccess access = MemoryAccess.ReadWrite, 
            Action<MCURegisterBuilder>? registerBuilder = null)
        {
            CheckForDuplicate(name);
            var builder = new MCURegisterBuilder(name.ToUpper(), access);
            registerBuilder?.Invoke(builder);
            registeredBuilder.Add(builder);
            return this;
        }

        public MCURegistersBuilder Define(int address, MemoryAccess access = MemoryAccess.ReadWrite, Action<MCURegisterBuilder>? registerBuilder = null)
        {
            CheckForDuplicate(address);
            var builder = new MCURegisterBuilder(address, access);
            registerBuilder?.Invoke(builder);
            registeredBuilder.Add(builder);
            return this;
        }

        public MCURegistersBuilder Define(int addressStart, int addressLen, MemoryAccess access = MemoryAccess.ReadWrite, Action<MCURegisterBuilder>? registerBuilder = null)
        {
            for (int i = 0; i < addressLen; i++)
            {
                CheckForDuplicate(addressStart + i);
                var builder = new MCURegisterBuilder(addressStart + i, access);
                registerBuilder?.Invoke(builder);
                registeredBuilder.Add(builder);
            }
            return this;
        }

        public MCURegisterCollection Build()
        {
            var registers = registeredBuilder.Select(r => r.Build()).ToList();
            return new MCURegisterCollection(registers);
        }
    }
}
