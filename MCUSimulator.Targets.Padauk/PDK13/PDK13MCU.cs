using LivingThing.Core.Frameworks.Common.OneOf;
using MCUSimulator.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.Targets.Padauk.PDK13
{
    public class PDK13MCU : MCU<PDK13MCUParameter>
    {
        bool globalInteruptEnabled = false;
        IReadWriteMemory a = default!;
        IReadWriteMemory acc = default!;
        IReadWriteMemory sp = default!;
        IReadWriteMemory t16 = default!;
        IReadWriteMemory t16m = default!;
        IReadWriteMemory clkmd = default!;
        IReadWriteMemory inten = default!;
        IReadWriteMemory intrq = default!;
        IReadWriteMemory eoscr = default!;
        IReadWriteMemory integs = default!;
        IReadWriteMemory padier = default!;
        IReadWriteMemory pa = default!;
        IReadWriteMemory pac = default!;
        IReadWriteMemory paph = default!;
        IReadWriteMemory papl = default!;
        IReadWriteMemory misc = default!;
        IReadWriteMemory gpcc = default!;
        IReadWriteMemory gpcs = default!;
        IReadWriteMemory tm2c = default!;
        IReadWriteMemory tm2ct = default!;
        IReadWriteMemory tm2b = default!;
        IReadWriteMemory tm2s = default!;
        IReadWriteMemory ihrc = default!;

        IReadableMemoryBitField<bool> pa0 = default!;
        IReadableMemoryBitField<bool> pa3 = default!;
        IReadableMemoryBitField<bool> pa4 = default!;
        IReadableMemoryBitField<bool> pa5 = default!;
        IReadableMemoryBitField<bool> pa6 = default!;
        IReadableMemoryBitField<bool> pa7 = default!;
        IReadWriteMemoryBitField<bool> ov = default!;
        IReadWriteMemoryBitField<bool> ac = default!;
        IReadWriteMemoryBitField<bool> c = default!;
        IReadWriteMemoryBitField<bool> z = default!;
        IReadWriteMemoryBitField<SystemClockSelection> systemClockSelection = default!;
        IReadWriteMemoryBitField<bool> ihrcen = default!;
        IReadWriteMemoryBitField<int> clkType = default!;
        IReadWriteMemoryBitField<bool> ilrcen = default!;
        IReadWriteMemoryBitField<bool> wdten = default!;
        IReadWriteMemoryBitField<Timer16ClockSource> t16ClkSrc = default!;
        IReadWriteMemoryBitField<Timer16ClockDivider> t16ClkDiv = default!;
        IReadWriteMemoryBitField<Timer16InterruptBit> t16IntBit = default!;

        IReadWriteMemoryBitField<ComparatorInterruptEdge> integsComparator = default!;
        IReadWriteMemoryBitField<Timer16InterruptEdge> integsTimer16 = default!;
        IReadWriteMemoryBitField<PortA0InterruptEdge> integsPortA0 = default!;

        void UpdateFlags(OpCodeExecutionContext context, int fromResult, PadaukFlagUpdate flags = PadaukFlagUpdate.All)
        {
            if (flags.HasFlag(PadaukFlagUpdate.Z))
            {
                z.Value = fromResult == 0 ? 1 : 0;
            }
            if (flags.HasFlag(PadaukFlagUpdate.C))
            {
                c.Value = fromResult < 0 || fromResult >= context.Simulator.RegisterMax ? 1 : 0;
            }
            if (flags.HasFlag(PadaukFlagUpdate.AC))
            {
                ac.Value = fromResult >= context.Simulator.RegisterMax ? 1 : 0;
            }
            if (flags.HasFlag(PadaukFlagUpdate.OV))
            {
                //ov.Value
            }
        }

        void PushToStack(MCUSimulatorEngine simulator, int value, int count)
        {
            while (count-- > 0)
            {
                var stackPointed = simulator.Registers[sp.Value];
                stackPointed.Value = value & 0xFF;
                sp.Value++;
                value >>= 8;
            }
        }

        void PopFromStack(MCUSimulatorEngine simulator, out int value, int count)
        {
            int val = 0;
            while (count-- > 0)
            {
                var stackPointed = simulator.Registers[sp.Value - 1];
                val <<= 8;
                val |= stackPointed.Value & 0xFF;
                sp.Value--;
            }
            value = val;
        }

        void RaiseInterrupt(MCUSimulatorEngine simulator)
        {
            //vector to interrupt
            globalInteruptEnabled = false;
            Debug.Assert(simulator.InstructionPointer % 2 == 0);
            PushToStack(simulator, simulator.InstructionPointer / 2, 2);
            simulator.InstructionPointer = 0x20;
        }

        int t16Div = 0;
        void Timer16Count(MCUSimulatorEngine simulator)
        {
            if (t16Div == 0)
            {
                var old = t16.Value;
                t16.Value++;
                var bitInterrupt = 1 << (t16IntBit.Value + 8);
                if ((inten.Value & (1 << 2)) != 0) //timer16 interrupt enable
                {
                    if ((old & bitInterrupt) != (t16.Value & bitInterrupt)) //timer overflowing
                    {
                        bool isPositiveEdge = (t16.Value & bitInterrupt) != 0;
                        var isRisingEdgeSettings = (integs.Value & (1 << 4)) == 0;
                        if ((isRisingEdgeSettings && isPositiveEdge) || (!isRisingEdgeSettings && !isPositiveEdge))
                        {
                            intrq.Value |= 1 << 2;
                            RaiseInterrupt(simulator);
                        }
                    }
                }
                t16Div = t16ClkDiv.TValue switch
                {
                    Timer16ClockDivider.Four => 4,
                    Timer16ClockDivider.Sixteen => 16,
                    Timer16ClockDivider.SixtyFour => 64,
                    _ => 1,
                };
            }
            t16Div--;
        }

        public PDK13MCU() : base("Padauk PDK13",
            @"These devices feature a 13-bit wide code memory. Byte order is little endian.

PMC150, PMC153, PMC156, PMC166, PMS150, PMS150B, PMS150C, PMS150G, PMS153, PMS156, PMS15A, PMS15B",
            13, 8)
        {
        }

        int GetSystemClock(MCUSimulatorEngine simulator)
        {
            var _ihrcen = ihrcen.TValue;
            var _ilrcen = ilrcen.TValue;
            var _clktype = clkType.TValue;
            var sysClockSel = systemClockSelection.TValue;
            int clk = 0;
            if (_clktype == 0)
            {
                clk = sysClockSel switch
                {
                    SystemClockSelection.T_0_IHRC_2 => _ihrcen ? Parameter.IHRCFrequency / 2 : 0,
                    SystemClockSelection.T_0_IHRC_4 => _ihrcen ? Parameter.IHRCFrequency / 4 : 0,
                    SystemClockSelection.T_0_ILRC => _ilrcen ? Parameter.ILRCFrequency / 1 : 0,
                    SystemClockSelection.T_0_ILRC_4 => _ilrcen ? Parameter.ILRCFrequency / 4 : 0,
                    _ => 0
                };
            }
            else
            {
                clk = sysClockSel switch
                {
                    SystemClockSelection.T_1_IHRC_8 => _ihrcen ? Parameter.IHRCFrequency / 8 : 0,
                    SystemClockSelection.T_1_IHRC_16 => _ihrcen ? Parameter.IHRCFrequency / 16 : 0,
                    SystemClockSelection.T_1_IHRC_32 => _ihrcen ? Parameter.IHRCFrequency / 32 : 0,
                    SystemClockSelection.T_1_IHRC_64 => _ihrcen ? Parameter.IHRCFrequency / 64 : 0,
                    SystemClockSelection.T_1_ILRC_16 => _ilrcen ? Parameter.ILRCFrequency / 16 : 0,
                    _ => 0
                };
            }
            if (clk == 0)
                simulator.LogDiagnostics(MCUDiagnosticLogLevel.Warning, "Invalid system clock");
            return clk;
        }

        int GetTimer16Clock(MCUSimulatorEngine simulator)
        {
            var _ihrcen = ihrcen.TValue;
            var _ilrcen = ilrcen.TValue;
            var _clktype = clkType.TValue;
            var sysClockSel = systemClockSelection.TValue;

            var t16ClockSource = t16ClkSrc.TValue;
            if (t16ClockSource == Timer16ClockSource.IHRC)
                return Parameter.IHRCFrequency;
            if (t16ClockSource == Timer16ClockSource.ILRC)
                return Parameter.ILRCFrequency;
            else if (t16ClockSource == Timer16ClockSource.SystemClock)
                return GetSystemClock(simulator);
            return 0;
        }


        public override MCUSimulatorEngine Build()
        {
            ClockBuilder.Define(OneOf<int, ClockProvider>.FromT1(GetSystemClock), "SystemClock", (simulation) =>
            {
                simulation.Step();
            });
            ClockBuilder.Define(OneOf<int, ClockProvider>.FromT1(GetTimer16Clock), "Timer16Clock", (simulation) =>
            {
                Timer16Count(simulation);
            });
            //ClockBuilder.Define(IHRCFrequency, "IHRC", (simulation) =>
            //{
            //    var _ihrcen = ihrcen.TValue;
            //    if (!_ihrcen)
            //        return;
            //    if (ihrcDiv == 0)
            //    {
            //        var _clktype = clkType.TValue;
            //        var sysClockSel = systemClockSelection.TValue;
            //        bool systemClockIsIHRC = sysClockSel == SystemClockSelection.T_0_IHRC_2 ||
            //            sysClockSel == SystemClockSelection.T_0_IHRC_4 ||
            //            sysClockSel == SystemClockSelection.T_1_IHRC_8 ||
            //            sysClockSel == SystemClockSelection.T_1_IHRC_16 ||
            //            sysClockSel == SystemClockSelection.T_1_IHRC_32 ||
            //            sysClockSel == SystemClockSelection.T_1_IHRC_64;
            //        if (systemClockIsIHRC)
            //        {
            //            simulation.Step();
            //        }

            //        var t16ClockSource = t16ClkSrc.TValue;
            //        if (t16ClockSource == Timer16ClockSource.IHRC)
            //            Timer16Count(simulation);
            //        else if (t16ClockSource == Timer16ClockSource.SystemClock && systemClockIsIHRC)
            //            Timer16Count(simulation);

            //        if (_clktype == 0)
            //        {
            //            ihrcDiv = sysClockSel switch
            //            {
            //                SystemClockSelection.T_0_IHRC_2 => 2,
            //                SystemClockSelection.T_0_IHRC_4 => 4,
            //                _ => 1
            //            };
            //        }
            //        else
            //        {
            //            ihrcDiv = sysClockSel switch
            //            {
            //                SystemClockSelection.T_1_IHRC_8 => 8,
            //                SystemClockSelection.T_1_IHRC_16 => 16,
            //                SystemClockSelection.T_1_IHRC_32 => 32,
            //                SystemClockSelection.T_1_IHRC_64 => 64,
            //                _ => 1
            //            };
            //        }
            //    }
            //    ihrcDiv--;
            //});

            //ClockBuilder.Define(62500, "ILRC", (simulation) =>
            //{
            //    var _ilrcen = ilrcen.TValue;
            //    if (!_ilrcen)
            //        return;
            //    if (ilrcDiv == 0)
            //    {
            //        var clockSel = systemClockSelection.TValue;
            //        bool systemClockIsILRC = clockSel == SystemClockSelection.T_0_ILRC ||
            //            clockSel == SystemClockSelection.T_0_ILRC_4 ||
            //            clockSel == SystemClockSelection.T_1_ILRC_16;
            //        if (systemClockIsILRC)
            //        {
            //            simulation.Step();
            //        }
            //        var t16ClockSource = t16ClkSrc.TValue;
            //        var _clktype = clkType.TValue;
            //        if (t16ClockSource == Timer16ClockSource.ILRC)
            //            Timer16Count(simulation);
            //        else if (t16ClockSource == Timer16ClockSource.SystemClock && systemClockIsILRC)
            //            Timer16Count(simulation);
            //        if (_clktype == 0)
            //        {
            //            ilrcDiv = clockSel switch
            //            {
            //                SystemClockSelection.T_0_ILRC => 1,
            //                SystemClockSelection.T_0_ILRC_4 => 4,
            //                _ => 1
            //            };
            //        }
            //        else
            //        {
            //            ilrcDiv = clockSel switch
            //            {
            //                SystemClockSelection.T_1_ILRC_16 => 16,
            //                _ => 1
            //            };
            //        }
            //    }
            //    ilrcDiv--;
            //});

            const int IO_BASE_ADDRESS_FLAG = 0x80000;

            RegisterBuilder
                .Define("a", registerBuilder: (rb) =>
                {
                    rb.WithSink((val) => a = val);
                })
                .Define("t16", registerBuilder: (rb) =>
                {
                    rb.WithSink((val) => t16 = val)
                       .WithWidth(16);
                })
                .Define("acc", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x00)
                    .WithSink((val) => acc = val)
                    .BooleanBit(3, "ov", sink: (v) => ov = (IReadWriteMemoryBitField<bool>)v)
                    .BooleanBit(2, "ac", sink: (v) => ac = (IReadWriteMemoryBitField<bool>)v)
                    .BooleanBit(1, "c", sink: (v) => c = (IReadWriteMemoryBitField<bool>)v)
                    .BooleanBit(0, "z", sink: (v) => z = (IReadWriteMemoryBitField<bool>)v);
                })
                .Define("sp", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x02)
                    .WithSink((val) =>
                    {
                        sp = val;
                        sp.OnChanged += (s, e) =>
                        {

                        };
                    });
                })
                .Define("clkmd", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x03)
                    .WithSink((val) => clkmd = val)
                    .WithReset(0b11110110)
                    .EnumBit<SystemClockSelection>(7..5, description: "System clock selection", sink: (v) => systemClockSelection = (IReadWriteMemoryBitField<SystemClockSelection>)v)
                    .BooleanBit(4, name: "ihrcen", description: "IHRC enable", sink: (v) => ihrcen = (IReadWriteMemoryBitField<bool>)v)
                    .Bit<int>(3, name: "clktype", description: "Clock Type", sink: (v) => clkType = (IReadWriteMemoryBitField<int>)v)
                    .BooleanBit(2, name: "ilrcen", description: "ILRC enable", sink: (v) => ilrcen = (IReadWriteMemoryBitField<bool>)v)
                    .BooleanBit(1, name: "wdten", description: "Watchdog enable", sink: (v) => wdten = (IReadWriteMemoryBitField<bool>)v);
                })
                .Define("inten", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x04)
                    .WithSink((val) => inten = val);
                })
                .Define("intrq", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x05)
                    .WithSink((val) => intrq = val);
                })
                .Define("t16m", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x06)
                    .WithSink((val) => t16m = val)
                    .EnumBit<Timer16ClockSource>(7..5, description: "Timer clock source selection", sink: (v) => t16ClkSrc = (IReadWriteMemoryBitField<Timer16ClockSource>)v)
                    .EnumBit<Timer16ClockDivider>(4..3, description: "Internal Clock divider", sink: (v) => t16ClkDiv = (IReadWriteMemoryBitField<Timer16ClockDivider>)v)
                    .EnumBit<Timer16InterruptBit>(2..0, description: "Interrupt source selection", sink: (v) => t16IntBit = (IReadWriteMemoryBitField<Timer16InterruptBit>)v);
                })
                .Define("eoscr", access: MemoryAccess.Write, registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x0A)
                    .WithSink((val) => eoscr = val);
                })
                .Define("integs", access: MemoryAccess.Write, registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x0C)
                    .WithSink((val) => integs = val)
                    .EnumBit<ComparatorInterruptEdge>(7..6, description: "Comparator edge selection", sink: (v) => integsComparator = (IReadWriteMemoryBitField<ComparatorInterruptEdge>)v)
                    .EnumBit<Timer16InterruptEdge>(4..3, description: "Timr16 edge selection", sink: (v) => integsTimer16 = (IReadWriteMemoryBitField<Timer16InterruptEdge>)v)
                    .EnumBit<PortA0InterruptEdge>(2..0, description: "PA0 edge selection", sink: (v) => integsPortA0 = (IReadWriteMemoryBitField<PortA0InterruptEdge>)v);

                })
                .Define("padier", access: MemoryAccess.Write, registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x0D)
                    .WithSink((val) => padier = val);
                })
                .Define("pa", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x10)
                       .WithSink((val) => pa = val)
                       .BooleanBit(0, sink: (v) => pa0 = (IReadWriteMemoryBitField<bool>)v)
                       .BooleanBit(3, sink: (v) => pa3 = (IReadWriteMemoryBitField<bool>)v)
                       .BooleanBit(4, sink: (v) => pa4 = (IReadWriteMemoryBitField<bool>)v)
                       .BooleanBit(5, sink: (v) => pa5 = (IReadWriteMemoryBitField<bool>)v)
                       .BooleanBit(6, sink: (v) => pa6 = (IReadWriteMemoryBitField<bool>)v)
                       .BooleanBit(7, sink: (v) => pa7 = (IReadWriteMemoryBitField<bool>)v);
                })
                .Define("pac", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x11)
                       .WithSink((val) => pac = val);
                })
                .Define("paph", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x12)
                       .WithSink((val) => paph = val);
                })
                .Define("papl", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x13)
                       .WithSink((val) => papl = val);
                })
                .Define("misc", access: MemoryAccess.Write, registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x1b)
                       .WithSink((val) => misc = val);
                })
                .Define("gpcc", access: MemoryAccess.Write, registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x1A)
                       .WithSink((val) => gpcc = val);
                })
                .Define("gpcs", access: MemoryAccess.Write, registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x1E)
                       .WithSink((val) => gpcs = val);
                })
                .Define("tm2c", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x1C)
                       .WithSink((val) => tm2c = val);
                })
                .Define("tm2ct", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x1D)
                       .WithSink((val) => tm2ct = val);
                })
                .Define("tm2b", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x09)
                       .WithSink((val) => tm2b = val);
                })
                .Define("tm2s", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0x17)
                       .WithSink((val) => tm2s = val);
                })
                .Define("ihcr", registerBuilder: (rb) =>
                {
                    rb.WithAddress(IO_BASE_ADDRESS_FLAG + 0xB)
                       .WithSink((val) => ihrc = val);
                })
                //Holes of unknown addresses
                //.Define(0x01)
                //.Define(0x07)
                //.Define(0x08)
                //.Define(0x0e)
                //.Define(0x0f)
                //.Define(0x14)
                //.Define(0x15)
                //.Define(0x16)
                //.Define(0x18)
                //define the SRAM
                .Define(0x00, 64)
                ;

            InstructionBuilder
                .Define("NOP", "0*13", (ref OpCodeExecutionContext context) => { })
                //A ← LowByte@CodeMem(WORD[SP])
                .Define("LDSPTL", "0*10,110", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[sp.Value];
                    a.Value = (int)(mem.Value & context.Simulator.RegisterMask);
                })
                //A ← HighByteB@Codemem(WORD[SP])
                .Define("LDSPTH", "0*10,111", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[sp.Value + 1];
                    a.Value = (int)(mem.Value & context.Simulator.RegisterMask);
                })
                //A ← A + CF
                .Define("ADDC A", "0*8,10000", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value + c.Value;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A - CF
                .Define("SUBC A", "0*8,10001", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value - c.Value;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //Increment A and skip next instruction if A is zero
                .Define("IZSN A", "0*8,10010", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value + 1;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                    if (result == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Decrement A and skip next instruction if A is zero
                .Define("DZSN A", "0*8,10011", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value - 1;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                    if (result == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Add A to PC
                .Define("PCADD A", "0*8,10111", (ref OpCodeExecutionContext context) =>
                {
                    context.Simulator.InstructionPointer += a.Value;
                }, instructionCycle: 2)
                //A ← ~A
                .Define("NOT A", "0*8,11000", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value ^ 0x7FFFFFFF;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = result;
                })
                //A ← NEG(A)
                .Define("NEG A", "0*8,11001", (ref OpCodeExecutionContext context) =>
                {
                    var result = -a.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = result;
                })
                //A ← A >> 1
                .Define("SR A", "0*8,11010", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value >> 1;
                    c.Value = (a.Value & 1) != 0 ? 1 : 0;
                    a.Value = result;
                })
                //A ← A << 1
                .Define("SR A", "0*8,11011", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value << 1;
                    c.Value = (a.Value & (1 << (context.Simulator.RegisterBits - 1))) != 0 ? 1 : 0;
                    a.Value = result;
                })
                //A ← CF:A >> 1
                .Define("SRC A", "0*8,11100", (ref OpCodeExecutionContext context) =>
                {
                    var result = (a.Value >> 1) | (c.Value != 0 ? (1 << (context.Simulator.RegisterBits - 1)) : 0);
                    c.Value = (a.Value & 1) != 0 ? 1 : 0;
                    a.Value = result;
                })
                //A ← A:CF << 1
                .Define("SLC A", "0*8,11101", (ref OpCodeExecutionContext context) =>
                {
                    var result = (a.Value << 1) | (c.Value != 0 ? 1 : 0);
                    c.Value = (a.Value & (1 << (context.Simulator.RegisterBits - 1))) != 0 ? 1 : 0;
                    a.Value = result;
                })
                //Swap the high nibble and low nibble of A
                .Define("SWAP A", "0*8,11110", (ref OpCodeExecutionContext context) =>
                {
                    var result = ((a.Value << 4) & 0xF0) | ((a.Value >> 4) & 0x0F);
                    a.Value = result;
                })
                //Reset Watchdog timer
                .Define("WDTRESET", "0*7,110000", (ref OpCodeExecutionContext context) =>
                {
                    context.Simulator.WatchdogTimer.Reset();
                })
                //Push A and flags to stack: [SP] ← A, [SP + 1] ← F, SP ← SP + 2
                .Define("PUSHAF", "0*7,110010", (ref OpCodeExecutionContext context) =>
                {
                    PushToStack(context.Simulator, a.Value, 1);
                    PushToStack(context.Simulator, acc.Value, 1);
                })
                //Pop A and flags from stack: SP ← SP + 2, F ← [SP + 1], [SP] ← A
                .Define("POPAF", "0*7,110011", (ref OpCodeExecutionContext context) =>
                {
                    PopFromStack(context.Simulator, out int _acc, 1);
                    PopFromStack(context.Simulator, out int _a, 1);
                    a.Value = _a;
                    acc.Value = _acc;
                })
                //Reset the whole chip
                .Define("RESET", "0*7,110101", (ref OpCodeExecutionContext context) =>
                {
                    context.Simulator.InstructionPointer = 0;
                })
                //System halt (OSC disabled)
                .Define("STOPSYS", "0*7,110110", (ref OpCodeExecutionContext context) =>
                {
                    throw new NotImplementedException("STOPSYS is not implemented");
                })
                //System halt (OSC disabled)
                .Define("STOPEXE", "0*7,110111", (ref OpCodeExecutionContext context) =>
                {
                    throw new NotImplementedException("STOPEXE is not implemented");
                })
                //Global interrupt enbale
                .Define("ENGINT", "0*7,111000", (ref OpCodeExecutionContext context) =>
                {
                    globalInteruptEnabled = true;
                })
                //Global interrupt disable
                .Define("DISGINT", "0*7,111001", (ref OpCodeExecutionContext context) =>
                {
                    globalInteruptEnabled = false;
                })
                //Return from subroutine
                .Define("RET", "0*7,111010", (ref OpCodeExecutionContext context) =>
                {
                    PopFromStack(context.Simulator, out int returnTo, 2);
                    context.Simulator.InstructionPointer = returnTo * 2;
                }, instructionCycle: 2)
                //Return from interrupt
                .Define("RETI", "0*7,111011", (ref OpCodeExecutionContext context) =>
                {
                    PopFromStack(context.Simulator, out int returnTo, 2);
                    context.Simulator.InstructionPointer = returnTo * 2;
                    globalInteruptEnabled = true;
                }, instructionCycle: 2)
                //Multiply
                .Define("MUL", "0*7,111100", (ref OpCodeExecutionContext context) =>
                {
                })
                //IO ← IO ^ A
                .Define("XOR.IO {arg}, A", "0*5,011,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var io = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[0]];
                    io.Value ^= a.Value;
                })
                //IO ← A
                .Define("MOV.IO {arg}, A", "0*5,100,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var io = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[0]];
                    io.Value = a.Value;
                })
                //A ← IO
                .Define("MOV.IO A, {arg}", "0*5,101,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var io = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[0]];
                    a.Value = io.Value;
                    UpdateFlags(context, a.Value, PadaukFlagUpdate.Z);
                })
                //Timer16 ← M (last bit of M set to 0, M must be word aligned)
                .Define("STT16 M, {arg}", "0*5,110,X*4,0", (ref OpCodeExecutionContext context) =>
                {
                    var address = (int)context.OpCodeArgument[0] << 1;
                    var lowio = context.Simulator.Registers[address];
                    var highio = context.Simulator.Registers[address + 1];
                    var value = (lowio.Value & context.Simulator.RegisterMask) | ((highio.Value & context.Simulator.RegisterMask) << 8);
                    t16.Value = (int)value;
                }, instructionCycle: 2)
                //M ← Timer16 (last bit of M set to 1, M must be word aligned)
                .Define("LDT16 M, {arg}", "0*5,110,X*4,1", (ref OpCodeExecutionContext context) =>
                {
                    var address = (int)context.OpCodeArgument[0] << 1;
                    var lowio = context.Simulator.Registers[address];
                    var highio = context.Simulator.Registers[address + 1];
                    var value = t16.Value;
                    lowio.Value = (int)(t16.Value & context.Simulator.RegisterMask);
                    highio.Value = (int)((t16.Value >> 8) & context.Simulator.RegisterMask);
                }, instructionCycle: 2)
                //[M] ← A (last bit of M set to 0, M must be word aligned, 2 cycles)
                .Define("IDXM {arg}, A", "0*5,111,X*4,0", (ref OpCodeExecutionContext context) =>
                {
                    var address = (int)context.OpCodeArgument[0] << 1;
                    var lowio = context.Simulator.Registers[address];
                    //var highio = context.Simulator.Registers[address + 1];
                    var indirectAddress = lowio.Value;// | highio.Value << 8;
                    var addressedRegister = context.Simulator.Registers[indirectAddress];
                    addressedRegister.Value = a.Value;
                }, instructionCycle: 2)
                //A ← [M] (last bit of M set to 1, M must be word aligned, 2 cycles)
                .Define("IDXM A, {arg}", "0*5,111,X*4,1", (ref OpCodeExecutionContext context) =>
                {
                    var address = (int)context.OpCodeArgument[0] << 1;
                    var lowio = context.Simulator.Registers[address];
                    //var highio = context.Simulator.Registers[address + 1];
                    var indirectAddress = lowio.Value;// | highio.Value << 8;
                    var addressedRegister = context.Simulator.Registers[indirectAddress];
                    a.Value = addressedRegister.Value;
                }, instructionCycle: 2)
                //A ← k and return from subroutine
                .Define("RET {arg}", "0*4,1,X*8", (ref OpCodeExecutionContext context) =>
                {
                    a.Value = (int)context.OpCodeArgument[0];
                    PopFromStack(context.Simulator, out int returnTo, 2);
                    context.Simulator.InstructionPointer = returnTo * 2;
                }, instructionCycle: 2)
                //Test bit n of memory M and skip next instruction if clear
                .Define("T0SN {1}.{0}", "00010,X*3,0,X*4", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var bit = (int)context.OpCodeArgument[1];
                    if ((mem.Value & (1 << bit)) == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Test bit n of memory M and skip next instruction if set
                .Define("T1SN {1}.{0}", "00010,X*3,1,X*4", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var bit = (int)context.OpCodeArgument[1];
                    if ((mem.Value & (1 << bit)) != 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Clear bit n of memory M
                .Define("SET0 {arg}.{arg}", "00011,X*3,0,X*4", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var bit = (int)context.OpCodeArgument[1];
                    mem.Value &= ~(1 << bit);
                })
                //Set bit n of memory M
                .Define("SET1 {arg}.{arg}", "00011,X*3,1,X*4", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var bit = (int)context.OpCodeArgument[1];
                    mem.Value |= (1 << bit);
                })
                //M ← M + A
                .Define("ADD {arg}, A", "0010000,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value + a.Value;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M - A
                .Define("SUB {arg}, A", "0010001,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value - a.Value;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M + A + C
                .Define("ADDC {arg}, A", "0010010,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value + a.Value + c.Value;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M - A - C
                .Define("SUBC {arg}, A", "0010011,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value - a.Value - c.Value;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M & A
                .Define("AND {arg}, A", "0010100,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value & a.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M | A
                .Define("OR {arg}, A", "0010101,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value | a.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M ^ A
                .Define("XOR {arg}, A", "0010110,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value ^ a.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← A
                .Define("MOV {arg}, A", "0010111,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    mem.Value = a.Value;
                })
                //A ← A + M
                .Define("ADD A, {arg}", "0011000,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value + mem.Value;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A - M
                .Define("SUB A, {arg}", "0011001,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value - mem.Value;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A + M + C
                .Define("ADDC A, {arg}", "0011010,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value + mem.Value + c.Value;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A - M - C
                .Define("SUBC A, {arg}", "0011011,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value - mem.Value - c.Value;
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A & M
                .Define("AND A, {arg}", "0011100,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value & mem.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A | M
                .Define("OR A, {arg}", "0011101,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value | mem.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A ^ M
                .Define("XOR A, {arg}", "0011110,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = a.Value ^ mem.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← M
                .Define("MOV A, {arg}", "0011111,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M + C
                .Define("ADDC {arg}", "0100000,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value + c.Value;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M - C
                .Define("SUBC {arg}", "0100001,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value - c.Value;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M + 1 , skip next instruction if M is 0
                .Define("IZSN {arg}", "0100010,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value + 1;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                    if (result == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //M ← M - 1 , skip next instruction if M is 0
                .Define("DZSN {arg}", "0100011,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value - 1;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                    if (result == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //M ← M + 1
                .Define("INC {arg}", "0100100,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value + 1;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M - 1
                .Define("DEC {arg}", "0100101,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value - 1;
                    UpdateFlags(context, result);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← 0
                .Define("CLEAR {arg}", "0100110,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    mem.Value = 0;
                })
                //Exchange A with M
                .Define("XCHG {arg}", "0100111,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var temp = a.Value;
                    a.Value = mem.Value;
                    mem.Value = temp;
                })
                //M ← ~M
                .Define("NOT {arg}", "0101000,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = mem.Value ^ 0x7FFFFFFF;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← NEG(M)
                .Define("NOT {arg}", "0101001,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = -mem.Value;
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M >> 1
                .Define("SR {arg}", "0101010,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    c.Value = (mem.Value & 1) != 0 ? 1 : 0;
                    var result = (mem.Value & context.Simulator.RegisterMask) >> 1;
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M << 1
                .Define("SL {arg}", "0101011,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    c.Value = (mem.Value & (1 << (context.Simulator.RegisterBits - 1))) != 0 ? 1 : 0;
                    var result = mem.Value << 1;
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← CF:M >> 1
                .Define("SRC {arg}", "0101100,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = (c.Value != 0 ? 1L << (context.Simulator.RegisterBits - 1) : 0) |
                                    ((mem.Value & context.Simulator.RegisterMask) >> 1);
                    c.Value = (mem.Value & 1) != 0 ? 1 : 0;
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //M ← M:CF << 1
                .Define("SLC {arg}", "0101101,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    var result = (c.Value != 0 ? 1 : 0) | (mem.Value << 1);
                    c.Value = (mem.Value & (1 << (context.Simulator.RegisterBits - 1))) != 0 ? 1 : 0;
                    mem.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //Skip next instruction if M is equal to A
                .Define("CEQSN A, {arg}", "0101110,X*6", (ref OpCodeExecutionContext context) =>
                {
                    var mem = context.Simulator.Registers[(int)context.OpCodeArgument[0]];
                    if (a.Value == mem.Value)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Test bit n of IO and skip next instruction if clear
                .Define("T0SN.IO {1}.{0}", "01100,X*3,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var bit = (int)context.OpCodeArgument[0];
                    var mem = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[1]];
                    if ((mem.Value & (1 << bit)) == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Test bit n of IO and skip next instruction if set
                .Define("T1SN.IO {1}.{0}", "01101,X*3,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var bit = (int)context.OpCodeArgument[0];
                    var mem = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[1]];
                    if ((mem.Value & (1 << bit)) != 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //Clear bit n of IO
                .Define("SET0.IO {arg}.{arg}", "01110,X*3,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var bit = (int)context.OpCodeArgument[0];
                    var mem = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[1]];
                    mem.Value &= ~(1 << bit);
                })
                //Set bit n of IO
                .Define("SET1.IO {arg}.{arg}", "01111,X*3,X*5", (ref OpCodeExecutionContext context) =>
                {
                    var bit = (int)context.OpCodeArgument[0];
                    var mem = context.Simulator.Registers[IO_BASE_ADDRESS_FLAG + (int)context.OpCodeArgument[1]];
                    mem.Value |= (1 << bit);
                })
                //A ← A + k
                .Define("ADD A, #{arg}", "10000,X*8", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value + (int)context.OpCodeArgument[0];
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A - k
                .Define("SUB A, #{arg}", "10001,X*8", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value - (int)context.OpCodeArgument[0];
                    UpdateFlags(context, result);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //Skip next instruction if A equals k
                .Define("CEQSN A, #{arg}", "10010,X*8", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value - (int)context.OpCodeArgument[0];
                    UpdateFlags(context, result);
                    if (result == 0)
                    {
                        context.Simulator.InstructionPointer += 2;
                        context.Cycle = 2;
                    }
                })
                //A ← A & k
                .Define("AND A, #{arg}", "10100,X*8", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value & (int)context.OpCodeArgument[0];
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A | k
                .Define("OR A, #{arg}", "10101,X*8", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value | (int)context.OpCodeArgument[0];
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← A ^ k
                .Define("XOR A, #{arg}", "10110,X*8", (ref OpCodeExecutionContext context) =>
                {
                    var result = a.Value ^ (int)context.OpCodeArgument[0];
                    UpdateFlags(context, result, PadaukFlagUpdate.Z);
                    a.Value = (int)(result & context.Simulator.RegisterMask);
                })
                //A ← k
                .Define("MOV A, #{arg}", "10111,X*8", (ref OpCodeExecutionContext context) =>
                {
                    a.Value = (int)context.OpCodeArgument[0];
                })
                //Jump to k (address in words, 2 cycles)
                .Define("GOTO #{arg}", "110,X*10", (ref OpCodeExecutionContext context) =>
                {
                    var address = (int)context.OpCodeArgument[0];
                    context.Simulator.InstructionPointer = address * 2;
                }, instructionCycle: 2, formatter: (arg) => $"GOTO #{arg[0] * 2:X}")
                //Call subroutine k (address in words, 2 cycles)
                .Define("CALL #{arg}", "111,X*10", (ref OpCodeExecutionContext context) =>
                {
                    var address = (int)context.OpCodeArgument[0];
                    Debug.Assert(context.Simulator.InstructionPointer % 2 == 0);
                    PushToStack(context.Simulator, context.Simulator.InstructionPointer / 2, 2);
                    context.Simulator.InstructionPointer = address * 2;
                }, instructionCycle: 2, formatter: (arg) => $"CALL #{arg[0] * 2:X}")
                ;

            var simulator = base.Build();

            pa0.OnChanged += (s, e) =>
            {
                //simulator.Invoke(() =>
                //{
                if (globalInteruptEnabled)
                {
                    if ((pac.Value & (1 << 0)) == 0) //confirm its input configurd
                    {
                        if ((padier.Value & (1 << 0)) != 0) //confirm its digital input
                        {
                            if ((inten.Value & (1 << 0)) != 0) //interrupt is enabled
                            {
                                if ((integsPortA0.TValue == PortA0InterruptEdge.Falling && !pa0.TValue) ||
                                (integsPortA0.TValue == PortA0InterruptEdge.Rising && pa0.TValue) ||
                                (integsPortA0.TValue == PortA0InterruptEdge.RisingAndFalling))
                                {
                                    intrq.Value |= 1 << 0;
                                    RaiseInterrupt(simulator);
                                }
                            }
                        }
                    }
                }
                //});
            };

            return simulator;
        }
    }
}
