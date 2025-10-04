#!/usr/bin/env node

/**
 * 🤖 Claude-AION CLI
 * Command-line interface for Claude Code to control AION Desktop Assistant
 *
 * Usage:
 *   node claude-aion-cli.js status
 *   node claude-aion-cli.js read-screen
 *   node claude-aion-cli.js move-mouse 100 200
 *   node claude-aion-cli.js click
 *   node claude-aion-cli.js type "Hello World"
 *   node claude-aion-cli.js speak "Hello from Claude"
 *   node claude-aion-cli.js execute "read screen and tell me what you see"
 */

const http = require('http');

const AION_API_URL = 'http://localhost:8080';

// 🎨 Console colors
const colors = {
    reset: '\x1b[0m',
    bright: '\x1b[1m',
    red: '\x1b[31m',
    green: '\x1b[32m',
    yellow: '\x1b[33m',
    blue: '\x1b[34m',
    magenta: '\x1b[35m',
    cyan: '\x1b[36m',
};

function log(emoji, color, message) {
    console.log(`${emoji} ${color}${message}${colors.reset}`);
}

function logSuccess(message) {
    log('✅', colors.green, message);
}

function logError(message) {
    log('❌', colors.red, message);
}

function logInfo(message) {
    log('ℹ️', colors.blue, message);
}

function logWarning(message) {
    log('⚠️', colors.yellow, message);
}

// 🌐 Make HTTP request to AION API
async function callAionApi(endpoint, method = 'GET', body = null) {
    return new Promise((resolve, reject) => {
        const url = new URL(endpoint, AION_API_URL);

        const options = {
            hostname: url.hostname,
            port: url.port,
            path: url.pathname,
            method: method,
            headers: {
                'Content-Type': 'application/json',
            }
        };

        if (body) {
            const bodyString = JSON.stringify(body);
            options.headers['Content-Length'] = Buffer.byteLength(bodyString);
        }

        const req = http.request(options, (res) => {
            let data = '';

            res.on('data', (chunk) => {
                data += chunk;
            });

            res.on('end', () => {
                try {
                    const response = JSON.parse(data);
                    resolve(response);
                } catch (e) {
                    reject(new Error(`Failed to parse response: ${data}`));
                }
            });
        });

        req.on('error', (error) => {
            reject(error);
        });

        if (body) {
            req.write(JSON.stringify(body));
        }

        req.end();
    });
}

// 📊 Get AION status
async function getStatus() {
    logInfo('Checking AION Desktop Assistant status...');

    try {
        const response = await callAionApi('/api/status');

        if (response.Success) {
            logSuccess('AION Desktop Assistant is running!');
            console.log(JSON.stringify(response.Data, null, 2));
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Cannot connect to AION: ${error.message}`);
        logWarning('Make sure AION Desktop Assistant is running with Remote Control enabled');
    }
}

// 👁️ Read screen text
async function readScreen() {
    logInfo('Reading screen with OCR...');

    try {
        const response = await callAionApi('/api/screen/read');

        if (response.Success) {
            logSuccess('Screen text extracted:');
            console.log('\n' + colors.cyan + response.Data.Text + colors.reset + '\n');
            logInfo(`Total characters: ${response.Data.Length}`);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 📸 Capture screen
async function captureScreen() {
    logInfo('Capturing screen...');

    try {
        const response = await callAionApi('/api/screen/capture');

        if (response.Success) {
            logSuccess('Screenshot captured!');
            logInfo(`Resolution: ${response.Data.Width}x${response.Data.Height}`);
            logInfo('Image data (base64): ' + response.Data.Image.substring(0, 50) + '...');
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🖱️ Move mouse
async function moveMouse(x, y) {
    logInfo(`Moving mouse to (${x}, ${y})...`);

    try {
        const response = await callAionApi('/api/mouse/move', 'POST', { X: parseInt(x), Y: parseInt(y) });

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🖱️ Click mouse
async function clickMouse(x = null, y = null) {
    const position = (x !== null && y !== null) ? ` at (${x}, ${y})` : ' at current position';
    logInfo(`Clicking mouse${position}...`);

    try {
        const body = (x !== null && y !== null) ? { X: parseInt(x), Y: parseInt(y) } : {};
        const response = await callAionApi('/api/mouse/click', 'POST', body);

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// ⌨️ Type text
async function typeText(text) {
    logInfo(`Typing text: "${text}"...`);

    try {
        const response = await callAionApi('/api/keyboard/type', 'POST', { Text: text });

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// ⌨️ Press key
async function pressKey(key) {
    logInfo(`Pressing key: ${key}...`);

    try {
        const response = await callAionApi('/api/keyboard/press', 'POST', { Key: key });

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🪟 List windows
async function listWindows() {
    logInfo('Listing open windows...');

    try {
        const response = await callAionApi('/api/window/list');

        if (response.Success) {
            logSuccess(`Found ${response.Data.Count} windows:`);
            response.Data.Windows.forEach((window, index) => {
                console.log(`  ${index + 1}. ${colors.cyan}${window}${colors.reset}`);
            });
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🪟 Switch window
async function switchWindow(windowName) {
    logInfo(`Switching to window: "${windowName}"...`);

    try {
        const response = await callAionApi('/api/window/switch', 'POST', { WindowName: windowName });

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logWarning(response.Message);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🔊 Speak text
async function speak(text) {
    logInfo(`Speaking: "${text}"...`);

    try {
        const response = await callAionApi('/api/voice/speak', 'POST', { Text: text });

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🎤 Start listening
async function startListening() {
    logInfo('Starting voice recognition...');

    try {
        const response = await callAionApi('/api/voice/listen', 'POST');

        if (response.Success) {
            logSuccess(response.Message);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// ♿ Analyze accessibility
async function analyzeAccessibility() {
    logInfo('Analyzing accessibility...');

    try {
        const response = await callAionApi('/api/accessibility/analyze');

        if (response.Success) {
            logSuccess('Accessibility analysis completed:');
            console.log(JSON.stringify(response.Data, null, 2));
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 🎯 Execute natural language command
async function executeCommand(command) {
    logInfo(`Executing command: "${command}"...`);

    try {
        const response = await callAionApi('/api/command/execute', 'POST', { Command: command });

        if (response.Success) {
            logSuccess('Command executed:');
            console.log(colors.cyan + response.Message + colors.reset);
        } else {
            logError(`Failed: ${response.Message}`);
        }
    } catch (error) {
        logError(`Error: ${error.message}`);
    }
}

// 📖 Show help
function showHelp() {
    console.log(`
${colors.bright}${colors.blue}🤖 Claude-AION CLI - Control AION Desktop Assistant from Claude Code${colors.reset}

${colors.bright}Usage:${colors.reset}
  node claude-aion-cli.js <command> [arguments]

${colors.bright}Commands:${colors.reset}

  ${colors.green}status${colors.reset}
    Check if AION Desktop Assistant is running

  ${colors.green}read-screen${colors.reset}
    Extract text from screen using OCR

  ${colors.green}capture-screen${colors.reset}
    Capture screenshot (returns base64)

  ${colors.green}move-mouse <x> <y>${colors.reset}
    Move mouse to coordinates
    Example: node claude-aion-cli.js move-mouse 500 300

  ${colors.green}click [x] [y]${colors.reset}
    Click mouse at current position or specified coordinates
    Example: node claude-aion-cli.js click
    Example: node claude-aion-cli.js click 500 300

  ${colors.green}type <text>${colors.reset}
    Type text using keyboard automation
    Example: node claude-aion-cli.js type "Hello World"

  ${colors.green}press <key>${colors.reset}
    Press a specific key
    Example: node claude-aion-cli.js press Enter

  ${colors.green}list-windows${colors.reset}
    List all open windows

  ${colors.green}switch-window <name>${colors.reset}
    Switch to a specific window
    Example: node claude-aion-cli.js switch-window "Chrome"

  ${colors.green}speak <text>${colors.reset}
    Speak text using text-to-speech
    Example: node claude-aion-cli.js speak "Hello from Claude"

  ${colors.green}listen${colors.reset}
    Start voice recognition

  ${colors.green}analyze${colors.reset}
    Analyze accessibility of current screen

  ${colors.green}execute <command>${colors.reset}
    Execute a natural language command
    Example: node claude-aion-cli.js execute "read screen and tell me what you see"

  ${colors.green}help${colors.reset}
    Show this help message

${colors.bright}Examples for Claude Code:${colors.reset}

  ${colors.cyan}# Check AION status
  node claude-aion-cli.js status

  # Read what's on the screen
  node claude-aion-cli.js read-screen

  # Control the mouse
  node claude-aion-cli.js move-mouse 500 300
  node claude-aion-cli.js click

  # Type and interact
  node claude-aion-cli.js type "Hello from Claude!"
  node claude-aion-cli.js press Enter

  # Voice interaction
  node claude-aion-cli.js speak "I am Claude, and I'm controlling this computer"

  # Natural language commands
  node claude-aion-cli.js execute "list all open windows"
  node claude-aion-cli.js execute "read the screen"${colors.reset}

${colors.bright}${colors.yellow}⚠️  Important:${colors.reset}
  - AION Desktop Assistant must be running with Remote Control enabled
  - Default API endpoint: http://localhost:8080
  - Some operations require administrator privileges

${colors.bright}${colors.magenta}🔗 Integration:${colors.reset}
  This CLI allows Claude Code to fully control AION Desktop Assistant,
  creating a bidirectional AI-powered accessibility system.
`);
}

// 🚀 Main execution
async function main() {
    const args = process.argv.slice(2);
    const command = args[0];

    if (!command || command === 'help' || command === '--help' || command === '-h') {
        showHelp();
        return;
    }

    console.log(`\n${colors.bright}${colors.magenta}🤖 Claude-AION CLI${colors.reset}\n`);

    switch (command) {
        case 'status':
            await getStatus();
            break;

        case 'read-screen':
        case 'read':
            await readScreen();
            break;

        case 'capture-screen':
        case 'capture':
        case 'screenshot':
            await captureScreen();
            break;

        case 'move-mouse':
        case 'move':
            if (args.length < 3) {
                logError('Missing coordinates. Usage: move-mouse <x> <y>');
                return;
            }
            await moveMouse(args[1], args[2]);
            break;

        case 'click':
            if (args.length >= 3) {
                await clickMouse(args[1], args[2]);
            } else {
                await clickMouse();
            }
            break;

        case 'type':
            if (args.length < 2) {
                logError('Missing text. Usage: type <text>');
                return;
            }
            await typeText(args.slice(1).join(' '));
            break;

        case 'press':
            if (args.length < 2) {
                logError('Missing key. Usage: press <key>');
                return;
            }
            await pressKey(args[1]);
            break;

        case 'list-windows':
        case 'windows':
        case 'list':
            await listWindows();
            break;

        case 'switch-window':
        case 'switch':
            if (args.length < 2) {
                logError('Missing window name. Usage: switch-window <name>');
                return;
            }
            await switchWindow(args.slice(1).join(' '));
            break;

        case 'speak':
        case 'say':
            if (args.length < 2) {
                logError('Missing text. Usage: speak <text>');
                return;
            }
            await speak(args.slice(1).join(' '));
            break;

        case 'listen':
        case 'voice':
            await startListening();
            break;

        case 'analyze':
        case 'accessibility':
        case 'a11y':
            await analyzeAccessibility();
            break;

        case 'execute':
        case 'exec':
        case 'cmd':
            if (args.length < 2) {
                logError('Missing command. Usage: execute <command>');
                return;
            }
            await executeCommand(args.slice(1).join(' '));
            break;

        default:
            logError(`Unknown command: ${command}`);
            logInfo('Run "node claude-aion-cli.js help" for usage information');
    }

    console.log(); // Empty line at the end
}

// Run main function
main().catch((error) => {
    logError(`Fatal error: ${error.message}`);
    process.exit(1);
});
