import { dotnet } from './_framework/dotnet.js';

const root = document.getElementById('out');

try {
    if (typeof window === 'undefined')
        throw new Error('The EditingSystem demo must run in a browser.');

    const runtime = await dotnet
        .withApplicationArgumentsFromQuery()
        .create();

    const config = runtime.getConfig();
    await runtime.runMain(config.mainAssemblyName, [globalThis.location.href]);
}
catch (error) {
    console.error('EditingSystem Avalonia Demo failed to start.', error);
    root.classList.add('startup-error');
    root.textContent = `The demo failed to start: ${error instanceof Error ? error.message : String(error)}`;
}
