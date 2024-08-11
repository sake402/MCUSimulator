using MCUSimulator.Targets.Padauk.pdk13;

var mcuBuilder = new PDK13Builder();

var simulator = mcuBuilder.Build();

var binFile = File.ReadAllBytes("E:\\Embedded\\PriceTag\\Source\\Padauk\\Release\\PriceTag.Padauk.bin");

simulator.Load(binFile);

simulator.ConsoleLog = true;

simulator.Clock.Run(simulator);

//await simulator.RunAsync(CancellationToken.None);