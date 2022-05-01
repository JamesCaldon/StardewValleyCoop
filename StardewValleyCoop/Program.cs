using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using StardewValleyCoop;
using static StardewValleyCoop.InstanceHandler;

// ----- Main Console Application -----
// Setup Main application configuration
string applicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
    throw new Exception("Must be run from exe.");
string logsPath = Path.Join(applicationPath, "\\Logs\\");
string handlersPath = Path.Join(applicationPath, "\\handlers\\");

// Log to both file and console.
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new FileLoggerProvider(Path.Join(logsPath, "StardewValleyCoop.log")))
    .AddSimpleConsole(options =>
    {
        options.IncludeScopes = false;
        options.SingleLine = true;
        options.TimestampFormat = "[HH:mm:ss] ";
    });
});

ILogger processHandlerLogger = loggerFactory.CreateLogger(typeof(InstanceHandler));
ILogger logger = loggerFactory.CreateLogger(typeof(Program));



// ----- Handler -----

// Parse config file
string configFile = Path.Join(applicationPath, "StardewValleyCoop.xml");

HandlerConfig handlerConfig = new();

handlerConfig.name = "Stardew Valley";
handlerConfig.injectionType = InstanceHandlerConfig.ProtoInjectionTypeIDs.EasyHookInjectRuntime;

handlerConfig.gameRootPaths = new string[] {
    "E:\\Games\\Stardew Valley\\Stardew Valley - Modded - James\\",
    "E:\\Games\\Stardew Valley\\Stardew Valley - Modded - Mum\\",
    "E:\\Games\\Stardew Valley\\Stardew Valley - Modded - Terry\\"
};
handlerConfig.exeName = "StardewModdingAPI.exe";
handlerConfig.numberOfPlayers = 3;
handlerConfig.useFocusLoop = true;
handlerConfig.useMonitors = new string[] { "MONITOR\\KGN3400\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0005" };//{ "\\\\.\\DISPLAY2" };
handlerConfig.windowTitleRegex = @"^Stardew Valley";
handlerConfig.processorAffinities = new int[] { 0x0003, 0x000C, 0x0030 };

handlerConfig.Save(configFile);
handlerConfig = Config.Load<HandlerConfig>(configFile);

// Start process handlers for each player
List<Thread> handlerThreads = new();
List<InstanceWindowPositionSettings> instancePositionList = new();

if (handlerConfig.resizeWindows)//TODO: Find interactive way to choose which monitor
{
    var allDisplaysInformation = MultipleDisplayInformation.GetAllDisplaysInformation();
    foreach (var display in allDisplaysInformation)
    {
        logger.LogInformation("All Displays Found: {id}, {name}, {posX}, {posY}, {width}, {height}",
            display.id, display.name,
            display.posX, display.posY,
            display.width, display.height);
        if (handlerConfig.useMonitors.Contains(display.name) || handlerConfig.useMonitors.Contains(display.id))
        {
            logger.LogInformation("display id and name to use {id}, {name}, {boolid}, {boolname}", display.id, display.name, handlerConfig.useMonitors.Contains(display.id), handlerConfig.useMonitors.Contains(display.name));
            instancePositionList.AddRange(display.DivideDisplayBetweenPlayers(
                handlerConfig.numberOfPlayers / handlerConfig.useMonitors.Length)); //TODO: Handle odds
        }
    }
    if (instancePositionList.Count != 0) logger.LogInformation("Instance Position Data:");
    foreach (var instancePosition in instancePositionList)
    {
        logger.LogInformation("Display to be used: {name}, {posX}, {posY}, {width}, {height}", 
            instancePosition.displayName,
            instancePosition.posX, instancePosition.posY,
            instancePosition.width, instancePosition.height);
    }
}

for (int instanceIndex = 0; instanceIndex < handlerConfig.numberOfPlayers; instanceIndex++)
{
    InstanceHandlerConfigOptionals instanceHandlerConfigOptionals = new();
    instanceHandlerConfigOptionals.useController = handlerConfig.useControllers;
    instanceHandlerConfigOptionals.controllerIndex = (uint) instanceIndex + 1;
    instanceHandlerConfigOptionals.resizeWindow = handlerConfig.resizeWindows;
    instanceHandlerConfigOptionals.useFocusLoop = handlerConfig.useFocusLoop;
    instanceHandlerConfigOptionals.processorAffinity = handlerConfig.processorAffinities[instanceIndex]; //TODO: CHECK BOUNDS
    if (instancePositionList.Count == handlerConfig.numberOfPlayers)
    {
        instanceHandlerConfigOptionals.windowPositionSettings = instancePositionList[instanceIndex];
        logger.Log(LogLevel.Information,
            "{posX}, {posY}, {width}, {height}", 
            instanceHandlerConfigOptionals.windowPositionSettings.posX,
            instanceHandlerConfigOptionals.windowPositionSettings.posY,
            instanceHandlerConfigOptionals.windowPositionSettings.width,
            instanceHandlerConfigOptionals.windowPositionSettings.height
            );
    }

    InstanceHandlerConfig config = new(
        gameRootPath: handlerConfig.gameRootPaths[instanceIndex], //TODO: not great aye..
        exeName: handlerConfig.exeName,
        windowTitleRegex: handlerConfig.windowTitleRegex,
        instanceIndex: instanceIndex + 1,
        logger: logger,
        injectionType: handlerConfig.injectionType
    );
    config.optionals = instanceHandlerConfigOptionals;

    // Start Application or Inject into runtime
    InstanceHandler processHandler = new(config,
        $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\ProtoInput1.1.2\\",
        processHandlerLogger);


    Thread handlerThread = new(processHandler.StartHandling);
    handlerThreads.Add(handlerThread);
    logger.Log(LogLevel.Information, "Starting new thread for process handler for instance {instance}",
    config.instanceIndex);

    handlerThread.Start();
    Thread.Sleep(7000);
}

//Wait for all processhandlers to exit...
foreach (Thread handlerThread in handlerThreads)
{
    handlerThread.Join();
}

handlerThreads.Clear();

logger.Log(LogLevel.Information, "All Process Handlers exited");

Console.ReadKey();
// Resize at end