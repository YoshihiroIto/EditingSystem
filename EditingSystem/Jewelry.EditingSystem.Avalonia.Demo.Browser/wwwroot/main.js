import { dotnet } from './_framework/dotnet.js';

if (typeof window === 'undefined')
    throw new Error('The EditingSystem demo must run in a browser.');

const runtime = await dotnet
    .withApplicationArgumentsFromQuery()
    .create();

await runtime.runMain(runtime.getConfig().mainAssemblyName, [globalThis.location.href]);
