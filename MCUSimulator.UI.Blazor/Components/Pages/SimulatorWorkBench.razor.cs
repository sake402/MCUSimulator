using LivingThing.Core.Frameworks.Client;
using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.Serialization;
using LivingThing.Core.Frameworks.Common.String;
using MCUSimulator.Core;
using MCUSimulator.Core.ProjectSystem;
using MCUSimulator.Core.SimulationModel;
using MCUSimulator.SimulationModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage.Pickers;
#endif

namespace MCUSimulator.UI.Blazor.Components.Pages
{
    public partial class SimulatorWorkBench : BaseComponent, IDisposable
    {
        [Inject] public IEnumerable<IMCUFactory> MCUFactories { get; set; } = default!;
        [Inject] public IEnumerable<ISimulationModelViewProvider> SimulationModelProviders { get; set; } = default!;
        [Inject] public IJavaScriptRunner JavaScript { get; set; } = default!;
        IMCUFactory? mcuFactory;
        MCU? mcuBuilder;
        MCUSimulatorEngine? simulator;
        MCUSimulatorProjectModel? project;
        string? projectPath;
        Dictionary<int, ElementReference> instructionView = new Dictionary<int, ElementReference>();
        const string ProjectFileExtension = ".mcusim";
        MCUSimulatorWatch newWatch = new MCUSimulatorWatch();

        void AddNewWatch()
        {
            if (simulator != null &&
                project != null &&
                !string.IsNullOrEmpty(newWatch.Name) &&
                newWatch.Type != MCUSimulatorWatchType.None &&
                !string.IsNullOrEmpty(newWatch.RegisterId) &&
                simulator.Registers.GetById(newWatch.RegisterId) != null)
            {
                project.Watches ??= new List<MCUSimulatorWatch>();
                project.Watches.Add(newWatch);
                newWatch = new MCUSimulatorWatch();
            }
        }

        void Reset()
        {
            mcuFactory = null;
            mcuBuilder = null;
            simulator?.Dispose();
            simulator = null;
            project = null;
            projectPath = null;
            simulationModels.Clear();
        }

        async Task<string?> PickAFile(string prompt, string extension)
        {
            var file = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = prompt,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>()
                {
                    [DevicePlatform.WinUI] = [extension]
                })
            });
            return file?.FullPath;
        }

        async Task LoadProject(IMCUFactory factory, MCUSimulatorProjectModel project)
        {
            this.project = project;
            mcuFactory = factory;
            mcuBuilder = factory.CreateMCU();
            simulator = mcuBuilder.Build();
            project.MCUParameter?.DeSerialize(mcuBuilder.BuilderParameter);
            if (!File.Exists(project.ProgramFile))
            {
                var file = await PickAFile("Select Program File", "*.bin");
                if (file == null)
                    return;
                project.ProgramFile = file;
            }
            var binFile = File.ReadAllBytes(project.ProgramFile!);
            simulator.Load(binFile);
            if (project.SimulationModels != null)
            {
                foreach (var model in project.SimulationModels)
                {
                    var provider = SimulationModelProviders.FirstOrDefault(p => p.GetType().FullName == model.Provider);
                    if (provider != null)
                    {
                        var simulationModel = provider.Build();
                        simulationModel.Name = model.Name;
                        model.Parameters?.DeSerialize(simulationModel.Parameter);
                        if (model.Inputs != null)
                        {
                            foreach (var input in model.Inputs)
                            {
                                var _in = simulationModel.Inputs.Values.FirstOrDefault(i => i.Name == input.Key);
                                if (_in != null)
                                    _in.Connect((IReadableMemory?)simulator.Registers.GetById(input.Value));
                            }
                        }
                        if (model.Outputs != null)
                        {
                            foreach (var output in model.Outputs)
                            {
                                var _out = simulationModel.Outputs.Values.FirstOrDefault(i => i.Name == output.Key);
                                if (_out != null)
                                    _out.Connect((IWritableMemory?)simulator.Registers.GetById(output.Value));
                            }
                        }
                        simulationModel.Initialize(simulator);
                        simulationModels.Add(simulationModel);
                    }
                }
            }
            if (project.BreakPoints != null)
            {
                foreach (var bkp in project.BreakPoints)
                {
                    simulator.Instructions![bkp].ToggleBreakPoint();
                }
            }

        }
        async Task NewProject(IMCUFactory mcuFactory)
        {
            var file = await PickAFile("Select Program File (bin)", "*.bin");
            if (file != null)
            {
                var project = new MCUSimulatorProjectModel
                {
                    MCUFactory = mcuFactory.GetType().FullName!,
                    ProgramFile = file
                };
                await LoadProject(mcuFactory, project);
            }
        }

        async Task OpenExistingProject()
        {
            Reset();
            var file = await PickAFile("Select Project File (json)", "*" + ProjectFileExtension);
            if (file != null)
            {
                projectPath = file;
                var project = File.ReadAllText(file).DeSerialize<MCUSimulatorProjectModel>();
                var mcuFactory = MCUFactories.FirstOrDefault(e => e.GetType().FullName == project.MCUFactory);
                if (mcuFactory == null)
                {
                    mcuFactory = await UnitOfWork.UI.Overlay.Select(
                        "Choose MCU",
                        "We couldn't find the MCU device that was used last with this project. Please select a device!",
                        e => e.Name,
                        MCUFactories);
                    if (mcuFactory != null)
                    {
                        project.MCUFactory = mcuFactory.GetType().FullName!;
                    }
                }
                if (mcuFactory != null)
                {
                    await LoadProject(mcuFactory, project);
                }
            }
        }

        FolderPicker? picker;
        async Task SaveProject()
        {
            if (simulator != null && mcuBuilder != null && project != null)
            {
                if (projectPath == null)
                {
                    var projectName = await UnitOfWork.UI.Overlay.Form<ViewConfiguration, string>(title: "Project Name".AsRenderFragment<object?>());
                    if (picker == null)
                    {
                        picker = new FolderPicker();
                        picker.FileTypeFilter.Add("*");
#if WINDOWS
                        var hwnd = ((MauiWinUIWindow)App.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
                        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif
                    }
                    var folder = await picker.PickSingleFolderAsync();
                    if (folder?.Path != null)
                    {
                        projectPath = Path.Combine(folder.Path, projectName + ProjectFileExtension);
                    }
                }
                if (projectPath != null)
                {
                    project.MCUParameter = mcuBuilder.BuilderParameter.Serialize();
                    project.SimulationModels = simulationModels.Select(m => new MCUSimulatorComponentModel
                    {
                        Name = m.Name,
                        Provider = m.Provider!.GetType().FullName,
                        Parameters = m.Parameter.Serialize(),
                        Inputs = m.Inputs.ToDictionary(i => i.Value.Name, i => i.Value.ConnectedMemory?.Id),
                        Outputs = m.Outputs.ToDictionary(i => i.Value.Name, i => i.Value.ConnectedMemory?.Id),
                    });
                    project.BreakPoints = simulator.Instructions?.Values.Where(e => e.BreakPoint).Select(e => e.StartAddress).ToList();
                    var json = project.Serialize().Json;
                    File.WriteAllText(projectPath, json);
                }
            }
        }

        ISimulationModel? selectedSimulationModel;
        List<ISimulationModel> simulationModels = new List<ISimulationModel>();

        async Task ConfigureMCU()
        {
            if (mcuBuilder != null)
            {
                await UnitOfWork.UI.Overlay.Form<ViewConfiguration, object>(
                    title: $"Configure {mcuBuilder.Name}".AsRenderFragment<object?>(),
                    model: mcuBuilder.BuilderParameter);
            }
        }

        async Task ConfigureModel(ISimulationModel model)
        {
            await UnitOfWork.UI.Overlay.Form<ViewConfiguration, ISimulationModel>(
                    title: $"Configure {model.Name}".AsRenderFragment<object?>(),
                    model: model);
        }

        void Remove(ISimulationModel model)
        {
            model.Dispose();
            if (selectedSimulationModel == model)
                selectedSimulationModel = null;
            simulationModels.Remove(model);
        }

        async Task AddModel()
        {
            var modelProvider = (await UnitOfWork.UI.Overlay.Select<ViewConfiguration, ISimulationModelViewProvider>(SimulationModelProviders.Select(s => new SelectOption<ISimulationModelViewProvider>(s)), "Select Model".AsRenderFragment<object?>())).FirstOrDefault();
            if (modelProvider != null)
            {
                var model = modelProvider.Build();
                simulationModels.Add(model);
                selectedSimulationModel = model;
                await ConfigureModel(model);
                model.Initialize(simulator!);
            }
        }

        void Keyed(KeyboardEventArgs arg)
        {

        }

        void UpdateModels()
        {
            if (simulator != null)
            {
                foreach (var model in simulationModels)
                {
                    model.Initialize(simulator);
                }
            }
        }

        protected override void OnInitialized()
        {
            _ = Task.Run(async () =>
            {
                while (!cancelRefresh.IsCancellationRequested)
                {
                    await Task.Delay(500, cancelRefresh.Token);
                    _ = StateChanged();
                }
            });
            base.OnInitialized();
        }

        void ParseInputValue(string value, IRegister register)
        {
            var val = value.ParseBinary(out _);
            register.Value = (int)val;
        }

        int lastInstructionPointer = -1;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (simulator != null && !simulator!.Running)
            {
                var next = simulator.InstructionPointer;
                if (next != lastInstructionPointer)
                {
                    if (instructionView.TryGetValue(next, out var element))
                        await JavaScript.ScrollTo(element);
                    lastInstructionPointer = simulator?.InstructionPointer ?? -1;
                }
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        CancellationTokenSource cancelRefresh = new CancellationTokenSource();
        public override void Dispose()
        {
            cancelRefresh.Cancel();
            base.Dispose();
        }
    }
}
