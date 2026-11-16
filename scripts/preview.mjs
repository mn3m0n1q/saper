// Adapter for the workspace preview daemon. Build Release before starting.
import { spawn } from 'node:child_process';
const args = process.argv.slice(2);
const port = args.includes('--port') ? args[args.indexOf('--port') + 1] : (process.env.PORT || '3000');
const child = spawn('/workspace/.dotnet/dotnet', ['bin/Release/net10.0/Saper.dll', '--urls', `http://0.0.0.0:${port}`], { stdio: 'inherit' });
for (const signal of ['SIGTERM', 'SIGINT']) process.on(signal, () => child.kill(signal));
child.on('exit', code => process.exit(code ?? 1));
