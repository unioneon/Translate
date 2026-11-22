// Plain-text editor with non-editable, inline spelling suggestions.
// Suggestions are excluded from value, copy, selection offsets and API payloads.
function createSpellingEditor(editor){
 let locked=false,lastSelection=null;
 const ghost=node=>node.nodeType===1&&node.hasAttribute('data-spelling-hint');
 function model(){
  let text='';const points=[],bounds=new WeakMap();
  function add(value,node,offset=0){const start=text.length;text+=value;for(let i=0;i<=value.length;i++)if(!points[start+i])points[start+i]={node,offset:offset+i};}
  function walk(node){
   const start=text.length;
   if(ghost(node)){bounds.set(node,{start,end:start,children:[start]});return;}
   if(node.nodeType===3){add(node.data,node);bounds.set(node,{start,end:text.length});return;}
   if(node.nodeType!==1&&node!==editor)return;
   if(node.nodeName==='BR'){
    const trailing=node===node.parentNode.lastChild&&(node.previousSibling?.nodeName==='BR'||node.parentNode.childNodes.length===1);
    if(!trailing){const parent=node.parentNode,index=[...parent.childNodes].indexOf(node);add('\n',parent,index);points[text.length]={node:parent,offset:index+1};}
    bounds.set(node,{start,end:text.length});return;
   }
   if(node!==editor&&/^(DIV|P)$/.test(node.nodeName)&&text&&!text.endsWith('\n')){const parent=node.parentNode,index=[...parent.childNodes].indexOf(node);add('\n',parent,index);points[text.length]={node,offset:0};}
   const children=[];for(const child of node.childNodes){children.push(text.length);walk(child);}children.push(text.length);bounds.set(node,{start,end:text.length,children});
  }
  walk(editor);if(!points.length)points.push({node:editor,offset:0});return{text,points,bounds};
 }
 function selection(){
  const data=model(),selected=window.getSelection();
  if(!selected?.rangeCount||!editor.contains(selected.anchorNode)||!editor.contains(selected.focusNode))return lastSelection||{start:data.text.length,end:data.text.length};
  function offset(node,index){const b=data.bounds.get(node);if(b)return node.nodeType===3?Math.min(b.end,b.start+index):(b.children?.[index]??b.end);while(node&&node!==editor){node=node.parentNode;const parent=data.bounds.get(node);if(parent)return parent.start;}return data.text.length;}
  const a=offset(selected.anchorNode,selected.anchorOffset),b=offset(selected.focusNode,selected.focusOffset);return lastSelection={start:Math.min(a,b),end:Math.max(a,b)};
 }
 function setSelection(start,end=start){const data=model();start=Math.max(0,Math.min(data.text.length,start));end=Math.max(start,Math.min(data.text.length,end));const a=data.points[start]||data.points.at(-1),b=data.points[end]||data.points.at(-1);const range=document.createRange();range.setStart(a.node,a.offset);range.setEnd(b.node,b.offset);const selected=window.getSelection();selected.removeAllRanges();selected.addRange(range);lastSelection={start,end};}
 Object.defineProperties(editor,{
  value:{get:()=>model().text,set:value=>{editor.textContent=String(value);lastSelection=null;}},
  readOnly:{get:()=>locked,set:value=>{locked=!!value;editor.contentEditable=locked?'false':'true';editor.setAttribute('aria-readonly',String(locked));}},
  selectionStart:{get:()=>selection().start},selectionEnd:{get:()=>selection().end}
 });
 editor.setSelectionRange=setSelection;
 editor.clearSpellingHints=()=>{if(!editor.querySelector('[data-spelling-hint]'))return;const position=selection(),focused=editor.contains(document.activeElement)||document.activeElement===editor,text=editor.value;editor.value=text;if(focused)setSelection(position.start,position.end);};
 editor.showSpellingHints=(text,corrections,label)=>{
  if(editor.value!==text)return;const position=selection(),focused=document.activeElement===editor,fragment=document.createDocumentFragment();let offset=0;
  for(const correction of corrections){if(correction.start<offset||correction.end>text.length||text.slice(correction.start,correction.end)!==correction.original)continue;
   fragment.append(document.createTextNode(text.slice(offset,correction.end)));offset=correction.end;
   const hint=document.createElement('span');hint.className='to-spelling-hint';hint.contentEditable='false';hint.dataset.spellingHint='';hint.setAttribute('role','button');hint.tabIndex=0;hint.textContent=correction.replacement;hint.title=label+' '+correction.replacement;hint.setAttribute('aria-label',hint.title);
   const accept=event=>{event.preventDefault();event.stopPropagation();if(locked||editor.value!==text)return;editor.dispatchEvent(new CustomEvent('spellingaccept',{detail:{...correction,text}}));};
   hint.addEventListener('pointerdown',event=>event.preventDefault());hint.addEventListener('click',accept);hint.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){accept(event);}});fragment.append(hint);
  }
  fragment.append(document.createTextNode(text.slice(offset)));editor.replaceChildren(fragment);if(focused)setSelection(position.start,position.end);
 };
 editor.getVisualEndRect=()=>{const walker=document.createTreeWalker(editor,NodeFilter.SHOW_TEXT);let node,last;while(node=walker.nextNode())if(node.length)last=node;if(!last)return null;const range=document.createRange();range.setStart(last,last.length);range.collapse(true);const rects=range.getClientRects();if(rects.length)return rects[rects.length-1];range.setStart(last,last.length-1);range.setEnd(last,last.length);const rect=range.getBoundingClientRect();return {left:rect.right,right:rect.right,top:rect.top,bottom:rect.bottom,height:rect.height};};
 editor.addEventListener('input',()=>editor.clearSpellingHints());
 editor.addEventListener('paste',event=>{if(locked)return;event.preventDefault();const text=event.clipboardData.getData('text/plain'),position=selection(),value=editor.value;editor.value=value.slice(0,position.start)+text+value.slice(position.end);setSelection(position.start+text.length);editor.dispatchEvent(new Event('input',{bubbles:true}));});
 editor.addEventListener('copy',event=>{const position=selection();if(position.start!==position.end){event.preventDefault();event.clipboardData.setData('text/plain',editor.value.slice(position.start,position.end));}});
 editor.addEventListener('drop',event=>event.preventDefault());
 editor.addEventListener('beforeinput',event=>{if(event.inputType?.startsWith('format'))event.preventDefault();});
 editor.readOnly=false;return editor;
}
