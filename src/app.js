(() => {
 'use strict';
 const root=document.getElementById('translator-original-draft');
 const q=selector=>root.querySelector(selector);
 const input=createSpellingEditor(q('.to-input')),form=q('form'),toast=document.querySelector('.app-toast'),settings=document.querySelector('.app-settings');
 const bridge=window.chrome?.webview;
 const names={ru:'Russian',en:'English',uz:'Uzbek',de:'German',fr:'French',es:'Spanish',uk:'Ukrainian',kk:'Kazakh',zh:'Chinese',ja:'Japanese',ko:'Korean'};
 const state={source:'auto',target:'ru',language:'ru',provider:'Offline'};
 let hasTranslation=false,lastSource='en',lastTarget='ru';
 let uiLanguage='system',savedUiLanguage='system',systemLanguage='ru',locale='ru',hasGoogleKey=false;
 const t=key=>translations[locale][key]||key;
 let busy=false,translationPending=false,requestId=0,previousText='',toastTimer,updating=false,offlineReady=false;
 const focusInput=()=>input.focus({preventScroll:true});
 const post=message=>bridge?.postMessage(message);
 let spellcheckEnabled=true,spellTimer,spellSequence=0,spellInFlight=false,spellCooldown=0,composing=false,spellingSuggestions=[];
 const spellingCache=new Map();
 function canCheckSpelling(){return spellcheckEnabled&&hasGoogleKey&&state.provider==='Google Api'&&!busy&&!hasTranslation&&!root.hidden&&!settings.open&&!composing&&Date.now()>=spellCooldown;}
 function clearSpelling(){clearTimeout(spellTimer);spellSequence++;if(spellInFlight)post({type:'cancelSpellcheck'});spellInFlight=false;spellingSuggestions=[];input.clearSpellingHints();}
 function showSpelling(text,items){spellingSuggestions=items;input.showSpellingHints(text,items,t('applySpelling'));fit();}
 function scheduleSpelling(){clearSpelling();const text=input.value;if(!canCheckSpelling()||text.length>5000||(text.match(/\p{L}/gu)||[]).length<2)return;if(spellingCache.has(text)){showSpelling(text,spellingCache.get(text));return;}
  spellTimer=setTimeout(()=>{if(!canCheckSpelling()||input.value!==text)return;spellInFlight=true;post({type:'spellcheck',requestId:++spellSequence,text});},1100);
 }
 input.addEventListener('spellingaccept',event=>{const correction=event.detail;if(busy||hasTranslation||input.value!==correction.text)return;const text=input.value,delta=correction.replacement.length-(correction.end-correction.start);const remaining=spellingSuggestions.filter(item=>item.start!==correction.start).map(item=>item.start>=correction.end?{...item,start:item.start+delta,end:item.end+delta}:item);clearSpelling();previousText=text;input.value=text.slice(0,correction.start)+correction.replacement+text.slice(correction.end);hasTranslation=false;render();showSpelling(input.value,remaining);spellingCache.set(input.value,remaining);fit();focusInput();input.setSelectionRange(correction.start+correction.replacement.length);});
 input.addEventListener('compositionstart',()=>{composing=true;clearSpelling();});input.addEventListener('compositionend',()=>{composing=false;scheduleSpelling();});
 const textAction=q('.to-text-action');
 const measure=document.createElement('div');measure.className='to-measure';measure.setAttribute('aria-hidden','true');form.append(measure);
 function positionTextAction(){
  textAction.hidden=!input.value||busy||root.hidden;if(textAction.hidden)return;
  const style=getComputedStyle(input);for(const key of ['fontFamily','fontSize','fontWeight','lineHeight','letterSpacing'])measure.style[key]=style[key];
  measure.style.width=Math.max(1,input.clientWidth-parseFloat(style.paddingRight||0)-parseFloat(style.paddingLeft||0))+'px';
  measure.replaceChildren(document.createTextNode(input.value));const marker=document.createElement('span');marker.textContent='\u200b';measure.append(marker);
  const markerBox=marker.getBoundingClientRect(),measureBox=measure.getBoundingClientRect();
  const size=textAction.getBoundingClientRect(),visual=input.getVisualEndRect(),inputBox=input.getBoundingClientRect();const endX=visual?visual.right-inputBox.left:markerBox.left-measureBox.left,endY=visual?visual.top-inputBox.top:markerBox.top-measureBox.top-input.scrollTop;
  const left=input.offsetLeft+Math.min(Math.max(0,input.clientWidth-size.width),endX+parseFloat(style.fontSize)*.15);
  const top=input.offsetTop+Math.max(0,Math.min(input.clientHeight-size.height,endY+(parseFloat(style.lineHeight)-size.height)/2));
  textAction.style.left=left+'px';textAction.style.top=top+'px';
  q('.to-action-copy').hidden=!hasTranslation;q('.to-action-clear').hidden=hasTranslation;
  textAction.setAttribute('aria-label',t(hasTranslation?'copyText':'clearText'));textAction.title=t(hasTranslation?'copyText':'clearText');
 }
 textAction.addEventListener('click',()=>{if(hasTranslation){post({type:'copyText',text:input.value});}else{clearSpelling();input.value='';previousText='';hasTranslation=false;notify('');render();fit();focusInput();}});
 input.addEventListener('scroll',positionTextAction);
 function notify(message){clearTimeout(toastTimer);toast.textContent=locale==='en'?(englishErrors[message]||(/[а-яё]/i.test(message)?'Unable to complete this action. Please check your settings.':message)):message;toast.hidden=!message;if(message)toastTimer=setTimeout(()=>{toast.hidden=true;},6500);}
 function resizeInput(){if(root.hidden)return;const width=form.clientWidth,base=width*.0647;input.style.height='1px';const height=Math.min(260,Math.max(base,input.scrollHeight));input.style.height=height+'px';form.style.height=(width*157/728+height-base)+'px';positionTextAction();}
 let fitQueued=false;
 function fit(){if(fitQueued)return;fitQueued=true;queueMicrotask(()=>{fitQueued=false;if(settings.open){post({type:'resize',mode:'settings',height:Math.ceil(settings.getBoundingClientRect().bottom+24)});return;}resizeInput();const card=form.getBoundingClientRect();const visualHeight=Math.ceil(card.bottom+25),required=Math.ceil(card.bottom+340);q('.to-stage').style.paddingBottom='340px';post({type:'resize',mode:'card',height:required,visualHeight,pixelRatio:window.devicePixelRatio});});}
 function openSettings(){clearSpelling();closeMenus();root.hidden=true;if(!settings.open)settings.show();fit();}
 function closeMenus(){root.querySelectorAll('.to-menu').forEach(menu=>menu.hidden=true);root.querySelectorAll('[aria-expanded]').forEach(button=>button.setAttribute('aria-expanded','false'));fit();}
 function savePreferences(){post({type:'preferences',...state});}
 function render(){
  form.classList.toggle('has-text',!!input.value);form.classList.toggle('is-russian',state.language==='ru');form.classList.toggle('is-google',state.provider==='Google Api');
  q('.to-language-label').hidden=state.language==='ru';q('.to-language-label').textContent=names[state.language];q('.to-language-trigger').setAttribute('aria-label',t('languageChoice')+names[state.language]);
  form.classList.toggle('is-long-language',state.language==='uk'||state.language==='ja');
  form.classList.toggle('has-translation',hasTranslation);q('.to-send').setAttribute('aria-label',t(hasTranslation?'retranslate':'translateLabel'));q('.to-send').title=t(hasTranslation?'retranslate':'translateLabel');
  q('.to-provider-label').hidden=state.provider==='Google Api';q('.to-provider-label').textContent=state.provider;q('.to-provider-trigger').setAttribute('aria-label',t('providerLabel')+state.provider);
  form.classList.toggle('offline-missing',state.provider==='Offline'&&!offlineReady&&!updating);form.classList.toggle('offline-downloading',state.provider==='Offline'&&updating);
  q('.to-inline-download').hidden=state.provider!=='Offline'||offlineReady||updating;q('.to-inline-progress').hidden=state.provider!=='Offline'||!updating;
  if(!q('.to-languages').children.length)Object.entries(names).forEach(([code,name])=>{
   const button=document.createElement('button');button.type='button';button.className='cursor-interaction';button.setAttribute('aria-pressed',String(code===state.language));
   button.dataset.language=code;
   const label=document.createElement('span');label.className='to-language-name';label.textContent=name;const dot=document.createElement('span');dot.className='to-language-dot';dot.setAttribute('aria-hidden','true');button.append(dot,label);
   button.addEventListener('click',()=>{if(busy)return;state.language=code;hasTranslation=false;closeMenus();render();q('.to-language-trigger').focus({preventScroll:true});savePreferences();});q('.to-languages').append(button);
  });
  q('.to-languages').querySelectorAll('button').forEach(button=>{button.setAttribute('aria-pressed',String(button.dataset.language===state.language));});
  root.querySelectorAll('[data-provider]').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.provider===state.provider)));
  download.hidden=state.provider!=='Offline';offlineInfo.hidden=state.provider!=='Offline';
  positionTextAction();
 }
 function setBusy(value){if(value)clearSpelling();busy=value;input.readOnly=value;root.querySelectorAll('button').forEach(button=>button.disabled=value&&!(translationPending&&button.matches('.to-language-trigger,.to-provider-trigger')));form.setAttribute('aria-busy',String(value));q('.to-send').style.opacity=value?'.45':'1';positionTextAction();}
 function translate(){
  if(busy)return;closeMenus();const text=input.value.trim();if(!text){focusInput();return;}
  if(state.provider==='Offline'&&!offlineReady){notify(t('downloadFirst'));return;}
  state.source=hasTranslation?lastTarget:'auto';state.target=hasTranslation?lastSource:state.language;
  previousText=input.value;translationPending=true;setBusy(true);notify('');
  post({type:'translate',requestId:++requestId,text,source:state.source,target:state.target,provider:state.provider});
 }
 root.querySelectorAll('[aria-controls]').forEach(button=>button.addEventListener('click',()=>{const menu=q('#'+button.getAttribute('aria-controls'));const open=menu.hidden;closeMenus();menu.hidden=!open;button.setAttribute('aria-expanded',String(open));fit();}));
 root.querySelectorAll('[data-provider]').forEach(button=>button.addEventListener('click',()=>{state.provider=button.dataset.provider;closeMenus();render();savePreferences();scheduleSpelling();}));
 q('.to-inline-download').addEventListener('click',event=>{event.stopPropagation();startDownload();});
 form.addEventListener('submit',event=>{event.preventDefault();translate();});
 input.addEventListener('keydown',event=>{if(event.isComposing||composing)return;if(event.key==='Enter'&&!event.shiftKey){event.preventDefault();translate();}if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='z'&&previousText){event.preventDefault();clearSpelling();input.value=previousText;previousText='';hasTranslation=false;render();fit();scheduleSpelling();}});
 input.addEventListener('input',()=>{hasTranslation=false;render();notify('');fit();scheduleSpelling();});
 root.addEventListener('click',event=>{if(!event.target.closest('.to-language-picker,.to-provider-picker'))closeMenus();});
 document.addEventListener('keydown',event=>{if(event.key==='Escape'){event.preventDefault();post({type:'minimize'});}});
 q('.to-clip').addEventListener('click',()=>post({type:'file'}));q('.to-mic').addEventListener('click',()=>post({type:'microphone',provider:state.provider}));
 const menu=q('#to-provider-menu');
 const offlineInfo=document.createElement('div');offlineInfo.className='to-caption';offlineInfo.textContent='Offline · русский ↔ английский';menu.append(offlineInfo);
 const download=document.createElement('button');download.type='button';download.dataset.i18n='downloadDictionary';download.addEventListener('click',startDownload);menu.append(download);
 function startDownload(){if(updating)return;closeMenus();updating=true;document.querySelector('.app-download-progress').hidden=false;document.querySelector('.app-download-progress').value=0;document.querySelector('.app-download-dictionary').disabled=true;download.disabled=true;render();notify('');post({type:'updateOffline'});}
 document.querySelector('.app-download-dictionary').addEventListener('click',startDownload);
 const configure=document.createElement('button');configure.type='button';configure.dataset.i18n='settings';configure.addEventListener('click',openSettings);menu.append(configure);
 const about=document.createElement('button');about.type='button';about.dataset.i18n='about';about.addEventListener('click',()=>{closeMenus();notify(t('dictionaryAbout'));});menu.append(about);
 for(const [type,key] of [['minimize','minimize'],['close','close']]){const button=document.createElement('button');button.type='button';button.dataset.i18n=key;button.addEventListener('click',()=>post({type}));menu.append(button);}
 form.addEventListener('pointerdown',event=>{if(event.button===0&&!event.target.closest('button,.to-input,.to-menu')){event.preventDefault();post({type:'drag'});}});
 settings.addEventListener('pointerdown',event=>{if(event.button===0&&!event.target.closest('input,button,a,label,summary,.app-ui-menu')){event.preventDefault();post({type:'drag'});}});
 document.querySelector('.app-settings-close').addEventListener('click',()=>{settings.close();fit();});
 document.querySelector('.app-settings-form').addEventListener('submit',event=>{
  event.preventDefault();const data=new FormData(event.currentTarget);if(data.get('hotkeyEnabled')==='on'&&!data.get('hotkey')){notify(t('hotkeyInvalid'));return;}post({type:'saveSettings',googleKey:data.get('googleKey'),clearKeys:data.get('clearKeys')==='on',hotkey:data.get('hotkey')||'Win+0',winZeroEnabled:data.get('hotkeyEnabled')==='on',spellcheckEnabled:data.get('spellcheckEnabled')==='on',uiLanguage});
 });
 settings.addEventListener('close',()=>{root.hidden=false;uiLanguage=savedUiLanguage;applyLanguage();fit();});
 document.querySelectorAll('.app-help-link').forEach(link=>link.addEventListener('click',event=>{event.preventDefault();post({type:'openHelp',page:link.dataset.page});}));
 const hotkeyInput=document.querySelector('[name="hotkey"]');
 const hotkeyEnabled=document.querySelector('[name="hotkeyEnabled"]');
 hotkeyEnabled.addEventListener('change',()=>{hotkeyInput.disabled=!hotkeyEnabled.checked;if(hotkeyEnabled.checked)hotkeyInput.focus();});
 hotkeyInput.addEventListener('keydown',event=>{if(event.key==='Tab')return;event.preventDefault();const key=event.code.replace(/^Key|^Digit/,'');if(!event.shiftKey&&/^(Key[A-Z]|Digit[0-9])$/.test(event.code)&&((event.metaKey&&!event.ctrlKey&&!event.altKey&&key!=='L')||(event.ctrlKey&&event.altKey&&!event.metaKey))){hotkeyInput.value=(event.metaKey?'Win+':'Ctrl+Alt+')+key;}else if(!['Control','Alt','Shift','Meta'].includes(event.key)){notify(t('hotkeyInvalid'));}});
 const uiMenu=document.querySelector('.app-ui-menu'),uiTrigger=document.querySelector('.app-ui-trigger');
 uiTrigger.addEventListener('click',()=>{uiMenu.hidden=!uiMenu.hidden;uiTrigger.setAttribute('aria-expanded',String(!uiMenu.hidden));});
 document.addEventListener('click',event=>{if(!event.target.closest('.app-ui-picker')){uiMenu.hidden=true;uiTrigger.setAttribute('aria-expanded','false');}});
 function applyLanguage(){
  locale=uiLanguage==='system'?systemLanguage:uiLanguage;document.documentElement.lang=locale;
  document.querySelectorAll('[data-i18n]').forEach(element=>element.textContent=t(element.dataset.i18n));
  document.querySelectorAll('[data-placeholder-i18n]').forEach(element=>element.placeholder=t(element.dataset.placeholderI18n));
  document.querySelector('.app-google-status').textContent=t(hasGoogleKey?'keySaved':'googleKeyHint');
  offlineInfo.textContent=t('offlineInfo');
  for(const [selector,key] of [['.to-input','textLabel'],['.to-mic','micLabel'],['.to-clip','fileLabel'],['.to-send','translateLabel']])q(selector).setAttribute('aria-label',t(key));
  hotkeyInput.setAttribute('aria-label',t('hotkeyTitle'));
  uiMenu.replaceChildren();for(const [code,label] of [['system',t('systemLanguage')],['ru','Русский'],['en','English']]){const button=document.createElement('button');button.type='button';button.textContent=label;button.setAttribute('aria-pressed',String(code===uiLanguage));button.addEventListener('click',()=>{uiLanguage=code;uiMenu.hidden=true;applyLanguage();fit();});uiMenu.append(button);}
  document.querySelector('.app-ui-value').textContent=uiLanguage==='system'?t('systemLanguage'):uiLanguage==='ru'?'Русский':'English';render();
 }
 bridge?.addEventListener('message',event=>{
  const message=event.data;
  if(message.type==='init'){
   if(names[message.language])state.language=message.language;offlineReady=!!message.offlineReady;if(['Google Api','Offline'].includes(message.provider))state.provider=message.provider;
   hasGoogleKey=message.hasGoogleKey;spellcheckEnabled=message.spellcheckEnabled!==false;document.querySelector('[name="spellcheckEnabled"]').checked=spellcheckEnabled;spellCooldown=0;spellingCache.clear();systemLanguage=message.systemLanguage==='ru'?'ru':'en';uiLanguage=['system','ru','en'].includes(message.uiLanguage)?message.uiLanguage:'system';savedUiLanguage=uiLanguage;
   hotkeyInput.value=message.hotkey||'Win+0';hotkeyEnabled.checked=message.winZeroEnabled!==false;hotkeyInput.disabled=!hotkeyEnabled.checked;applyLanguage();fit();scheduleSpelling();
  }
  if(message.type==='spelling'&&message.requestId===spellSequence&&message.text===input.value){spellInFlight=false;if(!canCheckSpelling())return;if(!message.ok){spellCooldown=Date.now()+Math.max(10000,Math.min(60000,message.cooldown||30000));if(message.errorCode==='auth'){hasGoogleKey=false;notify(t('spellcheckKeyIssue'));}return;}const items=Array.isArray(message.corrections)?message.corrections.filter(item=>Number.isInteger(item.start)&&Number.isInteger(item.end)&&item.start>=0&&item.end>item.start&&message.text.slice(item.start,item.end)===item.original&&typeof item.replacement==='string').slice(0,12):[];spellingCache.set(message.text,items);if(spellingCache.size>24)spellingCache.delete(spellingCache.keys().next().value);showSpelling(message.text,items);}
  if(message.type==='translation'&&message.requestId===requestId){translationPending=false;setBusy(false);if(message.ok){lastSource=message.source||(/[а-яё]/i.test(previousText)?'ru':'en');lastTarget=message.target||state.target;hasTranslation=true;input.value=message.text;input.lang=lastTarget;render();fit();if(!settings.open&&!q('.to-menu:not([hidden])'))focusInput();input.setSelectionRange(input.value.length,input.value.length);q('[role="status"]').textContent=t('translationReady');}else{notify(message.error);if(message.needsKey){openSettings();}}}
  if(message.type==='file'){if(message.ok){input.value=message.text;previousText='';hasTranslation=false;render();fit();focusInput();}else notify(message.error);}
  if(message.type==='speech'){if(message.ok){input.value=message.text;previousText='';hasTranslation=false;render();fit();focusInput();}notify(message.error||'');}
  if(message.type==='speechState'){const recording=message.state==='recording',processing=message.state==='processing';setBusy(recording||processing);q('.to-mic').disabled=processing;q('.to-mic').classList.toggle('is-recording',recording);q('.to-mic').setAttribute('aria-pressed',String(recording));q('.to-mic').setAttribute('aria-label',t(recording?'stopRecording':'micLabel'));q('.to-mic').title=t(recording?'stopRecording':'micLabel');if(recording||processing){notify(t(recording?'recordingHint':'recognizing'));clearTimeout(toastTimer);}else if(toast.textContent===t('recordingHint')||toast.textContent===t('recognizing'))notify('');}
  if(message.type==='settingsSaved'){if(message.ok){document.querySelectorAll('[name="googleKey"]').forEach(field=>field.value='');document.querySelector('[name="clearKeys"]').checked=false;settings.close();}notify(message.ok?t('saved'):message.error);fit();}
  if(message.type==='openSettings')openSettings();
  if(message.type==='showTranslator'){closeMenus();if(settings.open)settings.close();root.hidden=false;fit();requestAnimationFrame(focusInput);scheduleSpelling();}
  if(message.type==='hideTranslator'){clearSpelling();closeMenus();if(settings.open)settings.close();notify('');}
  if(message.type==='copied')notify(t(message.ok?'copied':'copyFailed'));
  if(message.type==='offlineProgress'){document.querySelector('.app-download-progress').value=message.percent;q('.to-inline-progress').setAttribute('aria-valuenow',String(message.percent));q('.to-inline-progress>span').style.width=message.percent+'%';download.textContent=t('downloading')+' · '+message.percent+'%';}
  if(message.type==='offlineUpdated'){updating=false;if(message.ok)offlineReady=true;download.disabled=false;download.textContent=t('downloadDictionary');document.querySelector('.app-download-dictionary').disabled=false;document.querySelector('.app-download-progress').hidden=true;render();notify(message.ok?t('updated')+' · '+message.version:message.error);}
 });
 window.addEventListener('resize',fit);applyLanguage();fit();post({type:'ready'});
})();
