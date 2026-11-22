const fs=require('fs'),path=require('path'),cp=require('child_process');
const folder=path.join(__dirname,'ui-verification');fs.mkdirSync(folder,{recursive:true});
const pre=`<script>
window.uiErrors=[];window.addEventListener('error',e=>uiErrors.push(e.message));window.sent=[];window.listeners=[];
window.chrome=window.chrome||{};window.chrome.webview={addEventListener:(name,fn)=>listeners.push(fn),postMessage:message=>{sent.push(message);if(message.type==='ready')setTimeout(()=>deliver({type:'init',language:'en',provider:'Google Api',offlineReady:true,hasGoogleKey:false,systemLanguage:'ru',uiLanguage:'ru',hotkey:''}),0);}};
window.deliver=message=>listeners.forEach(fn=>fn({data:message}));
</script>`;
const test=`<script>
setTimeout(async()=>{const checks=[];const q=s=>document.querySelector(s),root=q('#translator-original-draft'),dialog=q('.app-settings'),input=q('.to-input');
function check(ok,label){if(!ok)throw Error(label);checks.push(label);}
function click(s){q(s).click();}
try {
check(uiErrors.length===0,'startup without JavaScript errors');check(sent.some(x=>x.type==='ready'),'native bridge initialized');
check(q('.to-language-label').textContent==='English','language label rendered');
check(!q('#to-language-menu>.to-caption'),'language menu heading removed');check(q('.to-languages').children.length===11,'eleven translation languages');
click('.to-provider-trigger');check(!q('#to-provider-menu').hidden,'provider menu opens');
check([...document.querySelectorAll('[data-provider]')].map(x=>x.dataset.provider).join(',')==='Google Api,Offline','two providers');
check([...document.querySelectorAll('[data-provider]')].every(x=>x.querySelector('.to-provider-dot'))&&!q('#to-provider-menu').textContent.includes('✓'),'provider dots replace checkmarks');
const settingsButton=[...q('#to-provider-menu').querySelectorAll('button')].find(x=>x.textContent==='Настройки');check(!!settingsButton,'settings menu label');settingsButton.click();
check(dialog.open&&root.hidden,'settings opens');check(q('[data-page="googleKey"]').href==='https://aistudio.google.com/api-keys','AI Studio link');
check(q('[name="hotkey"]').value==='Win+0'&&q('[name="hotkeyEnabled"]').checked,'Win+0 enabled by default');
q('[name="hotkey"]').dispatchEvent(new KeyboardEvent('keydown',{key:'7',code:'Digit7',metaKey:true,bubbles:true,cancelable:true}));check(q('[name="hotkey"]').value==='Win+7','custom Win shortcut');
q('[name="hotkey"]').dispatchEvent(new KeyboardEvent('keydown',{key:'t',code:'KeyT',ctrlKey:true,altKey:true,bubbles:true,cancelable:true}));check(q('[name="hotkey"]').value==='Ctrl+Alt+T','custom Ctrl+Alt shortcut');
click('.app-ui-trigger');[...q('.app-ui-menu').children].find(x=>x.textContent==='English').click();check(q('h2').textContent==='Settings','interface language selection');
q('[name="googleKey"]').value='test-only-placeholder';q('.app-settings-form').dispatchEvent(new Event('submit',{cancelable:true,bubbles:true}));
check(sent.some(x=>x.type==='saveSettings'&&x.googleKey==='test-only-placeholder'&&x.uiLanguage==='en'&&x.hotkey==='Ctrl+Alt+T'&&x.winZeroEnabled),'settings save reaches native bridge');
deliver({type:'settingsSaved',ok:true});await new Promise(resolve=>setTimeout(resolve,50));check(!dialog.open&&!root.hidden,'settings closes after save');
input.value='Hello';input.dispatchEvent(new Event('input'));check(!q('.to-text-action').hidden&&!q('.to-action-clear').hidden,'clear action shown');click('.to-text-action');check(input.value==='','clear empties text');
input.value='Hello';input.dispatchEvent(new Event('input'));q('form').dispatchEvent(new Event('submit',{cancelable:true,bubbles:true}));const request=sent.filter(x=>x.type==='translate').pop();check(!!request,'translation dispatched');
deliver({type:'translation',requestId:request.requestId,ok:true,text:'Привет',source:'en',target:'ru'});check(input.value==='Привет'&&!q('.to-action-copy').hidden,'translation replaces text and shows copy');click('.to-text-action');check(sent.some(x=>x.type==='copyText'&&x.text==='Привет'),'copy dispatched');
click('.to-mic');check(sent.some(x=>x.type==='microphone'&&x.provider==='Google Api'),'voice uses selected provider');
deliver({type:'speechState',state:'recording'});check(q('.to-mic').classList.contains('is-recording')&&!q('.to-mic').disabled&&q('.to-send').disabled,'recording indicator and stop control');click('.to-mic');check(sent.filter(x=>x.type==='microphone').length===2,'second click stops recording');
deliver({type:'speechState',state:'processing'});check(q('.to-mic').disabled,'recording cannot restart while transcribing');deliver({type:'speech',ok:true,text:'Распознанная фраза'});deliver({type:'speechState',state:'idle'});check(input.value==='Распознанная фраза'&&!q('.to-send').disabled&&!q('.to-action-clear').hidden,'transcription appears as editable input');
deliver({type:'speech',ok:false,error:'Test microphone error'});check(input.value==='Распознанная фраза','voice error preserves existing text');
for(const code of ['uk','kk','zh','ja','ko']){q('[data-language='+code+']').click();input.value='Hello';input.dispatchEvent(new Event('input'));q('form').dispatchEvent(new Event('submit',{cancelable:true,bubbles:true}));const request=sent.filter(x=>x.type==='translate').pop();check(request.target===code,'new language request: '+code);deliver({type:'translation',requestId:request.requestId,ok:true,text:'Распознанная фраза',source:'en',target:code});}
deliver({type:'openSettings'});check(dialog.open,'settings opens via tray message');document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}));check(sent.some(x=>x.type==='minimize'),'Escape hides to tray');deliver({type:'hideTranslator'});await new Promise(resolve=>setTimeout(resolve,50));deliver({type:'showTranslator'});check(!dialog.open&&!root.hidden&&input.value==='Распознанная фраза','hotkey returns to input with text preserved');deliver({type:'openSettings'});check(uiErrors.length===0,'no runtime errors after interactions');
document.body.dataset.testResult=JSON.stringify({ok:true,checks});
}catch(e){document.body.dataset.testResult=JSON.stringify({ok:false,error:e.message,uiErrors,checks});}
},250);
</script>`;
fs.writeFileSync(path.join(folder,'smoke.html'),fs.readFileSync('index.html','utf8').replace('<head>','<head>'+pre).replace('</body>',test+'</body>'));
const edge='C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const profile=fs.mkdtempSync(path.join(folder,'edge-profile-'));
const result=cp.spawnSync(edge,['--headless','--disable-gpu','--no-first-run','--user-data-dir='+profile,'--virtual-time-budget=2500','--window-size=1020,740','--screenshot='+path.join(folder,'settings.png'),'--dump-dom',new URL('file:///'+path.join(folder,'smoke.html').replaceAll('\\','/')).href],{encoding:'utf8',timeout:45000,windowsHide:true,maxBuffer:4*1024*1024});
if(result.error)throw result.error;
const match=result.stdout.match(/data-test-result="([^"]*)"/);if(!match){fs.writeFileSync(path.join(folder,'failed-dom.html'),result.stdout||'');console.error('Browser exit:',result.status,result.stderr.slice(-1500));throw Error('No browser test result');}
const report=JSON.parse(match[1].replaceAll('&quot;','"').replaceAll('&amp;','&').replaceAll('&lt;','<').replaceAll('&gt;','>'));
fs.writeFileSync(path.join(folder,'result.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));if(!report.ok)process.exitCode=1;
