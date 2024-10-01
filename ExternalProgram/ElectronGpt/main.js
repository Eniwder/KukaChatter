const { app, BrowserWindow, Tray, Menu, dialog } = require('electron');
const path = require('node:path');
const express = require('express');
const bodyParser = require('body-parser');
const server = express();
const stateKeeper = require('electron-window-state');
const debounce = require('throttle-debounce').debounce;
const exec = require('child_process').exec;
const fs = require('fs');

const impCommand = fs.readFileSync('resources/impCommand.txt', 'utf-8');
let impInterval = 60 * 60 * 1000;

const defaultPort = 52322;
let impRead = false;
// let configFrame = true;
let mainWindow;
let tray;
let chatCounter = 0;
let serverInstance = null;
let stopState = false;

const closeHandler = ev => {
  ev.preventDefault();
  mainWindow.hide();
};

server.use(bodyParser.json()); // JSONリクエストを解析するミドルウェア

server.post('/toggle-visibility', (req, res) => {
  if (!mainWindow) {
    createWindow();
    return res.send("show");
  }
  if (mainWindow.isVisible()) {
    mainWindow.hide();
    return res.send("hide");
  } else {
    // 表示する時は最前面に持ってくる
    mainWindow.setAlwaysOnTop(true, "screen-saver");
    mainWindow.show();
    setTimeout(() => mainWindow.setAlwaysOnTop(false, "screen-saver"), 1);
    return res.send("show");
  }
});

server.post('/toggle-impread', (req, res) => {
  impRead = req?.body?.impread;
  return res.sendStatus(200);
});


server.post('/toggle-impread', (req, res) => {
  impRead = req?.body?.impread;
  return res.sendStatus(200);
});

server.post('/toggle-stop', (req, res) => {
  const isStop = req?.body?.stop;
  stopState = isStop;
  if (isStop) {
    mainWindow.hide();
  }
  // createWindow(isSmart);
  res.sendStatus(200);
});

// server.post('/focus', (req, res) => {
//   mainWindow.setAlwaysOnTop(true, "screen-saver");
//   mainWindow.show();
//   setTimeout(() => mainWindow.setAlwaysOnTop(false, "screen-saver"), 1);
//   return res.sendStatus(200);
// });

// server.post('/toggle-frame', (req, res) => {
//   const isSmart = !req?.body?.frame;
//   if (isSmart === configFrame) return res.sendStatus(200);
//   createWindow(isSmart);
//   res.sendStatus(200);
// });

server.post('/close', (req, res) => {
  res.sendStatus(200);
  mainWindow.removeListener('close', closeHandler);
  app.quit();
});

function createWindow() {
  if (mainWindow) mainWindow.close();
  let windowState = stateKeeper({
    defaultWidth: 840,
    defaultHeight: 1000,
  });
  mainWindow = new BrowserWindow({
    x: windowState.x,
    y: windowState.y,
    width: windowState.width,
    height: windowState.height,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      enableRemoteModule: false,
      nodeIntegration: false,
    },
    show: false,
    autoHideMenuBar: true,
    skipTaskbar: true,
    // transparent: true,
    // frame,
    opacity: 0.95
  });

  mainWindow.loadURL('https://chatgpt.com');
  // 最前面固定は不要な気がしてきた
  // mainWindow.setAlwaysOnTop(true, "screen-saver");

  // トレイアイコンを設定
  tray = new Tray('./resources/favicon.png');
  const contextMenu = Menu.buildFromTemplate([
    {
      label: '表示', click: () => {
        mainWindow.show();   // ウィンドウを表示
      }
    },
    { label: '非表示 (終了はLive2Dから)', click: () => { mainWindow.hide(); } }
  ]);
  tray.setContextMenu(contextMenu);

  // トレイアイコンがクリックされたときにウィンドウを表示または非表示
  tray.on('click', () => {
    if (mainWindow.isVisible()) {
      mainWindow.hide();
    } else {
      mainWindow.show();
    }
  });


  mainWindow.webContents.on('did-finish-load', () => {
    mainWindow.webContents.executeJavaScript(`
      const myStyle = document.createElement('style');
      myStyle.textContent = "body { font-family:'TB丸ｺﾞｼｯｸR' }";
      myStyle.textContent += ".sticky { font-family:'TTBPCinemaRGothic-M' }";
      myStyle.textContent += 'div[role="presentation"] div.text-token-text-secondary.text-center { opacity: 0 }';     
      document.head.appendChild(myStyle);
    `).catch((error) => {
      console.error('Error executing script:', error);
    });
  });

  mainWindow.webContents.on('page-title-updated', async (event, url) => {
    currentUrl = url;
    chatCounter = Number(await mainWindow.webContents.executeJavaScript(`
      document.querySelectorAll('article').length;
    `).catch((error) => -1));
  });

  // mainWindow.webContents.openDevTools(); // なんかエラー出る
  windowState.manage(mainWindow);

  mainWindow.on('close', closeHandler);
  mainWindow.on('resize', debounce(300, () => {
    windowState.saveState(mainWindow);
  }, { atBegin: false }));

}

app.whenReady().then(() => {
  createWindow();

  const args = process.argv.slice(1);
  const port = (args.find(arg => arg.startsWith('--port='))?.split('=')[1]) || defaultPort;
  impRead = (args.find(arg => arg.startsWith('--impRead='))?.split('=')[1] === 'true') || false;
  impInterval = Number((args.find(arg => arg.startsWith('--impInterval='))?.split('=')[1]) || impInterval);
  // dialog.showMessageBoxSync({ message: `port:${port}, impRead:${impRead}, impInterval:${impInterval}` });
  // console.log(`Port: ${port}`);

  serverInstance = server.listen(port, () => {
    console.log(`Server running at http://localhost:${port}/`);
  });
  mainWindow.show();

  setInterval(async () => {
    if (stopState) return;
    const count = Number(await mainWindow.webContents.executeJavaScript(`
      document.querySelectorAll('article').length;
    `).catch((error) => -1));
    // console.log(`chatCounter:${chatCounter}, count:${count}, impRead:${impRead}, impInterval:${impInterval}`);
    // dialog.showMessageBoxSync({ message: `chatCounter:${chatCounter}, count:${count}, impRead:${impRead}, impInterval:${impInterval}` });
    if (chatCounter === count) return;
    chatCounter = count;
    if (impRead) {
      getImpression(mainWindow.webContents.getURL());
    }
  }, impInterval);
});

app.on('before-quit', (event) => {
  serverInstance?.close(() => {
    console.log('Server closed');
  });
  tray.destroy();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

function getImpression(url) {
  console.log(url);
  const impWindow = new BrowserWindow({
    x: 0,
    y: 0,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      enableRemoteModule: false,
      nodeIntegration: false,
    },
    width: 1000,
    height: 1000,
    show: false,
  });

  impWindow.loadURL(url);

  const emos = ['喜', '哀', '驚'];
  const emo = emos[parseInt(Math.random() * emos.length)];
  const command = impCommand.replaceAll('@emo@', emo).replace(/\r?\n/g, '\\n');

  impWindow.webContents.on('did-navigate', async (event, url) => {
    setTimeout(() => {
      impWindow.webContents.executeJavaScript(`
        document.querySelector('#prompt-textarea').innerText='${command}';
        document.querySelector('#prompt-textarea').dispatchEvent(new Event('input'));
        setTimeout(() => document.querySelector('button[data-testid="send-button"]').click(),1000);
        `).catch((error) => {
        console.error('Error executing script:', error);
      });
      setTimeout(async () => {
        const text = (await impWindow.webContents.executeJavaScript(`
            var chats = document.querySelectorAll('article [data-message-author-role="assistant"]');
            chats[chats.length - 1].innerText;
          `)).replace(/\n/g, ' ').replace(/[なにぬねのやゆよ]にゃ/g, 'にゃ');
        console.log(emo, text);
        exec(`.\\venv\\Scripts\\python inference.py --text="${text}" --outname="${emo}"`, { cwd: '.\\sbv2\\Style-Bert-VITS2' }, (err, stdout, stderr) => {
          if (err) { console.log(err); }
          console.log(stdout);
        });
      }, 30 * 1000); // 30
    }, 30 * 1000); // 30
  });

}